// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetDependencyWalker.Walking.MonoCecil
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Mono.Cecil;

    public class UniversalAssemblyResolver
    {
        private DotNetCorePathFinder _dotNetCorePathFinder;
        private readonly string _mainAssemblyFileName;
        private readonly string _baseDirectory;
        private readonly HashSet<string> _directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> GacPaths = GetGacPaths();

        public void AddSearchDirectory(string directory)
        {
            if (_directories.Add(directory))
            {
                _dotNetCorePathFinder?.AddSearchDirectory(directory);
            }
        }

        private readonly string _targetFramework;
        private readonly TargetFrameworkIdentifier _targetFrameworkIdentifier;
        private readonly Version _targetFrameworkVersion;

        public UniversalAssemblyResolver(string mainAssemblyFileName, string targetFramework)
        {
            _mainAssemblyFileName = mainAssemblyFileName;
            _targetFramework = targetFramework ?? string.Empty;
            (_targetFrameworkIdentifier, _targetFrameworkVersion) = ParseTargetFramework(_targetFramework);

            if (mainAssemblyFileName != null)
            {
                var baseDirectory = Path.GetDirectoryName(mainAssemblyFileName);
                if (string.IsNullOrWhiteSpace(_baseDirectory))
                    _baseDirectory = Environment.CurrentDirectory;
                AddSearchDirectory(baseDirectory);
            }
        }

        internal static (TargetFrameworkIdentifier, Version) ParseTargetFramework(string targetFramework)
        {
            var tokens = targetFramework.Split(',');
            TargetFrameworkIdentifier identifier;

            switch (tokens[0].Trim().ToUpperInvariant())
            {
                case ".NETCOREAPP":
                    identifier = TargetFrameworkIdentifier.NetCoreApp;
                    break;
                case ".NETSTANDARD":
                    identifier = TargetFrameworkIdentifier.NetStandard;
                    break;
                case "SILVERLIGHT":
                    identifier = TargetFrameworkIdentifier.Silverlight;
                    break;
                default:
                    identifier = TargetFrameworkIdentifier.NetFramework;
                    break;
            }

            Version version = null;

            for (var i = 1; i < tokens.Length; i++)
            {
                var pair = tokens[i].Trim().Split('=');

                if (pair.Length != 2)
                    continue;

                switch (pair[0].Trim().ToUpperInvariant())
                {
                    case "VERSION":
                        var versionString = pair[1].TrimStart('v', ' ', '\t');
                        if (identifier == TargetFrameworkIdentifier.NetCoreApp ||
                            identifier == TargetFrameworkIdentifier.NetStandard)
                        {
                            if (versionString.Length == 3)
                                versionString += ".0";
                        }
                        if (!Version.TryParse(versionString, out version))
                            version = null;
                        break;
                }
            }

            return (identifier, version ?? ZeroVersion);
        }

        public string FindAssemblyFile(AssemblyName name)
        {
            switch (_targetFrameworkIdentifier)
            {
                case TargetFrameworkIdentifier.NetCoreApp:
                case TargetFrameworkIdentifier.NetStandard:
                    if (IsZeroOrAllOnes(_targetFrameworkVersion))
                        goto default;
                    if (_dotNetCorePathFinder == null)
                    {
                        _dotNetCorePathFinder = _mainAssemblyFileName == null ? 
                            new DotNetCorePathFinder(_targetFrameworkIdentifier, _targetFrameworkVersion) : 
                            new DotNetCorePathFinder(_mainAssemblyFileName, _targetFramework, _targetFrameworkIdentifier, _targetFrameworkVersion);

                        foreach (var directory in _directories)
                        {
                            _dotNetCorePathFinder.AddSearchDirectory(directory);
                        }
                    }
                    var file = _dotNetCorePathFinder.TryResolveDotNetCore(name);
                    if (file != null)
                        return file;
                    goto default;
                default:
                    return ResolveInternal(name);
            }
        }

        private string ResolveInternal(AssemblyName name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var assembly = SearchDirectory(name, _directories);
            if (assembly != null)
                return assembly;

            var frameworkDirs = new[] { Path.GetDirectoryName(typeof(object).Module.FullyQualifiedName) };

            if (IsSpecialVersionOrRetargetable(name))
            {
                assembly = SearchDirectory(name, frameworkDirs);
                if (assembly != null)
                    return assembly;
            }

            if (name.Name == "mscorlib")
            {
                assembly = GetCorlib(name);
                if (assembly != null)
                    return assembly;
            }

            assembly = GetAssemblyInGac(name);
            if (assembly != null)
                return assembly;

            // when decompiling assemblies that target frameworks prior to 4.0, we can fall back to the 4.0 assemblies in case the target framework is not installed.
            // but when looking for Microsoft.Build.Framework, Version=15.0.0.0 we should not use the version 4.0 assembly here so that the LoadedAssembly logic can instead fall back to version 15.1.0.0
            if (name.Version <= new Version(4, 0, 0, 0))
            {
                assembly = SearchDirectory(name, frameworkDirs);
                if (assembly != null)
                    return assembly;
            }

            throw new FileLoadException(name.ToString());
        }

        #region .NET / mono GAC handling

        private string SearchDirectory(AssemblyName name, IEnumerable<string> directories)
        {
            foreach (var directory in directories)
            {
                var file = SearchDirectory(name, directory);
                if (file != null)
                    return file;
            }

            return null;
        }

        private static bool IsSpecialVersionOrRetargetable(AssemblyName reference)
        {
            return IsZeroOrAllOnes(reference.Version);
        }

        private string SearchDirectory(AssemblyName name, string directory)
        {
            var extensions = new[] { ".exe", ".dll" };
            foreach (var extension in extensions)
            {
                var file = Path.Combine(directory, name.Name + extension);
                if (!File.Exists(file))
                {
                    continue;
                }

                try
                {
                    return file;
                }
                catch (BadImageFormatException)
                {
                }
            }
            return null;
        }

        public static bool IsZeroOrAllOnes(Version version)
        {
            return version == null
                   || (version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision == 0)
                   || (version.Major == 65535 && version.Minor == 65535 && version.Build == 65535 && version.Revision == 65535);
        }

        internal static Version ZeroVersion = new Version(0, 0, 0, 0);

        private string GetCorlib(AssemblyName reference)
        {
            var version = reference.Version;

            if (reference.GetPublicKeyToken() == null)
                return null;

            var path = GetMscorlibBasePath(version, ToHexString(reference.GetPublicKeyToken(), 8));

            if (path == null)
                return null;

            var file = Path.Combine(path, "mscorlib.dll");
            if (File.Exists(file))
                return file;

            return null;
        }

        public static string ToHexString(IEnumerable<byte> bytes, int estimatedLength)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            var sb = new StringBuilder(estimatedLength * 2);
            foreach (var b in bytes)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }

        private string GetMscorlibBasePath(Version version, string publicKeyToken)
        {
            string GetSubFolderForVersion()
            {
                switch (version.Major)
                {
                    case 1:
                        if (version.MajorRevision == 3300)
                            return "v1.0.3705";
                        return "v1.1.4322";
                    case 2:
                        return "v2.0.50727";
                    case 4:
                        return "v4.0.30319";
                    default:
                        throw new NotSupportedException("Version not supported: " + version);
                }
            }

            if (publicKeyToken == "969db8053d3322ac")
            {
                var programFiles = Environment.Is64BitOperatingSystem ?
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) :
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var cfPath = $@"Microsoft.NET\SDK\CompactFramework\v{version.Major}.{version.Minor}\WindowsCE\";
                var cfBasePath = Path.Combine(programFiles, cfPath);
                if (Directory.Exists(cfBasePath))
                    return cfBasePath;
            }
            else
            {
                var rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET");
                var frameworkPaths = new[] {
                    Path.Combine(rootPath, "Framework"),
                    Path.Combine(rootPath, "Framework64")
                };

                var folder = GetSubFolderForVersion();

                if (folder != null)
                {
                    foreach (var path in frameworkPaths)
                    {
                        var basePath = Path.Combine(path, folder);
                        if (Directory.Exists(basePath))
                            return basePath;
                    }
                }
            }

            throw new NotSupportedException("Version not supported: " + version);
        }

        public static List<string> GetGacPaths()
        {
            var paths = new List<string>(2);
            var windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            paths.Add(Path.Combine(windir, "assembly"));
            paths.Add(Path.Combine(windir, "Microsoft.NET", "assembly"));
            return paths;
        }

        public static string GetAssemblyInGac(AssemblyName reference)
        {
            var publicKeyToken = reference.GetPublicKeyToken();
            if (publicKeyToken == null || publicKeyToken.Length == 0)
                return null;

            return GetAssemblyInNetGac(reference);
        }

        private static string GetAssemblyInNetGac(AssemblyName reference)
        {
            var gacs = new[] { "GAC_MSIL", "GAC_32", "GAC_64", "GAC" };
            var prefixes = new[] { string.Empty, "v4.0_" };

            for (var i = 0; i < GacPaths.Count; i++)
            {
                foreach (var path in gacs)
                {
                    var gac = Path.Combine(GacPaths[i], path);
                    var file = GetAssemblyFile(reference, prefixes[i], gac);
                    if (Directory.Exists(gac) && File.Exists(file))
                        return file;
                }
            }

            return null;
        }

        private static string GetAssemblyFile(AssemblyName reference, string prefix, string gac)
        {
            var gacFolder = new StringBuilder()
                .Append(prefix)
                .Append(reference.Version)
                .Append("__");

            var publicKeyToken = reference.GetPublicKeyToken();
            if (publicKeyToken != null)
            {
                foreach (var ch in publicKeyToken)
                {
                    gacFolder.Append(ch.ToString("x2"));
                }
            }

            return Path.Combine(
                Path.Combine(
                    Path.Combine(gac, reference.Name ?? string.Empty), gacFolder.ToString()),
                reference.Name + ".dll");
        }

        /// <summary>
        /// Gets the names of all assemblies in the GAC.
        /// </summary>
        public static IEnumerable<AssemblyNameReference> EnumerateGac()
        {
            var gacs = new[] { "GAC_MSIL", "GAC_32", "GAC_64", "GAC" };
            foreach (var path in GetGacPaths())
            {
                foreach (var gac in gacs)
                {
                    var rootPath = Path.Combine(path, gac);
                    if (!Directory.Exists(rootPath))
                        continue;
                    foreach (var item in new DirectoryInfo(rootPath).EnumerateFiles("*.dll", SearchOption.AllDirectories))
                    {
                        var directoryName = Path.GetDirectoryName(item.FullName);
                        if (string.IsNullOrWhiteSpace(directoryName))
                        {
                            continue;
                        }
                        var name = directoryName.Substring(rootPath.Length + 1).Split(new[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
                        if (name.Length != 2)
                            continue;
                        var match = Regex.Match(name[1], "(v4.0_)?(?<version>[^_]+)_(?<culture>[^_]+)?_(?<publicKey>[^_]+)");
                        if (!match.Success)
                            continue;
                        var culture = match.Groups["culture"].Value;
                        if (string.IsNullOrEmpty(culture))
                            culture = "neutral";
                        yield return AssemblyNameReference.Parse(name[0] + ", Version=" + match.Groups["version"].Value + ", Culture=" + culture + ", PublicKeyToken=" + match.Groups["publicKey"].Value);
                    }
                }
            }
        }

        #endregion
    }
}