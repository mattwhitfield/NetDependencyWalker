namespace NetDependencyWalker.Walking
{
    public interface IWalker
    {
        AssemblySet WalkFrom(string rootFileName);
    }
}