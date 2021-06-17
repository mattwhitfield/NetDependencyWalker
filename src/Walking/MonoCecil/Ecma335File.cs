// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetDependencyWalker.Walking.MonoCecil
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;
    using System.Text.RegularExpressions;

    public class Ecma335File
    {
        public string FileName { get; }

        public Ecma335File(string fileName)
        {
            FileName = fileName;
        }

        public Ecma335File Read()
        {
            using var fileStream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var reader = new PEReader(fileStream, PEStreamOptions.PrefetchEntireImage);

            if (!reader.HasMetadata)
            {
                LoadError = "Portable Executable has no managed metadata.";
            }

            var metadataReader = reader.GetMetadataReader();
            MetadataVersion = metadataReader.MetadataVersion;
            Name = metadataReader.GetAssemblyDefinition().GetAssemblyName();
            TargetFrameworkId = DetectTargetFrameworkId(reader, FileName);

            foreach (var assemblyReference in metadataReader.AssemblyReferences)
            {
                var reference = metadataReader.GetAssemblyReference(assemblyReference);
                var assemblyName = reference.GetAssemblyName();
                ReferencedAssemblies.Add(assemblyName);
            }

            return this;
        }

        public string TargetFrameworkId { get; private set; }

        public List<AssemblyName> ReferencedAssemblies { get; } = new List<AssemblyName>();

        public AssemblyName Name { get; private set; }

        public string MetadataVersion { get; private set; }

        public string LoadError { get; private set; }

        
        public static EntityHandle GetAttributeType(CustomAttribute attribute, MetadataReader reader)
        {
            switch (attribute.Constructor.Kind)
            {
                case HandleKind.MethodDefinition:
                    var md = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                    return md.GetDeclaringType();
                case HandleKind.MemberReference:
                    var mr = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    return mr.Parent;
                default:
                    throw new BadImageFormatException("Unexpected token kind for attribute constructor: "
                                                      + attribute.Constructor.Kind);
            }
        }

        public static string GetFullTypeName(EntityHandle handle, MetadataReader reader)
        {
            if (handle.IsNil)
                throw new ArgumentNullException(nameof(handle));
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    return reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)handle).Name);
                case HandleKind.TypeReference:
                    return reader.GetString(reader.GetTypeReference((TypeReferenceHandle)handle).Name);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static readonly string PathPattern =
            @"(Reference Assemblies[/\\]Microsoft[/\\]Framework[/\\](?<type>.NETFramework)[/\\]v(?<version>[^/\\]+)[/\\])" +
            @"|((?<type>Microsoft\.NET)[/\\]assembly[/\\]GAC_(MSIL|32|64)[/\\])" +
            @"|((?<type>Microsoft\.NET)[/\\]Framework(64)?[/\\](?<version>[^/\\]+)[/\\])" +
            @"|(NuGetFallbackFolder[/\\](?<type>[^/\\]+)\\(?<version>[^/\\]+)([/\\].*)?[/\\]ref[/\\])" +
            @"|(shared[/\\](?<type>[^/\\]+)\\(?<version>[^/\\]+)([/\\].*)?[/\\])" +
            @"|(packs[/\\](?<type>[^/\\]+)\\(?<version>[^/\\]+)\\ref([/\\].*)?[/\\])";

        public static string DetectTargetFrameworkId(PEReader assembly, string assemblyPath = null)
		{
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));

			const string targetFrameworkAttributeName = "System.Runtime.Versioning.TargetFrameworkAttribute";
			var reader = assembly.GetMetadataReader();

			foreach (var h in reader.GetCustomAttributes(Handle.AssemblyDefinition))
			{
				try
				{
					var attribute = reader.GetCustomAttribute(h);
					if (GetFullTypeName(GetAttributeType(attribute, reader), reader) != targetFrameworkAttributeName)
						continue;
					var blobReader = reader.GetBlobReader(attribute.Value);
					if (blobReader.ReadUInt16() == 0x0001)
					{
						return blobReader.ReadSerializedString();
					}
				}
				catch (BadImageFormatException)
				{
					// ignore malformed attributes
				}
			}

			foreach (var h in reader.AssemblyReferences)
			{
				try
				{
					var r = reader.GetAssemblyReference(h);
					if (r.PublicKeyOrToken.IsNil)
						continue;
					string version;
					switch (reader.GetString(r.Name))
					{
						case "netstandard":
							version = r.Version.ToString(3);
							return $".NETStandard,Version=v{version}";
						case "System.Runtime":
							// System.Runtime.dll uses the following scheme:
							// 4.2.0 => .NET Core 2.0
							// 4.2.1 => .NET Core 2.1 / 3.0
							// 4.2.2 => .NET Core 3.1
							if (r.Version >= new Version(4, 2, 0))
							{
								version = "2.0";
								if (r.Version >= new Version(4, 2, 1))
								{
									version = "3.0";
								}
								if (r.Version >= new Version(4, 2, 2))
								{
									version = "3.1";
								}
								return $".NETCoreApp,Version=v{version}";
							}
							else
							{
								continue;
							}
						case "mscorlib":
							version = r.Version.ToString(2);
							return $".NETFramework,Version=v{version}";
					}
				}
				catch (BadImageFormatException)
				{
					// ignore malformed references
				}
			}

			// Optionally try to detect target version through assembly path as a fallback (use case: reference assemblies)
			if (assemblyPath != null)
			{
				/*
				 * Detected path patterns (examples):
				 * 
				 * - .NETFramework -> C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.1\mscorlib.dll
				 * - .NETCore      -> C:\Program Files\dotnet\sdk\NuGetFallbackFolder\microsoft.netcore.app\2.1.0\ref\netcoreapp2.1\System.Console.dll
				 * - .NETStandard  -> C:\Program Files\dotnet\sdk\NuGetFallbackFolder\netstandard.library\2.0.3\build\netstandard2.0\ref\netstandard.dll
				 */
				var pathMatch = Regex.Match(assemblyPath, PathPattern,
					RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
				if (pathMatch.Success)
				{
					var type = pathMatch.Groups["type"].Value;
					var version = pathMatch.Groups["version"].Value;
					if (string.IsNullOrEmpty(version))
						version = reader.MetadataVersion;

					if (type == "Microsoft.NET" || type == ".NETFramework")
					{
						return $".NETFramework,Version=v{version.TrimStart('v').Substring(0, 3)}";
					}
					else if (type.IndexOf("netcore", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return $".NETCoreApp,Version=v{version}";
					}
					else if (type.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return $".NETStandard,Version=v{version}";
					}
				}
				else
				{
					return $".NETFramework,Version={reader.MetadataVersion.Substring(0, 4)}";
				}
			}

			return string.Empty;
		}
    }
}
