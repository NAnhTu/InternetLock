using System.Windows;
using InternetLock.ViewModels;

namespace InternetLock.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += async (s, e) =>
            {
                await viewModel.InitializeAsync();
            };
        }
    }
}
