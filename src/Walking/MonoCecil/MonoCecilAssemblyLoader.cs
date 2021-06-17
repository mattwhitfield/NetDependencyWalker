namespace NetDependencyWalker.Walking.MonoCecil
{
    using System;

    public class MonoCecilAssemblyLoader : AssemblyLoader
    {
        public Func<Ecma335File> Load { get; }

        public MonoCecilAssemblyLoader(string name, Version version, Func<Ecma335File> load) : base(name, version)
        {
            Load = load;
        }
    }
}