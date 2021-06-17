namespace NetDependencyWalker.Walking.Native
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;

    class NativeWalkerAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly List<AssemblyDependencyResolver> _resolvers = new List<AssemblyDependencyResolver>();
        private readonly HashSet<string> _addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public NativeWalkerAssemblyLoadContext() : base(true)
        {
        }

        public void AddPath(string path)
        {
            if (_addedPaths.Add(path))
            {
                _resolvers.Add(new AssemblyDependencyResolver(path));
            }
        }

        protected override Assembly Load(AssemblyName name)
        {
            foreach (var resolver in _resolvers)
            {
                var assemblyPath = resolver.ResolveAssemblyToPath(name);
                if (assemblyPath != null)
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }

                assemblyPath = name.Name + ".dll";
                if (File.Exists(assemblyPath))
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }

                foreach (var path in _addedPaths)
                {
                    assemblyPath = Path.Join(path, name.Name + ".dll");
                    if (File.Exists(assemblyPath))
                    {
                        return LoadFromAssemblyPath(assemblyPath);
                    }
                }
            }

            return null;
        }
    }
}