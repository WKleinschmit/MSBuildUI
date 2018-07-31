using System.ComponentModel;

namespace MSBuildUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            if (DataContext is MainWindowViewModel mainWindowViewModel)
                mainWindowViewModel.SetMainWindow(this);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (!(DataContext is MainWindowViewModel model))
                return;

            e.Cancel = !model.PromptModified();
            if (!e.Cancel)
                model.SaveSettings();
        }
    }
}
