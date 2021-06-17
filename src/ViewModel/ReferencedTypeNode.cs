using System.Collections.Generic;

namespace NetDependencyWalker.ViewModel
{
    using GalaSoft.MvvmLight;

    public class ReferencedTypeNode : ViewModelBase
    {
        public ReferencedTypeNode(string ns, string className, bool? isIncoming, string hint)
        {
            Namespace = ns;
            ClassName = className;
            IsIncoming = isIncoming;
            Initial = ns + ".";
            Secondary = className;
            Hint = hint;
        }

        public ReferencedTypeNode(ReferencingMemberType memberType, string memberName, string hint)
        {
            MemberType = memberType;
            MemberName = memberName;
            Initial = memberType + ": ";
            Secondary = memberName;
            Hint = hint;
        }

        public string Hint { get; }
        public string Namespace { get; }
        public string ClassName { get; }
        public bool? IsIncoming { get; }
        public string Initial { get; }
        public string Secondary { get; }
        public ReferencingMemberType MemberType { get; }
        public string MemberName { get; }

        public List<ReferencedTypeNode> Children { get; } = new List<ReferencedTypeNode>();
    }
}
