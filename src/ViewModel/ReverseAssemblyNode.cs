namespace NetDependencyWalker.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NetDependencyWalker.Walking;

    public class ReverseAssemblyNode : AssemblyNode
    {
        public ReverseAssemblyNode(AssemblySet set, AssemblyDetail detail, Func<string, bool> filter, Version loadedVersion, Version requestedVersion) : base(set, detail, filter, GetChildren, HasChildren, loadedVersion, requestedVersion)
        {
        }

        private static bool HasChildren(AssemblySet set, AssemblyDetail detail, Func<string, bool> filter)
        {
            foreach (var referencingAssembly in set.AssembliesByName.Values.Where(x => filter(x.Name)).OrderBy(x => x.IdentityName))
            {
                var versionMatchedSource = referencingAssembly.ReferencedAssemblies.FirstOrDefault(x => string.Equals(x.IdentityName, detail.IdentityName, StringComparison.OrdinalIgnoreCase));
                if (versionMatchedSource != null)
                {
                    return true;
                }

                var nameMatchedSource = referencingAssembly.ReferencedAssemblies.FirstOrDefault(x => string.Equals(x.Name, detail.Name, StringComparison.OrdinalIgnoreCase));
                if (nameMatchedSource != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<AssemblyNode> GetChildren(AssemblyNode parent, AssemblySet set, AssemblyDetail detail, Func<string, bool> filter)
        {
            foreach (var referencingAssembly in set.AssembliesByName.Values.Where(x => filter(x.Name)).OrderBy(x => x.IdentityName))
            {
                var versionMatchedSource = referencingAssembly.ReferencedAssemblies.FirstOrDefault(x => string.Equals(x.IdentityName, detail.IdentityName, StringComparison.OrdinalIgnoreCase));
                if (versionMatchedSource != null)
                {
                    yield return new ReverseAssemblyNode(set, referencingAssembly, filter, detail.LoadedVersion, versionMatchedSource.Version);
                }
                else
                {
                    var nameMatchedSource = referencingAssembly.ReferencedAssemblies.FirstOrDefault(x => string.Equals(x.Name, detail.Name, StringComparison.OrdinalIgnoreCase));
                    if (nameMatchedSource != null)
                    {
                        yield return new ReverseAssemblyNode(set, referencingAssembly, filter, detail.LoadedVersion, nameMatchedSource.Version);
                    }
                }
            }
        }
    }
}