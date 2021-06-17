namespace NetDependencyWalker.Walking
{
    using System;

    public class ReferencedAssembly
    {
        public ReferencedAssembly(string name, Version version)
        {
            Name = name;
            Version = new Version(version.ToString());
            IdentityName = name + " (" + Version + ")";
        }

        public string Name { get; }
        public Version Version { get; }
        public string IdentityName { get; }
    }
}