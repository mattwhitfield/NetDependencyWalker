namespace NetDependencyWalker.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NetDependencyWalker.Walking;

    public class ForwardAssemblyNode : AssemblyNode
    {
        public ForwardAssemblyNode Parent { get; }

        public ForwardAssemblyNode(AssemblySet set, AssemblyDetail detail, Func<string, bool> filter, Version requestedVersion, ForwardAssemblyNode parent) : base(set, detail, filter, GetChildren, HasChildren, detail.LoadedVersion, requestedVersion)
        {
            Parent = parent;
        }

        private static bool HasChildren(AssemblySet set, AssemblyDetail detail, Func<string, bool> filter)
        {
            foreach (var referencedAssembly in detail.ReferencedAssemblies.Where(x => filter(x.Name)))
            {
                if (set.AssembliesByName.TryGetValue(referencedAssembly.IdentityName ?? string.Empty, out var assembly))
                {
                    return true;
                }

                assembly = set.AssembliesByName.FirstOrDefault(x => x.Value.Name == referencedAssembly.Name).Value;
                if (assembly != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<AssemblyNode> GetChildren(AssemblyNode startingNode, AssemblySet set, AssemblyDetail detail, Func<string, bool> filter)
        {
            foreach (var referencedAssembly in detail.ReferencedAssemblies.Where(x => filter(x.Name)).OrderBy(x => x.Name))
            {
                if (set.AssembliesByName.TryGetValue(referencedAssembly.IdentityName ?? string.Empty, out var assembly))
                {
                    yield return new ForwardAssemblyNode(set, assembly, filter, referencedAssembly.Version, startingNode as ForwardAssemblyNode);
                }
                else
                {
                    assembly = set.AssembliesByName.FirstOrDefault(x => x.Value.Name == referencedAssembly.Name).Value;
                    if (assembly != null)
                    {
                        yield return new ForwardAssemblyNode(set, assembly, filter, referencedAssembly.Version, startingNode as ForwardAssemblyNode);
                    }
                }
            }
        }
    }
}