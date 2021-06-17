namespace NetDependencyWalker.Walking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class AssemblyDetail
    {
        public string Name { get; }
        public string Path { get; }
        public Version LoadedVersion { get; }
        public List<Version> Versions { get; } = new List<Version>();
        public bool HasLoadingErrors { get; set; }
        public List<ReferencedAssembly> ReferencedAssemblies { get; } = new List<ReferencedAssembly>();
        public List<string> Errors { get; } = new List<string>();
        public string IdentityName { get; }

        public AssemblyDetail(string name, string path, Version version)
        {
            Name = name;
            Path = path;
            if (version != null)
            {
                Versions.Add(new Version(version.ToString()));
                LoadedVersion = version;
                IdentityName = $"{name} ({version})";
            }
            else
            {
                IdentityName = name;
            }
        }

        public void AddVersion(Version version)
        {
            if (version != null && !Versions.Any(x => x.Equals(version)))
            {
                Versions.Add(new Version(version.ToString()));
            }
        }
    }
}