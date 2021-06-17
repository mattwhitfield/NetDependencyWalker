namespace NetDependencyWalker.Walking
{
    using System;

    public abstract class AssemblyLoader
    {
        public string Name { get; }
        public Version Version { get; }

        protected AssemblyLoader(string name, Version version)
        {
            Name = name;
            Version = version;
        }
    }
}