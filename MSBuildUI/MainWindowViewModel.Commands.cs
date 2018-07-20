using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MSBuildUI.Collections;
using R = MSBuildUI.Properties.Resources;

namespace MSBuildUI
{
    public partial class MainWindowViewModel
    {
        public ICommand ExitCommand { get; private set; }
        public ICommand NewCollectionCommand { get; private set; }
        public ICommand OpenCollectionCommand { get; private set; }
        public ICommand SaveCollectionCommand { get; private set; }
        public ICommand SaveCollectionAsCommand { get; private set; }
        public ICommand AddSolutionCommand { get; private set; }

        private void InitCommands()
        {
            ExitCommand = new RelayCommand(OnExit, _ => true);
            NewCollectionCommand = new RelayCommand(OnNewCollection, _ => SolutionCollection.Filename != null);
            OpenCollectionCommand = new RelayCommand(OnOpenCollection);
            SaveCollectionCommand = new RelayCommand(OnSaveCollection);
            SaveCollectionAsCommand = new RelayCommand(OnSaveCollectionAs);
            AddSolutionCommand = new RelayCommand(OnAddSolution);
        }

        private void OnAddSolution(object obj)
        {

        }

        private void OnSaveCollectionAs(object obj)
        {
            SolutionCollection.SaveAs();
        }

        private void OnSaveCollection(object obj)
        {
            SolutionCollection.Save();
        }

        private void OnOpenCollection(object obj)
        {
            if (!PromptModified())
                return;

            SolutionCollection solutionCollection = SolutionCollection.OpenExisting(null);
        }

        private void OnNewCollection(object obj)
        {
            if (!PromptModified())
                return;

            if (SolutionCollection.Filename != null)
                SolutionCollection = SolutionCollection.CreateNew();
        }

        private void OnExit(object obj)
        {
            if (!PromptModified())
                return;

            _mainWindow?.Close();
        }

        private bool PromptModified()
        {
            if (!SolutionCollection.Modified)
                return true;

            MessageBoxResult result = MessageBox.Show(
                R.modifiedPrompt, R.appTitle,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (result)
            {
                case MessageBoxResult.Cancel:
                    return false;
                case MessageBoxResult.Yes:
                    return SolutionCollection.Save();
                case MessageBoxResult.No:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
