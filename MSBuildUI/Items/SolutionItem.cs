using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;
using MSBuildObjects;
using MSBuildUI.Annotations;

namespace MSBuildUI.Items
{
    public class SolutionItem : INotifyPropertyChanged
    {
        public Solution Solution { get; }
        public bool IsActive { get; set; } = true;
        public string SelectedConfiguration { get; set; }

        public BuildState BuildState { get; set; } = BuildState.Waiting;
        public FlowDocument Flow { get; } = new FlowDocument();
        private Paragraph MainParagraph { get; } = new Paragraph
        {
            FontFamily = new FontFamily("Lucida Console")
        };

        public SolutionItem()
            : this(new Solution())
        {
        }

        public SolutionItem(Solution solution)
        {
            Solution = solution;
            SelectedConfiguration = solution.SolutionConfigurationPlatforms.FirstOrDefault();

            Flow.Blocks.Add(MainParagraph);

            foreach (Project project in Solution.Projects)
            {
                MainParagraph.Inlines.Add(new InlineUIContainer(new ProjectControl
                {
                    DataContext = new ProjectItem(this, project),
                    Margin = new Thickness(3)
                }));
            }

        }

        public void OnSelectedConfigurationChanged()
        {

        }

        public void Save(XElement eltParent)
        {
            eltParent.Add(new XElement(
                "Solution",
                new XAttribute("filename", Solution.Filename),
                new XAttribute("isActive", IsActive),
                new XAttribute("selectedConfiguration", SelectedConfiguration)));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
