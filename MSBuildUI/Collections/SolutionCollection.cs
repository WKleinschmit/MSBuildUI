using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using MSBuildUI.Annotations;
using Ookii.Dialogs.Wpf;
using R = MSBuildUI.Properties.Resources;

namespace MSBuildUI.Collections
{
    public class SolutionCollection : INotifyPropertyChanged
    {
        private static int counter = 0;

        private SolutionCollection()
        {

        }

        public string Title { get; private set; }
        public string Filename { get; private set; }
        public bool Modified { get; private set; } = false;

        public static SolutionCollection CreateNew()
        {
            SolutionCollection solutionCollection = new SolutionCollection
            {
                Title = string.Format(R.solutionCollection, ++counter),
                Filename = null,
            };
            return solutionCollection;
        }

        public static SolutionCollection OpenExisting(string filename)
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
                    return null;

                filename = ofd.FileName;
            }

            XDocument doc = XDocument.Load(filename);

            SolutionCollection solutionCollection = new SolutionCollection
            {
                Title = Path.GetFileNameWithoutExtension(filename),
                Filename = filename,
            };

            return solutionCollection;
        }

        public bool Save()
        {
            if (Filename == null)
                return SaveAs();

            XDocument doc = new XDocument();

            XElement eltSolutionCollection = new XElement("SolutionCollection");
            doc.Add(eltSolutionCollection);

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
            };

            bool? result = sfd.ShowDialog();
            if (!result.HasValue || !result.Value || !Save())
                return false;

            Filename = sfd.FileName;
            Title = Path.GetFileNameWithoutExtension(Filename);
            return true;
        }

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
