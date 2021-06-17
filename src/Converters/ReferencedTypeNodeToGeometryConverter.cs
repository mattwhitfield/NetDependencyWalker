namespace NetDependencyWalker.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;
    using NetDependencyWalker.ViewModel;

    public class ReferencedTypeNodeToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ReferencedTypeNode node)
            {
                switch (node.MemberType)
                {
                    default:
                    {
                        if (node.IsIncoming.HasValue)
                        {
                            return node.IsIncoming.Value ? InboundReferenceGeometry : OutboundReferenceGeometry;
                        }
                        return DefaultGeometry;
                    }
                    case ReferencingMemberType.Method:
                        return MethodGeometry;
                    case ReferencingMemberType.Property:
                        return PropertyGeometry;
                    case ReferencingMemberType.Field:
                        return FieldGeometry;
                    case ReferencingMemberType.TypeHierarchy:
                        return TypeHierarchyGeometry;
                }
            }

            return Geometry.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public Geometry DefaultGeometry { get; set; }

        public Geometry InboundReferenceGeometry { get; set; }

        public Geometry OutboundReferenceGeometry { get; set; }

        public Geometry MethodGeometry { get; set; }

        public Geometry PropertyGeometry { get; set; }

        public Geometry FieldGeometry { get; set; }

        public Geometry TypeHierarchyGeometry { get; set; }
    }
}