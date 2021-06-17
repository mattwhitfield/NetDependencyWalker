namespace NetDependencyWalker.ViewModel
{
    using System.Collections.Generic;
    using GalaSoft.MvvmLight;

    public class AssemblyErrorNode : ViewModelBase
    {
        public string Name { get; }
        public IList<string> Errors { get; }

        public AssemblyErrorNode(string name, IList<string> errors)
        {
            Name = name;
            Errors = errors;
        }
    }
}