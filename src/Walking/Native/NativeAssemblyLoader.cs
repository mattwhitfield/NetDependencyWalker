namespace NetDependencyWalker.Walking.Native
{
    using System;
    using System.Reflection;
    using System.Runtime.Loader;

    public class NativeAssemblyLoader : AssemblyLoader
    {
        public Func<AssemblyLoadContext, string, Assembly> Load { get; }

        public NativeAssemblyLoader(string name, Version version, Func<AssemblyLoadContext, string, Assembly> load) : base(name, version)
        {
            Load = load;
        }
    }
}