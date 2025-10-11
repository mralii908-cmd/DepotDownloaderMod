using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using DepotDownloaderGUI.ViewModels;

namespace DepotDownloaderGUI
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private DispatcherTimer _scrollTimer;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            DataContextChanged += MainWindow_DataContextChanged;

            // Setup timer for continuous scrolling during downloads
            _scrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _scrollTimer.Tick += ScrollTimer_Tick;
            _scrollTimer.Start();
        }

        private void ScrollTimer_Tick(object sender, EventArgs e)
        {
            // Scroll to bottom if auto-scroll is enabled and there are items
            var viewModel = DataContext as MainViewModel;
            if (viewModel?.AutoScroll == true && FilesDataGrid.Items.Count > 0)
            {
                try
                {
                    var lastItem = FilesDataGrid.Items[FilesDataGrid.Items.Count - 1];
                    FilesDataGrid.ScrollIntoView(lastItem);
                }
                catch
                {
                    // Ignore scroll errors
                }
            }
        }

        private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old ViewModel
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            // Subscribe to new ViewModel
            _viewModel = DataContext as MainViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.LogText))
            {
                if (_viewModel?.AutoScroll == true)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        LogTextBox.ScrollToEnd();
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Shutdown();
            }
        }
    }
}
