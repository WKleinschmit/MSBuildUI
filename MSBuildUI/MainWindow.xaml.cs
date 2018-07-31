using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
