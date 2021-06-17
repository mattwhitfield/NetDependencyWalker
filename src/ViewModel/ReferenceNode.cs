namespace NetDependencyWalker.ViewModel
{
    using System.Collections.Generic;
    using NetDependencyWalker.Walking;
    using GalaSoft.MvvmLight;

    public class ReferenceNode : ViewModelBase
    {
        public string Name { get; }
        public string Path { get; }
        public string Version { get; }
        public string IdentityName { get; }
        public List<ReferencedAssembly> ReferencedAssemblies { get; }

        public ReferenceNode(string name, string path, string version, string identityName, List<ReferencedAssembly> referencedAssemblies)
        {
            Name = name;
            Path = path;
            Version = version;
            IdentityName = identityName;
            ReferencedAssemblies = referencedAssemblies;
        }

        public override string ToString()
        {
            return IdentityName;
        }
    }
}