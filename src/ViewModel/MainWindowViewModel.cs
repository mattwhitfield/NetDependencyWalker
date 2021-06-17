namespace NetDependencyWalker.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Data;
    using NetDependencyWalker.Walking;
    using NetDependencyWalker.Walking.MonoCecil;
    using NetDependencyWalker.Walking.Native;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;

    class MainWindowViewModel : ViewModelBase
    {
        private readonly Action<Action> _dispatch;

        public MainWindowViewModel(Action<Action> dispatch)
        {
            _dispatch = dispatch;
            RefreshCommand = new RelayCommand(Refresh);
            ToggleFilterCommand = new RelayCommand(() => FilterVisible = !FilterVisible);
            UseMonoWalkingCommand = new RelayCommand(() => UsingMonoWalking = true);
            UseNativeWalkingCommand = new RelayCommand(() => UsingMonoWalking = false);
            _selectedInspectionOrdering = AvailableInspectionOrderings.First();
        }

        private bool _usingMonoWalking = true;

        public bool UsingMonoWalking
        {
            get => _usingMonoWalking;

            set
            {
                if (_usingMonoWalking == value)
                {
                    return;
                }

                _usingMonoWalking = value;
                RaisePropertyChanged(nameof(UsingMonoWalking));
                RaisePropertyChanged(nameof(UsingNativeWalking));
                Refresh();
            }
        }

        public bool UsingNativeWalking => !UsingMonoWalking;

        private bool _filterVisible;

        public bool FilterVisible
        {
            get => _filterVisible;

            set
            {
                if (_filterVisible == value)
                {
                    return;
                }

                _filterVisible = value;
                if (!_filterVisible)
                {
                    FilterText = string.Empty;
                }
                RaisePropertyChanged(nameof(FilterVisible));
            }
        }

        private string _filterText;

        public string FilterText
        {
            get => _filterText;

            set
            {
                if (_filterText == value)
                {
                    return;
                }

                _filterText = value;
                if (_set != null)
                {
                    AddToDisplay(_set, -1);
                }
                RaisePropertyChanged(nameof(FilterText));
            }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand ToggleFilterCommand { get; }
        public RelayCommand UseMonoWalkingCommand { get; }
        public RelayCommand UseNativeWalkingCommand { get; }
        
        private string _rootFileName;

        public string RootFileNameOnly => Path.GetFileName(RootFileName);
        public string RootFileName
        {
            get => _rootFileName;

            set
            {
                _set = null;

                if (_rootFileName == value)
                {
                    return;
                }

                _rootFileName = value;

                Refresh();

                RaisePropertyChanged(nameof(RootFileName));
                RaisePropertyChanged(nameof(RootFileNameOnly));
            }
        }

        private string _rootTargetFramework;

        public bool RootTargetFrameworkKnown => !string.IsNullOrWhiteSpace(RootTargetFramework);

        public string RootTargetFramework
        {
            get => _rootTargetFramework;

            set
            {
                if (_rootTargetFramework == value)
                {
                    return;
                }

                _rootTargetFramework = value;
                RaisePropertyChanged(nameof(RootTargetFramework));
                RaisePropertyChanged(nameof(RootTargetFrameworkKnown));
            }
        }

        private void Refresh()
        {
            LoadingPrompt = "Loading...";
            Clear();

            HasFile = !string.IsNullOrWhiteSpace(_rootFileName) && File.Exists(_rootFileName);
            if (HasFile)
            {
                ScanInThread(_rootFileName);
            }
        }

        private bool _hasFile;

        public bool WaitingForFile => !HasFile;
        public bool HasFile
        {
            get => _hasFile;

            set
            {
                if (_hasFile == value)
                {
                    return;
                }

                _hasFile = value;
                RaisePropertyChanged(nameof(HasFile));
                RaisePropertyChanged(nameof(WaitingForFile));
            }
        }

        private bool _isBusy;
        private AssemblySet _set;

        public bool IsBusy
        {
            get => _isBusy;

            set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy = value;
                RaisePropertyChanged(nameof(IsBusy));
            }
        }

        private bool _isFiltered;

        public bool IsFiltered
        {
            get => _isFiltered;

            set
            {
                if (_isFiltered == value)
                {
                    return;
                }

                _isFiltered = value;
                RaisePropertyChanged(nameof(IsFiltered));
            }
        }

        private void ScanInThread(string rootFileName)
        {
            IsBusy = true;
            Task.Factory.StartNew(() => Scan(rootFileName));
        }

        private void Scan(string rootFileName)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            IWalker walker;
            if (UsingMonoWalking)
            {
                walker = new MonoCecilWalker();
            }
            else
            {
                walker = new NativeWalker();
            }

            AssemblySet set;
            try
            {
                set = walker.WalkFrom(rootFileName);
            }
            catch (Exception e)
            {
                sw.Stop();
                _dispatch(() => SetDisplayToLoadError(e));
                return;
            }

            _set = set;
            sw.Stop();
            _dispatch(() => AddToDisplay(set, sw.ElapsedMilliseconds));
        }

        private string _loadingPrompt;

        public string LoadingPrompt
        {
            get => _loadingPrompt;

            set
            {
                if (_loadingPrompt == value)
                {
                    return;
                }

                _loadingPrompt = value;
                RaisePropertyChanged(nameof(LoadingPrompt));
            }
        }

        private string _loadErrorsCount;

        public string LoadErrorsCount
        {
            get => _loadErrorsCount;

            set
            {
                if (_loadErrorsCount == value)
                {
                    return;
                }

                _loadErrorsCount = value;
                RaisePropertyChanged(nameof(LoadErrorsCount));
            }
        }

        private string _referenceListCount;

        public string ReferenceListCount
        {
            get => _referenceListCount;

            set
            {
                if (_referenceListCount == value)
                {
                    return;
                }

                _referenceListCount = value;
                RaisePropertyChanged(nameof(ReferenceListCount));
            }
        }

        private string _multipleVersionsCount;

        public string MultipleVersionsCount
        {
            get => _multipleVersionsCount;

            set
            {
                if (_multipleVersionsCount == value)
                {
                    return;
                }

                _multipleVersionsCount = value;
                RaisePropertyChanged(nameof(MultipleVersionsCount));
            }
        }

        private string _loadExceptionType;

        public string LoadExceptionType
        {
            get { return _loadExceptionType; }

            set
            {
                if (_loadExceptionType == value)
                {
                    return;
                }

                _loadExceptionType = value;
                RaisePropertyChanged(nameof(LoadExceptionType));
            }
        }

        private string _loadExceptionMessage;

        public string LoadExceptionMessage
        {
            get { return _loadExceptionMessage; }

            set
            {
                if (_loadExceptionMessage == value)
                {
                    return;
                }

                _loadExceptionMessage = value;
                RaisePropertyChanged(nameof(LoadExceptionMessage));
            }
        }

        private bool _hasLoadException;

        public bool HasLoadException
        {
            get { return _hasLoadException; }

            set
            {
                if (_hasLoadException == value)
                {
                    return;
                }

                _hasLoadException = value;
                RaisePropertyChanged(nameof(HasLoadException));
                RaisePropertyChanged(nameof(NoLoadException));
            }
        }

        public bool NoLoadException => !HasLoadException;

        private void SetDisplayToLoadError(Exception e)
        {
            Clear();

            LoadingPrompt = "Load failed.";
            IsBusy = false;
            HasFile = false;
            LoadExceptionType = e.GetType().Name;
            LoadExceptionMessage = e.Message;
            HasLoadException = true;
        }

        private void AddToDisplay(AssemblySet set, long elapsedMilliseconds)
        {
            Clear();

            Func<string, bool> filter = _ => true;
            IsFiltered = !string.IsNullOrWhiteSpace(_filterText);
            if (IsFiltered)
            {
                filter = itemName => itemName.Contains(_filterText, StringComparison.OrdinalIgnoreCase);
            }

            var forwardRootNodes = new List<ForwardAssemblyNode>();
            var reverseRootNodes = new List<ReverseAssemblyNode>();
            var referencedAssemblies = new List<ReferenceNode>();
            var multipleVersionNodes = new List<AssemblyVersionsNode>();
            var errorRootNodes = new List<AssemblyErrorNode>();

            forwardRootNodes.Add(new ForwardAssemblyNode(set, set.Root, filter, null, null));
            foreach (var assemblyDetail in set.AssembliesByName.Values.Where(x => filter(x.Name)).OrderBy(x => x.Name))
            {
                if (!assemblyDetail.HasLoadingErrors)
                {
                    reverseRootNodes.Add(new ReverseAssemblyNode(set, assemblyDetail, filter, null, null));
                    referencedAssemblies.Add(new ReferenceNode(assemblyDetail.Name, assemblyDetail.Path, assemblyDetail.LoadedVersion.ToString(), assemblyDetail.IdentityName, assemblyDetail.ReferencedAssemblies));

                    if (assemblyDetail.Versions.Count > 1)
                    {
                        multipleVersionNodes.Add(new AssemblyVersionsNode(assemblyDetail.Name, assemblyDetail.LoadedVersion, assemblyDetail.Versions));
                    }
                }
                else
                {
                    errorRootNodes.Add(new AssemblyErrorNode(assemblyDetail.Name, assemblyDetail.Errors));
                }
            }

            RootTargetFramework = set.TargetFrameworkId;
            ReferenceListCount = referencedAssemblies.Count > 0 ? $" ({referencedAssemblies.Count})" : string.Empty;
            MultipleVersionsCount = multipleVersionNodes.Count > 0 ? $" ({multipleVersionNodes.Count})" : string.Empty;
            LoadErrorsCount = errorRootNodes.Count > 0 ? $" ({errorRootNodes.Count})" : string.Empty;

            if (elapsedMilliseconds > 0)
            {
                LoadingPrompt = $"Processed {set.ProcessedReferenceCount} references in {elapsedMilliseconds}ms.";
            }

            IsBusy = false;

            forwardRootNodes.ForEach(ForwardRootNodes.Add);
            reverseRootNodes.ForEach(ReverseRootNodes.Add);
            referencedAssemblies.ForEach(ReferencedAssemblies.Add);
            multipleVersionNodes.ForEach(MultipleVersionNodes.Add);
            errorRootNodes.ForEach(ErrorRootNodes.Add);

            SelectedInspectionSource = ReferencedAssemblies.FirstOrDefault(x => x.IdentityName == set.Root.IdentityName);
        }

        private void Clear()
        {
            ReferencedAssemblies.Clear();
            ForwardRootNodes.Clear();
            ReverseRootNodes.Clear();
            ErrorRootNodes.Clear();
            MultipleVersionNodes.Clear();
            LoadErrorsCount = string.Empty;
            MultipleVersionsCount = string.Empty;
            ReferenceListCount = string.Empty;
            RootTargetFramework = string.Empty;
        }

        public ObservableCollection<ReferenceNode> ReferencedAssemblies { get; } = new ObservableCollection<ReferenceNode>();

        public ObservableCollection<AssemblyNode> ForwardRootNodes { get; } = new ObservableCollection<AssemblyNode>();
        
        public ObservableCollection<AssemblyNode> ReverseRootNodes { get; } = new ObservableCollection<AssemblyNode>();

        public ObservableCollection<AssemblyVersionsNode> MultipleVersionNodes { get; } = new ObservableCollection<AssemblyVersionsNode>();

        public ObservableCollection<AssemblyErrorNode> ErrorRootNodes { get; } = new ObservableCollection<AssemblyErrorNode>();

        private string _referencedAssembliesSearchTerm;

        public string ReferencedAssembliesSearchTerm
        {
            get { return _referencedAssembliesSearchTerm; }

            set
            {
                if (_referencedAssembliesSearchTerm == value)
                {
                    return;
                }

                _referencedAssembliesSearchTerm = value;

                var source = CollectionViewSource.GetDefaultView(ReferencedAssemblies);

                if (source.Filter == null)
                {
                    source.Filter = FilterReferenceNode;
                }

                source.Refresh();

                RaisePropertyChanged(nameof(ReferencedAssembliesSearchTerm));
            }
        }

        public bool FilterReferenceNode(object filterableEntity)
        {
            var node = filterableEntity as ReferenceNode;

            if (node == null)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(_referencedAssembliesSearchTerm) || node.Name.IndexOf(_referencedAssembliesSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string _reverseNodesSearchTerm;

        public string ReverseNodesSearchTerm
        {
            get { return _reverseNodesSearchTerm; }

            set
            {
                if (_reverseNodesSearchTerm == value)
                {
                    return;
                }

                _reverseNodesSearchTerm = value;

                var source = CollectionViewSource.GetDefaultView(ReverseRootNodes);

                if (source.Filter == null)
                {
                    source.Filter = FilterReverseNode;
                }

                source.Refresh();

                RaisePropertyChanged(nameof(ReverseNodesSearchTerm));
            }
        }

        public bool FilterReverseNode(object filterableEntity)
        {
            var node = filterableEntity as AssemblyNode;

            if (node == null)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(_reverseNodesSearchTerm) || node.Name.IndexOf(_reverseNodesSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private ReferenceNode _selectedInspectionSource;

        public ReferenceNode SelectedInspectionSource
        {
            get => _selectedInspectionSource;

            set
            {
                if (_selectedInspectionSource == value)
                {
                    return;
                }

                _selectedInspectionSource = value;
                RaisePropertyChanged(nameof(SelectedInspectionSource));

                AvailableInspectionTargets.Clear();
                ReferencedTypes.Clear();
                NoDirectReferencesFound = false;

                if (_selectedInspectionSource != null)
                {
                    foreach (var referencedAssembly in _selectedInspectionSource.ReferencedAssemblies.OrderBy(x => x.Name))
                    {
                        var node = ReferencedAssemblies.FirstOrDefault(x => x.IdentityName == referencedAssembly.IdentityName);
                        if (node != null)
                        {
                            AvailableInspectionTargets.Add(node);
                        }
                        else
                        {
                            node = ReferencedAssemblies.FirstOrDefault(x => x.Name == referencedAssembly.Name);
                            if (node != null)
                            {
                                AvailableInspectionTargets.Add(node);
                            }
                        }
                    }
                }
            }
        }

        public bool NoInspectionTargetSelected => SelectedInspectionTarget == null;

        private ReferenceNode _selectedInspectionTarget;

        public ReferenceNode SelectedInspectionTarget
        {
            get => _selectedInspectionTarget;

            set
            {
                if (_selectedInspectionTarget == value)
                {
                    return;
                }

                _selectedInspectionTarget = value;
                RaisePropertyChanged(nameof(SelectedInspectionTarget));
                RaisePropertyChanged(nameof(NoInspectionTargetSelected));

                CalculateReferencedTypes();
            }
        }

        private void CalculateReferencedTypes()
        {
            ReferencedTypes.Clear();
            if (SelectedInspectionSource == null || SelectedInspectionTarget == null || SelectedInspectionOrdering == null)
            {
                return;
            }

            var inspector = new TypeInspector(SelectedInspectionOrdering.OrderingType);

            foreach (var node in inspector.CalculateReferencedTypes(SelectedInspectionSource.Path, SelectedInspectionTarget.Path))
            {
                ReferencedTypes.Add(node);
            }

            NoDirectReferencesFound = ReferencedTypes.Count == 0;
        }

        public ObservableCollection<ReferenceNode> AvailableInspectionTargets { get; } = new ObservableCollection<ReferenceNode>();

        public ObservableCollection<ReferencedTypeNode> ReferencedTypes { get; } = new ObservableCollection<ReferencedTypeNode>();

        private bool _noDirectReferencesFound;

        public bool NoDirectReferencesFound
        {
            get => _noDirectReferencesFound;

            set
            {
                if (_noDirectReferencesFound == value)
                {
                    return;
                }

                _noDirectReferencesFound = value;
                RaisePropertyChanged(nameof(NoDirectReferencesFound));
            }
        }

        public ObservableCollection<OrderingTypeNode> AvailableInspectionOrderings { get; } = new ObservableCollection<OrderingTypeNode>(new []
        {
            new OrderingTypeNode(OrderingType.ReferencedThenReferencingThenMember),
            new OrderingTypeNode(OrderingType.ReferencingThenMemberThenReferenced),
            new OrderingTypeNode(OrderingType.ReferencingThenReferenced)
        });

        private OrderingTypeNode _selectedInspectionOrdering;

        public OrderingTypeNode SelectedInspectionOrdering
        {
            get => _selectedInspectionOrdering;

            set
            {
                if (_selectedInspectionOrdering == value)
                {
                    return;
                }

                _selectedInspectionOrdering = value;
                RaisePropertyChanged(nameof(SelectedInspectionOrdering));

                CalculateReferencedTypes();
            }
        }

        private bool _listOnlyMajorVersionMismatches;

        public bool ListOnlyMajorVersionMismatches
        {
            get { return _listOnlyMajorVersionMismatches; }

            set
            {
                if (_listOnlyMajorVersionMismatches == value)
                {
                    return;
                }

                _listOnlyMajorVersionMismatches = value;

                var source = CollectionViewSource.GetDefaultView(MultipleVersionNodes);

                if (source.Filter == null)
                {
                    source.Filter = FilterAssemblyVersionNode;
                }

                source.Refresh();

                RaisePropertyChanged(nameof(ListOnlyMajorVersionMismatches));
            }
        }

        public bool FilterAssemblyVersionNode(object filterableEntity)
        {
            var node = filterableEntity as AssemblyVersionsNode;

            if (node == null)
            {
                return false;
            }

            return node.MajorVersionMismatched || !_listOnlyMajorVersionMismatches;
        }
    }
}
