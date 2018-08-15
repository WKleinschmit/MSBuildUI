using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using MSBuildUI.Properties;

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

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (!string.IsNullOrEmpty(Settings.Default.Layout))
                this.SetPlacement(Settings.Default.Layout);

            if (!string.IsNullOrEmpty(Settings.Default.Docking))
                XamDockManager.LoadLayout(Settings.Default.Docking);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (!(DataContext is MainWindowViewModel model))
                return;

            e.Cancel = !model.PromptModified();
            if (e.Cancel)
                return;

            Settings.Default.Layout = this.GetPlacement();

            model.SaveSettings();
            MemoryStream ms = new MemoryStream();
            XamDockManager.SaveLayout(ms);
            Settings.Default.Docking = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int) ms.Position);
            ms.Dispose();

            Settings.Default.Save();
        }
    }
}
