// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetDependencyWalker.Walking.MonoCecil.DependencyReader
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class DependencyContextJObjectReader
    {
        static readonly string[] EmptyStringArray = new string[0];

        public DependencyContext Read(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var streamReader = new StreamReader(stream);
            return Read(streamReader);
        }


        private DependencyContext Read(TextReader reader)
        {
            // {
            //     "runtimeTarget": {...},
            //     "compilationOptions": {...},
            //     "targets": {...},
            //     "libraries": {...},
            //     "runtimes": {...}
            // }

            JObject root;
            using (var jsonTextReader = new JsonTextReader(reader))
            {
                root = JObject.Load(jsonTextReader);
            }

            var runtime = string.Empty;
            var framework = string.Empty;
            var isPortable = true;

            ReadRuntimeTarget(root.ValueAsJsonObject(DependencyContextStrings.RuntimeTargetPropertyName), out var runtimeTargetName, out var runtimeSignature);
            var compilationOptions = ReadCompilationOptions(root.ValueAsJsonObject(DependencyContextStrings.CompilationOptionsPropertyName)) ?? CompilationOptions.Default;
            var targets = ReadTargets(root.ValueAsJsonObject(DependencyContextStrings.TargetsPropertyName));
            var libraryStubs = ReadLibraries(root.ValueAsJsonObject(DependencyContextStrings.LibrariesPropertyName));
            var runtimeFallbacks = ReadRuntimes(root.ValueAsJsonObject(DependencyContextStrings.RuntimesPropertyName));

            var runtimeTarget = SelectRuntimeTarget(targets, runtimeTargetName);
            runtimeTargetName = runtimeTarget?.Name;

            if (runtimeTargetName != null)
            {
                var separatorIndex = runtimeTargetName.IndexOf(DependencyContextStrings.VersionSeparator);
                if (separatorIndex > -1 && separatorIndex < runtimeTargetName.Length)
                {
                    runtime = runtimeTargetName.Substring(separatorIndex + 1);
                    framework = runtimeTargetName.Substring(0, separatorIndex);
                    isPortable = false;
                }
                else
                {
                    framework = runtimeTargetName;
                }
            }

            Target compileTarget = null;

            var ridlessTarget = targets.FirstOrDefault(t => !IsRuntimeTarget(t.Name));
            if (ridlessTarget != null)
            {
                compileTarget = ridlessTarget;
                if (runtimeTarget == null)
                {
                    runtimeTarget = compileTarget;
                    framework = ridlessTarget.Name;
                }
            }

            if (runtimeTarget == null)
                throw new FormatException("No runtime target found");

            return new DependencyContext(
                new TargetInfo(framework, runtime, runtimeSignature, isPortable),
                compilationOptions,
                CreateLibraries(compileTarget?.Libraries, false, libraryStubs).Cast<CompilationLibrary>().ToArray(),
                CreateLibraries(runtimeTarget.Libraries, true, libraryStubs).Cast<RuntimeLibrary>().ToArray(),
                runtimeFallbacks ?? Enumerable.Empty<RuntimeFallbacks>());
        }

        private Target SelectRuntimeTarget(List<Target> targets, string runtimeTargetName)
        {
            Target target;

            if (targets == null || targets.Count == 0)
                throw new FormatException("Dependency file does not have 'targets' section");

            if (!string.IsNullOrEmpty(runtimeTargetName))
            {
                target = targets.FirstOrDefault(t => t.Name == runtimeTargetName);
                if (target == null)
                    throw new FormatException($"Target with name {runtimeTargetName} not found");
            }
            else
            {
                target = targets.FirstOrDefault(t => IsRuntimeTarget(t.Name));
            }

            return target;
        }

        private bool IsRuntimeTarget(string name)
            => name.Contains(DependencyContextStrings.VersionSeparator);

        private void ReadRuntimeTarget(JObject runtimeTargetJson, out string runtimeTargetName, out string runtimeSignature)
        {
            // {
            //     "name": ".NETCoreApp,Version=v1.0",
            //     "signature": "35bd60f1a92c048eea72ff8160ba07b616ebd0f6"
            // }

            runtimeTargetName = runtimeTargetJson?.ValueAsString(DependencyContextStrings.RuntimeTargetNamePropertyName);
            runtimeSignature = runtimeTargetJson?.ValueAsString(DependencyContextStrings.RuntimeTargetSignaturePropertyName);
        }

        private CompilationOptions ReadCompilationOptions(JObject compilationOptionsJson)
        {
            return compilationOptionsJson?.ToObject<CompilationOptions>();
        }

        private List<Target> ReadTargets(JObject targetsJson)
        {
            // Object dictionary: string => object

            var targets = new List<Target>();

            if (targetsJson != null)
            {
                foreach (var pair in targetsJson)
                {
                    targets.Add(ReadTarget(pair.Key, targetsJson.ValueAsJsonObject(pair.Key)));
                }
            }

            return targets;
        }

        private Target ReadTarget(string targetName, JObject targetJson)
        {
            // Object dictionary: string => object

            var libraries = new List<TargetLibrary>();

            foreach (var pair in targetJson)
            {
                libraries.Add(ReadTargetLibrary(pair.Key, targetJson.ValueAsJsonObject(pair.Key)));
            }

            return new Target { Name = targetName, Libraries = libraries };
        }

        private IEnumerable<string> GetKeys(JObject obj)
        {
            if (obj == null)
            {
                yield break;
            }

            foreach (var pair in obj)
            {
                yield return pair.Key;
            }
        }

        private TargetLibrary ReadTargetLibrary(string targetLibraryName, JObject targetLibraryJson)
        {
            // {
            //     "dependencies": {...},
            //     "runtime": {...},       # Dictionary: name => {}
            //     "native": {...},        # Dictionary: name => {}
            //     "compile": {...},       # Dictionary: name => {}
            //     "runtime": {...},
            //     "resources": {...},
            //     "compileOnly": "true|false"
            // }

            var dependencies = ReadTargetLibraryDependencies(targetLibraryJson.ValueAsJsonObject(DependencyContextStrings.DependenciesPropertyName));
            var runtimes = GetKeys(targetLibraryJson.ValueAsJsonObject(DependencyContextStrings.RuntimeAssembliesKey));
            var natives = GetKeys(targetLibraryJson.ValueAsJsonObject(DependencyContextStrings.NativeLibrariesKey));
            var compilations = GetKeys(targetLibraryJson.ValueAsJsonObject(DependencyContextStrings.CompileTimeAssembliesKey));
            var runtimeTargets = ReadTargetLibraryRuntimeTargets(targetLibraryJson.ValueAsJsonObject(DependencyContextStrings.RuntimeTargetsPropertyName));
            var resources = ReadTargetLibraryResources(targetLibraryJson.ValueAsJsonObject(DependencyContextStrings.ResourceAssembliesPropertyName));
            var compileOnly = targetLibraryJson.ValueAsNullableBoolean(DependencyContextStrings.CompilationOnlyPropertyName);

            return new TargetLibrary
            {
                Name = targetLibraryName,
                Dependencies = dependencies ?? Enumerable.Empty<Dependency>(),
                Runtimes = runtimes?.ToList(),
                Natives = natives?.ToList(),
                Compilations = compilations?.ToList(),
                RuntimeTargets = runtimeTargets,
                Resources = resources,
                CompileOnly = compileOnly
            };
        }

        public IEnumerable<Dependency> ReadTargetLibraryDependencies(JObject targetLibraryDependenciesJson)
        {
            // Object dictionary: string => string

            var dependencies = new List<Dependency>();

            if (targetLibraryDependenciesJson != null)
                foreach (var pair in targetLibraryDependenciesJson)
                {
                    dependencies.Add(new Dependency(pair.Key, targetLibraryDependenciesJson.ValueAsString(pair.Key)));
                }

            return dependencies;
        }

        private List<RuntimeTargetEntryStub> ReadTargetLibraryRuntimeTargets(JObject targetLibraryRuntimeTargetsJson)
        {
            // Object dictionary: string => { "rid": "...", "assetType": "..." }

            var runtimeTargets = new List<RuntimeTargetEntryStub>();

            if (targetLibraryRuntimeTargetsJson != null)
            {
                foreach (var pair in targetLibraryRuntimeTargetsJson)
                {
                    var runtimeTargetJson = targetLibraryRuntimeTargetsJson.ValueAsJsonObject(pair.Key);

                    runtimeTargets.Add(new RuntimeTargetEntryStub
                    {
                        Path = pair.Key,
                        Rid = runtimeTargetJson?.ValueAsString(DependencyContextStrings.RidPropertyName),
                        Type = runtimeTargetJson?.ValueAsString(DependencyContextStrings.AssetTypePropertyName)
                    });
                }
            }

            return runtimeTargets;
        }

        private List<ResourceAssembly> ReadTargetLibraryResources(JObject targetLibraryResourcesJson)
        {
            // Object dictionary: string => { "locale": "..." }

            var resources = new List<ResourceAssembly>();

            if (targetLibraryResourcesJson != null)
            {
                foreach (var pair in targetLibraryResourcesJson)
                {
                    var locale = targetLibraryResourcesJson.ValueAsJsonObject(pair.Key)?.ValueAsString(DependencyContextStrings.LocalePropertyName);

                    if (locale != null)
                        resources.Add(new ResourceAssembly(pair.Key, locale));
                }
            }

            return resources;
        }

        private Dictionary<string, LibraryStub> ReadLibraries(JObject librariesJson)
        {
            // Object dictionary: string => object

            var libraries = new Dictionary<string, LibraryStub>();

            if (librariesJson != null)
                foreach (var pair in librariesJson)
                    libraries.Add(pair.Key, ReadLibrary(librariesJson.ValueAsJsonObject(pair.Key)));

            return libraries;
        }

        private LibraryStub ReadLibrary(JObject libraryJson)
        {
            return libraryJson.ToObject<LibraryStub>();
        }

        private List<RuntimeFallbacks> ReadRuntimes(JObject runtimesJson)
        {
            // Object dictionary: string => ["...","...",...]

            var runtimeFallbacks = new List<RuntimeFallbacks>();

            if (runtimesJson != null)
                foreach (var pair in runtimesJson)
                    runtimeFallbacks.Add(new RuntimeFallbacks(pair.Key, runtimesJson.ValueAsStringArray(pair.Key) ?? EmptyStringArray));

            return runtimeFallbacks;
        }

        private IEnumerable<Library> CreateLibraries(IEnumerable<TargetLibrary> libraries, bool runtime, Dictionary<string, LibraryStub> libraryStubs)
        {
            if (libraries == null)
                return Enumerable.Empty<Library>();

            return libraries.Select(property => CreateLibrary(property, runtime, libraryStubs))
                .Where(library => library != null);
        }

        private Library CreateLibrary(TargetLibrary targetLibrary, bool runtime, Dictionary<string, LibraryStub> libraryStubs)
        {
            var nameWithVersion = targetLibrary.Name;

            if (libraryStubs == null || !libraryStubs.TryGetValue(nameWithVersion, out var stub))
            {
                throw new InvalidOperationException($"Cannot find library information for {nameWithVersion}");
            }

            var separatorPosition = nameWithVersion.IndexOf(DependencyContextStrings.VersionSeparator);

            var name = nameWithVersion.Substring(0, separatorPosition);
            var version = nameWithVersion.Substring(separatorPosition + 1);

            if (runtime)
            {
                // Runtime section of this library was trimmed by type:platform
                var isCompilationOnly = targetLibrary.CompileOnly;
                if (isCompilationOnly == true)
                {
                    return null;
                }

                var runtimeAssemblyGroups = new List<RuntimeAssetGroup>();
                var nativeLibraryGroups = new List<RuntimeAssetGroup>();
                if (targetLibrary.RuntimeTargets != null)
                {
                    foreach (var ridGroup in targetLibrary.RuntimeTargets.GroupBy(e => e.Rid))
                    {
                        var groupRuntimeAssemblies = ridGroup
                            .Where(e => e.Type == DependencyContextStrings.RuntimeAssetType)
                            .Select(e => e.Path)
                            .ToArray();

                        if (groupRuntimeAssemblies.Any())
                        {
                            runtimeAssemblyGroups.Add(new RuntimeAssetGroup(
                                ridGroup.Key,
                                groupRuntimeAssemblies.Where(a => Path.GetFileName(a) != "_._")));
                        }

                        var groupNativeLibraries = ridGroup
                            .Where(e => e.Type == DependencyContextStrings.NativeAssetType)
                            .Select(e => e.Path)
                            .ToArray();

                        if (groupNativeLibraries.Any())
                        {
                            nativeLibraryGroups.Add(new RuntimeAssetGroup(
                                ridGroup.Key,
                                groupNativeLibraries.Where(a => Path.GetFileName(a) != "_._")));
                        }
                    }
                }

                if (targetLibrary.Runtimes != null && targetLibrary.Runtimes.Count > 0)
                {
                    runtimeAssemblyGroups.Add(new RuntimeAssetGroup(string.Empty, targetLibrary.Runtimes));
                }

                if (targetLibrary.Natives != null && targetLibrary.Natives.Count > 0)
                {
                    nativeLibraryGroups.Add(new RuntimeAssetGroup(string.Empty, targetLibrary.Natives));
                }

                return new RuntimeLibrary(
                    stub.Type,
                    name,
                    version,
                    stub.Hash,
                    runtimeAssemblyGroups,
                    nativeLibraryGroups,
                    targetLibrary.Resources ?? Enumerable.Empty<ResourceAssembly>(),
                    targetLibrary.Dependencies,
                    stub.Serviceable,
                    stub.Path,
                    stub.HashPath,
                    stub.RuntimeStoreManifestName);
            }

            var assemblies = targetLibrary.Compilations ?? Enumerable.Empty<string>();
            return new CompilationLibrary(
                stub.Type,
                name,
                version,
                stub.Hash,
                assemblies,
                targetLibrary.Dependencies,
                stub.Serviceable,
                stub.Path,
                stub.HashPath);
        }
        
        private class Target
        {
            public string Name;

            public IEnumerable<TargetLibrary> Libraries;
        }

        private struct TargetLibrary
        {
            public string Name;

            public IEnumerable<Dependency> Dependencies;

            public List<string> Runtimes;

            public List<string> Natives;

            public List<string> Compilations;

            public List<RuntimeTargetEntryStub> RuntimeTargets;

            public List<ResourceAssembly> Resources;

            public bool? CompileOnly;
        }

        private struct RuntimeTargetEntryStub
        {
            public string Type;

            public string Path;

            public string Rid;
        }

        private class LibraryStub
        {
            [JsonProperty("sha512")]
            public string Hash { get; set; }

            // ReSharper disable UnusedAutoPropertyAccessor.Local
            public string Type { get; set; }

            public bool Serviceable { get; set; }

            public string Path { get; set; }

            public string HashPath { get; set; }

            public string RuntimeStoreManifestName { get; set; }
            // ReSharper restore UnusedAutoPropertyAccessor.Local
        }
    }
}