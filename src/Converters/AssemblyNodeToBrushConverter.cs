namespace NetDependencyWalker.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;
    using NetDependencyWalker.ViewModel;

    public class AssemblyNodeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AssemblyNode node)
            {
                if (!node.VersionMatchingSpecified)
                {
                    return UnspecifiedBrush;
                }
                if (node.MajorVersionMismatched)
                {
                    return MajorVersionMismatchBrush;
                }
                if (node.MinorVersionMismatched)
                {
                    return MinorVersionMismatchBrush;
                }
                if (node.PatchVersionMismatched)
                {
                    return PatchVersionMismatchBrush;
                }
                if (node.VersionMatched)
                {
                    return VersionMatchBrush;
                }
            }

            if (value is AssemblyVersionsNode versionsNode)
            {
                return versionsNode.MajorVersionMismatched ? MajorVersionMismatchBrush : MinorVersionMismatchBrush;
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public Brush MajorVersionMismatchBrush { get; set; }

        public Brush MinorVersionMismatchBrush { get; set; }

        public Brush PatchVersionMismatchBrush { get; set; }

        public Brush VersionMatchBrush { get; set; }

        public Brush UnspecifiedBrush { get; set; }
    }
}