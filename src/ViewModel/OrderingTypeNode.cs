namespace NetDependencyWalker.ViewModel
{
    public class OrderingTypeNode
    {
        public OrderingType OrderingType { get; }

        public OrderingTypeNode(OrderingType orderingType)
        {
            OrderingType = orderingType;
        }

        public override string ToString()
        {
            switch (OrderingType)
            {
                case OrderingType.ReferencedThenReferencingThenMember:
                    return "Referenced Type -> Referencing Type -> Member";
                case OrderingType.ReferencingThenMemberThenReferenced:
                    return "Referencing Type -> Member -> Referenced Type";
                default:
                    return "Referencing Type -> Referenced Type";
            }
        }
    }
}