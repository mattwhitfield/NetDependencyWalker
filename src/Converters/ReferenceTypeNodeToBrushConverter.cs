namespace NetDependencyWalker.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;
    using NetDependencyWalker.ViewModel;

    public class ReferencedTypeNodeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ReferencedTypeNode node)
            {
                switch (node.MemberType)
                {
                    default:
                        return DefaultBrush;
                    case ReferencingMemberType.Method:
                        return MethodBrush;
                    case ReferencingMemberType.Property:
                        return PropertyBrush;
                    case ReferencingMemberType.Field:
                        return FieldBrush;
                    case ReferencingMemberType.TypeHierarchy:
                        return TypeHierarchyBrush;
                }
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public Brush DefaultBrush { get; set; }

        public Brush MethodBrush { get; set; }

        public Brush PropertyBrush { get; set; }

        public Brush FieldBrush { get; set; }

        public Brush TypeHierarchyBrush { get; set; }
    }
}