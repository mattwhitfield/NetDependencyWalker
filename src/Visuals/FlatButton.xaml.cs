namespace NetDependencyWalker.Visuals
{
    using System.Windows;
    using System.Windows.Media;

    public partial class FlatButton
    {
        public static readonly DependencyProperty HighlightShownProperty = DependencyProperty.Register(nameof(HighlightShown), typeof(bool), typeof(FlatButton), new UIPropertyMetadata(false, PropertyChangedCallback));

        public static readonly DependencyProperty PathProperty = DependencyProperty.Register(nameof(Path), typeof(Geometry), typeof(FlatButton), new UIPropertyMetadata(Geometry.Empty, PropertyChangedCallback));

        public FlatButton()
        {
            InitializeComponent();
        }

        public bool HighlightShown
        {
            get => (bool)GetValue(HighlightShownProperty);
            set => SetValue(HighlightShownProperty, value);
        }

        public Geometry Path
        {
            get => (Geometry)GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }

        private static void PropertyChangedCallback(DependencyObject source, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (source is FlatButton miniPathButton)
            {
                switch (dependencyPropertyChangedEventArgs.Property.Name)
                {
                    case nameof(Path):
                        miniPathButton.PathElement.Data = dependencyPropertyChangedEventArgs.NewValue as Geometry ?? Geometry.Empty;
                        break;

                    case nameof(HighlightShown):
                        miniPathButton.HighlightBorder.BorderBrush = (dependencyPropertyChangedEventArgs.NewValue as bool? ?? false) ? Brushes.DodgerBlue : Brushes.Transparent;
                        break;
                }
            }
        }
    }
}