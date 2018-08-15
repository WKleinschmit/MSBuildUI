using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MSBuildObjects;
using Ookii.Dialogs.Wpf;
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
        public ICommand RunBuildCommand { get; private set; }
        public ICommand RunRebuildCommand { get; private set; }
        public ICommand CancelBuildCommand { get; private set; }

        private void InitCommands()
        {
            ExitCommand = new RelayCommand(OnExit, _ => IsIdle);
            NewCollectionCommand = new RelayCommand(OnNewCollection, _ => SolutionCollection.Filename != null && IsIdle);
            OpenCollectionCommand = new RelayCommand(OnOpenCollection, _ => IsIdle);
            SaveCollectionCommand = new RelayCommand(OnSaveCollection);
            SaveCollectionAsCommand = new RelayCommand(OnSaveCollectionAs);
            AddSolutionCommand = new RelayCommand(OnAddSolution, _ => IsIdle);
            RunBuildCommand = new RelayCommand(OnRunBuild, _ => IsIdle && SolutionCollection.Solutions.Any(s => s.IsActive));
            RunRebuildCommand = new RelayCommand(OnRunRebuild, _ => IsIdle && SolutionCollection.Solutions.Any(s => s.IsActive));
            CancelBuildCommand = new RelayCommand(OnCancelBuild, _ => !IsIdle);
        }

        private void OnCancelBuild(object obj)
        {
            
        }

        private async void OnRunBuild(object obj)
        {
            await RunBuild("Build");
        }

        private async void OnRunRebuild(object obj)
        {
            await RunBuild("Rebuild");
        }

        private void OnAddSolution(object obj)
        {
            VistaOpenFileDialog ofd = new VistaOpenFileDialog();
            ofd.AddFilter("sln");
            bool? result = ofd.ShowDialog(_mainWindow);
            if (!result.HasValue || !result.Value)
                return;

            Solution solution = Solution.OpenSolution(ofd.FileName);
            SolutionCollection.AddSolution(solution);
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

            string filename = obj as string;
            SolutionCollection.OpenExisting(ref filename);
            AddRecentFile(filename);
        }

        private void OnNewCollection(object obj)
        {
            if (!PromptModified())
                return;

            if (SolutionCollection.Filename != null)
                SolutionCollection.CreateNew();
        }

        private void OnExit(object obj)
        {
            if (!PromptModified())
                return;

            _mainWindow?.Close();
        }

        public bool PromptModified()
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
