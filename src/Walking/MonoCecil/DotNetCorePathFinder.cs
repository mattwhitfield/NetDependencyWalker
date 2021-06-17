// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetDependencyWalker.Walking.MonoCecil
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NetDependencyWalker.Walking.MonoCecil.DependencyReader;

    public class DotNetCorePathFinder
	{
        private static readonly string[] LookupPaths = {
			 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages")
		};

        private static readonly string[] RuntimePacks = {
			"Microsoft.NETCore.App",
			"Microsoft.WindowsDesktop.App",
			"Microsoft.AspNetCore.App",
			"Microsoft.AspNetCore.All"
		};

        private readonly List<string> _searchPaths = new List<string>();
        private readonly List<string> _packageBasePaths = new List<string>();
        private readonly Version _targetFrameworkVersion;
        private readonly string _dotnetBasePath = FindDotNetExeDirectory();

		public DotNetCorePathFinder(TargetFrameworkIdentifier targetFramework, Version targetFrameworkVersion)
		{
			_targetFrameworkVersion = targetFrameworkVersion;

			if (targetFramework == TargetFrameworkIdentifier.NetStandard)
			{
				// .NET Standard 2.1 is implemented by .NET Core 3.0 or higher
				if (targetFrameworkVersion.Major == 2 && targetFrameworkVersion.Minor == 1)
				{
					_targetFrameworkVersion = new Version(3, 0, 0);
				}
			}
		}

		public DotNetCorePathFinder(string parentAssemblyFileName, string targetFrameworkIdString, TargetFrameworkIdentifier targetFramework, Version targetFrameworkVersion)
			: this(targetFramework, targetFrameworkVersion)
		{
            var assemblyName = Path.GetFileNameWithoutExtension(parentAssemblyFileName);
			var basePath = Path.GetDirectoryName(parentAssemblyFileName);

            if (string.IsNullOrWhiteSpace(basePath))
            {
                return;
            }

			_searchPaths.Add(basePath);

			var depsJsonFileName = Path.Combine(basePath, $"{assemblyName}.deps.json");
			if (File.Exists(depsJsonFileName))
            {
                var packages = LoadPackageInfos(depsJsonFileName).ToArray();

                foreach (var path in LookupPaths)
				{
					foreach (var p in packages)
                    {
                        var runtimeAssetGroup = (p.RuntimeAssemblyGroups.FirstOrDefault(x => x.Runtime == targetFrameworkIdString) ?? p.RuntimeAssemblyGroups.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Runtime)));
                        if (runtimeAssetGroup != null)
                        {
                            foreach (var item in runtimeAssetGroup.AssetPaths)
                            {
                                var itemPath = Path.GetDirectoryName(item);
                                if (!string.IsNullOrWhiteSpace(itemPath))
                                {
                                    var fullPath = Path.Combine(path, p.Name, p.Version, itemPath).ToLowerInvariant();
                                    if (Directory.Exists(fullPath))
                                        _packageBasePaths.Add(fullPath);
                                }
                            }
                        }
                    }
				}
            }
		}

		public void AddSearchDirectory(string path)
		{
			_searchPaths.Add(path);
		}

		public void RemoveSearchDirectory(string path)
		{
			_searchPaths.Remove(path);
		}

		public string TryResolveDotNetCore(AssemblyName name)
		{
			foreach (var basePath in _searchPaths.Concat(_packageBasePaths))
			{
				if (File.Exists(Path.Combine(basePath, name.Name + ".dll")))
				{
					return Path.Combine(basePath, name.Name + ".dll");
				}
				else if (File.Exists(Path.Combine(basePath, name.Name + ".exe")))
				{
					return Path.Combine(basePath, name.Name + ".exe");
				}
			}

			return FallbackToDotNetSharedDirectory(name);
		}

		internal string GetReferenceAssemblyPath(string targetFramework)
		{
			var (tfi, version) = UniversalAssemblyResolver.ParseTargetFramework(targetFramework);
			string identifier, identifierExt;
			switch (tfi)
			{
				case TargetFrameworkIdentifier.NetCoreApp:
					identifier = "Microsoft.NETCore.App";
					identifierExt = "netcoreapp" + version.Major + "." + version.Minor;
					break;
				case TargetFrameworkIdentifier.NetStandard:
					identifier = "NETStandard.Library";
					identifierExt = "netstandard" + version.Major + "." + version.Minor;
					break;
				default:
					throw new NotSupportedException();
			}
			return Path.Combine(_dotnetBasePath, "packs", identifier + ".Ref", version.ToString(), "ref", identifierExt);
		}

        private static IEnumerable<RuntimeLibrary> LoadPackageInfos(string depsJsonFileName)
		{
			var reader = new DependencyContextJObjectReader();
            using var stream = new FileStream(depsJsonFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var context = reader.Read(stream);
            return context.RuntimeLibraries;
        }

        private string FallbackToDotNetSharedDirectory(AssemblyName name)
		{
            if (_dotnetBasePath == null)
            {
                return null;
            }

            foreach (var basePath in RuntimePacks.Select(pack => Path.Combine(_dotnetBasePath, "shared", pack)))
			{
				if (!Directory.Exists(basePath))
					continue;
				var closestVersion = GetClosestVersionFolder(basePath, _targetFrameworkVersion);
				if (File.Exists(Path.Combine(basePath, closestVersion, name.Name + ".dll")))
				{
					return Path.Combine(basePath, closestVersion, name.Name + ".dll");
				}

                if (File.Exists(Path.Combine(basePath, closestVersion, name.Name + ".exe")))
                {
                    return Path.Combine(basePath, closestVersion, name.Name + ".exe");
                }
            }

			return null;
		}

        private static string GetClosestVersionFolder(string basePath, Version version)
		{
			var foundVersions = new DirectoryInfo(basePath).GetDirectories()
				.Select(d => ConvertToVersion(d.Name))
				.Where(v => v.version != null);
			foreach (var folder in foundVersions.OrderBy(v => v.Item1))
			{
				if (folder.version >= version)
					return folder.directoryName;
			}
			return version.ToString();
		}

		internal static (Version version, string directoryName) ConvertToVersion(string name)
		{
			string RemoveTrailingVersionInfo()
			{
				var shortName = name;
				var dashIndex = shortName.IndexOf('-');
				if (dashIndex > 0)
				{
					shortName = shortName.Remove(dashIndex);
				}
				return shortName;
			}

			try
			{
				return (new Version(RemoveTrailingVersionInfo()), name);
			}
			catch (Exception ex)
			{
				Trace.TraceWarning(ex.ToString());
				return (null, null);
			}
		}

		public static string FindDotNetExeDirectory()
		{
			var dotnetExeName = (Environment.OSVersion.Platform == PlatformID.Unix) ? "dotnet" : "dotnet.exe";
            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            foreach (var item in path.Split(Path.PathSeparator))
			{
				try
				{
					var fileName = Path.Combine(item, dotnetExeName);
					if (!File.Exists(fileName))
						continue;
					return Path.GetDirectoryName(fileName);
				}
				catch (ArgumentException) { }
			}
			return null;
		}
	}
}
