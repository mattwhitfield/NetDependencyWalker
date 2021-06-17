namespace NetDependencyWalker.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;
    using NetDependencyWalker.ViewModel;

    public class AssemblyNodeToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AssemblyNode node)
            {
                if (!node.VersionMatchingSpecified)
                {
                    return UnspecifiedGeometry;
                }
                if (node.MajorVersionMismatched)
                {
                    return MajorVersionMismatchGeometry;
                }
                if (node.MinorVersionMismatched)
                {
                    return MinorVersionMismatchGeometry;
                }
                if (node.PatchVersionMismatched)
                {
                    return PatchVersionMismatchGeometry;
                }
                if (node.VersionMatched)
                {
                    return VersionMatchGeometry;
                }
            }

            if (value is AssemblyVersionsNode versionsNode)
            {
                return versionsNode.MajorVersionMismatched ? MajorVersionMismatchGeometry : MinorVersionMismatchGeometry;
            }

            return Geometry.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public Geometry MajorVersionMismatchGeometry { get; set; }

        public Geometry MinorVersionMismatchGeometry { get; set; }

        public Geometry PatchVersionMismatchGeometry { get; set; }

        public Geometry VersionMatchGeometry { get; set; }

        public Geometry UnspecifiedGeometry { get; set; }
    }
}