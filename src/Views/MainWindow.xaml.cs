namespace NetDependencyWalker.Views
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using NetDependencyWalker.ViewModel;

    public partial class MainWindow
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel = new MainWindowViewModel(Dispatcher.Invoke);
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
        }

        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                _viewModel.RootFileName = files[0];
            }
        }

        private void MultipleVersionsListMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = MultipleVersionsList.SelectedItem as AssemblyVersionsNode;

            if (selectedItem == null)
            {
                return;
            }

            for (var i = 0; i < ReverseTree.Items.Count; i++)
            {
                if (ReverseTree.Items[i] is ReverseAssemblyNode reverseNode && string.Equals(reverseNode.Name, selectedItem.Name, StringComparison.OrdinalIgnoreCase))
                {
                    reverseNode.IsExpanded = true;

                    TabControl.SelectedIndex = TabControl.Items.IndexOf(ReverseTreeTabItem);
                    
                    if (ReverseTree.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem container)
                    {
                        container.IsSelected = true;
                        container.BringIntoView();
                    }
                    break;
                }
            }
        }

        private void OpenMaterialDesignIcons(object sender, RoutedEventArgs e)
        {
            TryStart("https://materialdesignicons.com/");
        }

        private void OpenMonoCecil(object sender, RoutedEventArgs e)
        {
            TryStart("https://github.com/jbevain/cecil");
        }
        
        private void OpenIlSpy(object sender, RoutedEventArgs e)
        {
            TryStart("https://github.com/icsharpcode/ILSpy");
        }
        
        private void OpenXUnit(object sender, RoutedEventArgs e)
        {
            TryStart("https://github.com/xunit/xunit");
        }

        private static void TryStart(string url)
        {
            try
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            catch (Win32Exception)
            {
            }
        }

    }
}
