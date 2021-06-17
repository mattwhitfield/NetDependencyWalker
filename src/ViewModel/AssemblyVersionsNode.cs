namespace NetDependencyWalker.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NetDependencyWalker.Walking.MonoCecil;
    using GalaSoft.MvvmLight;

    public class AssemblyVersionsNode : ViewModelBase
    {
        public string Name { get; }
        public string LoadedVersion { get; }
        public IList<string> Versions { get; }

        public bool MajorVersionMismatched { get; }
        public string MismatchDescription { get; }
        public AssemblyVersionsNode(string name, Version loadedVersion, IList<Version> versions)
        {
            Name = name;
            LoadedVersion = loadedVersion.ToString();
            var validVersions = versions.Where(x => !UniversalAssemblyResolver.IsZeroOrAllOnes(x)).ToList();
            Versions = validVersions.Select(x => x.ToString()).OrderBy(x => x).ToList();
            var majorVersions = new HashSet<int>(validVersions.Select(x => x.Major));
            MajorVersionMismatched = majorVersions.Count > 1;
            MismatchDescription = MajorVersionMismatched ?
                "The references to " + Name + " specify multiple major versions (" + majorVersions.OrderBy(x => x).Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y) + ")" :
                "All of the references to " + Name + " specify the same major version (" + majorVersions.First() + ")";
        }
    }
}