using System.ComponentModel;
using System.Runtime.CompilerServices;
using MSBuildUI.Annotations;
using MSBuildUI.Collections;
using R = MSBuildUI.Properties.Resources;

namespace MSBuildUI
{
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
        private MainWindow _mainWindow;

        public MainWindowViewModel()
        {
            InitCommands();
            SolutionCollection = SolutionCollection.CreateNew();
            SolutionCollection.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(SolutionCollection.Modified))
                {
                    OnPropertyChanged(nameof(Title));
                }
                if (args.PropertyName == nameof(SolutionCollection.Title))
                {
                    OnPropertyChanged(nameof(Title));
                }
            };
        }

        public SolutionCollection SolutionCollection { get; private set; }

        public void SetMainWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public string Title => $"{R.appTitle} - {SolutionCollection.Title} {(SolutionCollection.Modified ? "*" : "")}";


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
