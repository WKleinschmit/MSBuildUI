using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using MSBuildObjects;
using MSBuildUI.Annotations;
using Ookii.Dialogs.Wpf;
using R = MSBuildUI.Properties.Resources;

namespace MSBuildUI.Items
{
    public class SolutionCollection : INotifyPropertyChanged
    {
        private static int counter = 0;

        internal SolutionCollection()
        {
        }

        public string Title { get; private set; }
        public string Filename { get; private set; }
        public bool Modified { get; private set; } = false;

        public void CreateNew()
        {
            Solutions.Clear();
            Title = string.Format(R.solutionCollection, ++counter);
            Filename = null;
        }

        public void OpenExisting(string filename)
        {
            if (filename == null)
            {
                VistaOpenFileDialog ofd = new VistaOpenFileDialog
                {
                    Title = R.openFileTitle,
                    Filter = R.saveAsFilter,

                };

                bool? result = ofd.ShowDialog();
                if (!result.HasValue || !result.Value)
                    return;

                filename = ofd.FileName;
            }

            XDocument doc = XDocument.Load(filename);

            if (!(doc.Root is XElement eltSolutionCollection))
                return;

            Solutions.Clear();
            Title = Path.GetFileNameWithoutExtension(filename);
            Filename = filename;

            foreach (XElement eltSolution in eltSolutionCollection.Elements("Solution"))
            {
                Solution solution = Solution.OpenSolution(eltSolution.Attribute("filename")?.Value);
                SolutionItem solutionItem = AddSolution(solution);
                solutionItem.IsActive = bool.Parse(eltSolution.Attribute("isActive")?.Value ?? "true");
                solutionItem.SelectedConfiguration = eltSolution.Attribute("selectedConfiguration")?.Value;
            }
        }

        public bool Save()
        {
            if (Filename == null)
                return SaveAs();

            XDocument doc = new XDocument();

            XElement eltSolutionCollection = new XElement("SolutionCollection");
            doc.Add(eltSolutionCollection);

            foreach (SolutionItem solutionItem in Solutions)
            {
                solutionItem.Save(eltSolutionCollection);
            }

            using (XmlWriter xmlWriter = XmlWriter.Create(Filename, new XmlWriterSettings { Indent = true }))
                doc.WriteTo(xmlWriter);

            return true;
        }

        public bool SaveAs()
        {
            VistaSaveFileDialog sfd = new VistaSaveFileDialog
            {
                Title = R.saveAsTitle,
                Filter = R.saveAsFilter,
                DefaultExt = "slncoll",
            };

            bool? result = sfd.ShowDialog();
            if (!result.HasValue || !result.Value)
                return false;

            Filename = sfd.FileName;
            Title = Path.GetFileNameWithoutExtension(Filename);
            return Save();
        }

        public ObservableCollection<SolutionItem> Solutions { get; } = new ObservableCollection<SolutionItem>();

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public SolutionItem AddSolution(Solution solution)
        {
            SolutionItem solutionItem = new SolutionItem(solution);
            Solutions.Add(solutionItem);
            return solutionItem;
        }
    }
}
