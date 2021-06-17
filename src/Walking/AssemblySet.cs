namespace NetDependencyWalker.Walking
{
    using System.Collections.Generic;

    public class AssemblySet
    {
        public AssemblySet(AssemblyDetail root, IDictionary<string, AssemblyDetail> assembliesByName, int processedReferenceCount, string targetFrameworkId)
        {
            Root = root;
            AssembliesByName = assembliesByName;
            ProcessedReferenceCount = processedReferenceCount;
            TargetFrameworkId = targetFrameworkId;
        }

        public IDictionary<string, AssemblyDetail> AssembliesByName { get; }
        public AssemblyDetail Root { get; }
        public int ProcessedReferenceCount { get; }
        public string TargetFrameworkId { get; }
    }
}