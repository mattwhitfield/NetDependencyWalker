namespace NetDependencyWalker.ViewModel
{
    public enum OrderingType
    {
        ReferencedThenReferencingThenMember,
        ReferencingThenMemberThenReferenced,
        ReferencingThenReferenced
    }
}