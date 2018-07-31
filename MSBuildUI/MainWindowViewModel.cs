using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Infragistics.Windows.Ribbon;
using MSBuildUI.Annotations;
using MSBuildUI.Items;
using MSBuildUI.Properties;
using R = MSBuildUI.Properties.Resources;

namespace MSBuildUI
{
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
        private MainWindow _mainWindow;
        private static readonly ImageSource Coll16 = new BitmapImage(new Uri("pack://application:,,,/MSBuildUI;component/img/collection@16px.png"));
        private static readonly ImageSource Coll32 = new BitmapImage(new Uri("pack://application:,,,/MSBuildUI;component/img/collection@32px.png"));
        private static readonly string vswherePath;

        static MainWindowViewModel()
        {
            vswherePath = Path.GetDirectoryName(Path.GetFullPath(typeof(MainWindowViewModel).Assembly.Location));
            vswherePath = Path.Combine(vswherePath ?? @".\", "vswhere.exe");
        }

        public MainWindowViewModel()
        {
            InitCommands();
            SolutionCollection.CreateNew();
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

            if (Settings.Default.MRU == null)
                Settings.Default.MRU = new StringCollection();
        }

        public SolutionCollection SolutionCollection { get; } = new SolutionCollection();

        public void RemoveRecentFile(string path)
        {
            path = Path.GetFullPath(path);
            ButtonTool buttonTool = _mainWindow.ApplicationMenu.RecentItems
                .OfType<ButtonTool>()
                .FirstOrDefault(bt => bt.CommandParameter is string p && p.Equals(path, StringComparison.OrdinalIgnoreCase));

            if (buttonTool != null)
                _mainWindow.ApplicationMenu.RecentItems.Remove(buttonTool);
        }

        public void AddRecentFile(string path)
        {
            RemoveRecentFile(path);

            ButtonTool buttonTool = new ButtonTool
            {
                LargeImage = Coll32,
                SmallImage = Coll16,
                Caption = Path.GetFileNameWithoutExtension(path),
                Command = OpenCollectionCommand,
                CommandParameter = Path.GetFullPath(path),
                ToolTip = Path.GetFullPath(path),
            };
            _mainWindow.ApplicationMenu.RecentItems.Insert(0, buttonTool);
        }

        public bool IsIdle { get; set; } = true;

        public void SetMainWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            foreach (string path in Settings.Default.MRU.OfType<string>().Reverse())
            {
                AddRecentFile(path);
            }
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

        private void RunBuild(string target)
        {
            try
            {
                IsIdle = false;
                foreach (SolutionItem solutionItem in SolutionCollection.Solutions)
                {
                    if (solutionItem.IsActive)
                        BuildSolution(solutionItem, target);
                }
            }
            finally
            {
                IsIdle = true;
            }
        }

        private void BuildSolution(SolutionItem solutionItem, string target)
        {
            string[] parts = solutionItem.SelectedConfiguration.Split("|".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);
            string pipeName = Guid.NewGuid().ToString("N");

            using (new PushDir(solutionItem.Solution.Directory))
            {
                string tempFileName = Path.GetTempFileName();
                File.Move(tempFileName, Path.ChangeExtension(tempFileName, ".bat"));
                tempFileName = Path.ChangeExtension(tempFileName, ".bat");
                try
                {
                    using (TextWriter textWriter = new StreamWriter(tempFileName))
                    {
                        textWriter.WriteLine($@"@echo off

set VSWHERE=""{vswherePath}""
for /f ""usebackq tokens=1,* delims=: "" %%a in (`%VSWHERE% -legacy -latest`) do set ""%%a=%%b""

if exist ""%installationPath%\Common7\Tools\vsdevcmd.bat"" (
    pushd .
    if %installationVersion% gtr 15.5 (
        call ""%installationPath%\Common7\Tools\vsdevcmd.bat"" -vcvars_ver=14.11
    ) else (
        call ""%installationPath%\Common7\Tools\vsdevcmd.bat""
    )
    popd
) else (
    echo Visual Studio installation not found.
    exit /B 1
)

msbuild /nologo /n ^
    ""/target:{target}"" ^
    ""/property:Configuration={parts[0]}"" ^
    ""/property:Platform={parts[1]}"" ^
    ""/logger:RemoteLogger,MSBuildLogging;PipeName={pipeName}"" ^
    ""{Path.GetFileName(solutionItem.Solution.Filename)}""
");
                    }

                    ProcessStartInfo psi = new ProcessStartInfo("cmd.exe")
                    {
                        Arguments = $"/C call \"{tempFileName}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };

                    Process.Start(psi)?.Dispose();

                }
                finally
                {
                    if (File.Exists(tempFileName))
                        File.Delete(tempFileName);
                }
            }
        }

        public void SaveSettings()
        {
            Settings.Default.MRU.Clear();
            foreach (ButtonTool buttonTool in _mainWindow.ApplicationMenu.RecentItems.OfType<ButtonTool>())
            {
                if (buttonTool.CommandParameter is string path)
                    Settings.Default.MRU.Add(path);
            }
            Settings.Default.Save();
        }
    }
}
