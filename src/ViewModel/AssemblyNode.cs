namespace NetDependencyWalker.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using NetDependencyWalker.Walking;
    using GalaSoft.MvvmLight;

    public abstract class AssemblyNode : ViewModelBase
    {
        private static readonly AssemblyNode Default = new InertAssemblyNode();
        private readonly Func<AssemblyNode, AssemblySet, AssemblyDetail, Func<string, bool>, IEnumerable<AssemblyNode>> _childItemsLoader;

        private readonly AssemblyDetail _detail;
        private readonly Func<string, bool> _filter;

        private readonly AssemblySet _set;

        private bool _childrenLoaded;

        private bool _isExpanded;

        protected AssemblyNode(AssemblySet set, AssemblyDetail detail, Func<string, bool> filter, Func<AssemblyNode, AssemblySet, AssemblyDetail, Func<string, bool>, IEnumerable<AssemblyNode>> childItemsLoader, Func<AssemblySet, AssemblyDetail, Func<string, bool>, bool> hasChildren, Version loadedVersion, Version requestedVersion)
        {
            _childItemsLoader = childItemsLoader;
            _set = set;
            _detail = detail;
            _filter = filter ?? (_ => true);
            ChildNodes = new ObservableCollection<AssemblyNode>();
            if (hasChildren(set, detail, _filter))
            {
                ChildNodes.Add(Default);
            }

            if (loadedVersion != null && requestedVersion != null)
            {
                VersionMatchingSpecified = true;
                VersionMatched = loadedVersion.Equals(requestedVersion);
                if (!VersionMatched)
                {
                    if (loadedVersion.Major == requestedVersion.Major)
                    {
                        if (loadedVersion.Minor == requestedVersion.Minor)
                        {
                            PatchVersionMismatched = true;
                            if (loadedVersion.Build == requestedVersion.Build)
                            {
                                VersionMismatchPrompt = "Referenced version was " + requestedVersion + " and loaded version was " + loadedVersion + " (revision version mismatch)";
                            }
                            else
                            {
                                VersionMismatchPrompt = "Referenced version was " + requestedVersion + " and loaded version was " + loadedVersion + " (patch/build version mismatch)";
                            }
                        }
                        else
                        {
                            MinorVersionMismatched = true;
                            VersionMismatchPrompt = "Referenced version was " + requestedVersion + " and loaded version was " + loadedVersion + " (minor version mismatch)";
                        }
                    }
                    else
                    {
                        MajorVersionMismatched = true;
                        VersionMismatchPrompt = "Referenced version was " + requestedVersion + " and loaded version was " + loadedVersion + " (major version mismatch)";
                    }
                }
                else
                {
                    VersionMismatchPrompt = "Referenced and loaded versions matched";
                }
            }
            else
            {
                VersionMismatchPrompt = null;
            }

            Name = detail.Name;
            Text = detail.IdentityName;
            Path = detail.Path;
        }

        private AssemblyNode()
        {
            ChildNodes = new ObservableCollection<AssemblyNode>();
            Text = "Loading...";
            _childItemsLoader = (_, __, ___, ____) => Enumerable.Empty<AssemblyNode>();
        }

        public ObservableCollection<AssemblyNode> ChildNodes { get; }

        public string Path { get; }

        public bool VersionMatched { get; }

        public bool PatchVersionMismatched { get; }

        public bool MinorVersionMismatched { get; }

        public bool MajorVersionMismatched { get; }

        public string VersionMismatchPrompt { get; }

        public bool VersionMatchingSpecified { get; }

        public bool IsExpanded
        {
            get => _isExpanded;

            set
            {
                if (_isExpanded == value)
                {
                    return;
                }

                _isExpanded = value;

                if (_isExpanded && !_childrenLoaded)
                {
                    ChildNodes.Clear();
                    foreach (var child in _childItemsLoader(this, _set, _detail, _filter))
                    {
                        ChildNodes.Add(child);
                    }

                    _childrenLoaded = true;
                }

                RaisePropertyChanged(nameof(IsExpanded));
            }
        }

        public string Name { get; }
        public string Text { get; }

        private class InertAssemblyNode : AssemblyNode
        {
        }
    }
}