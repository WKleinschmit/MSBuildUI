using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;
using MSBuildObjects;
using MSBuildUI.Annotations;
using PropertyChanged;

namespace MSBuildUI.Items
{
    public class SolutionItem : INotifyPropertyChanged
    {
        public Solution Solution { get; }

        public bool IsActive
        {
            get => _isActive && CanBeActive;
            set => _isActive = value;
        }

        [AlsoNotifyFor(nameof(IsActive))]
        public bool CanBeActive { get; set; } = true;

        public string SelectedConfiguration { get; set; }
        public BuildState BuildState { get; set; } = BuildState.Waiting;

        public FlowDocument Flow { get; } = new FlowDocument();
        private Paragraph MainParagraph { get; } = new Paragraph
        {
            FontFamily = new FontFamily("Lucida Console")
        };

        private readonly List<ProjectItem> _ProjectItems = new List<ProjectItem>();
        private bool _isActive = true;
        public IReadOnlyList<ProjectItem> ProjectItems => _ProjectItems;

        public SolutionItem()
            : this(new Solution())
        {
        }

        public SolutionItem(Solution solution)
        {
            Solution = solution;

            Flow.Blocks.Add(MainParagraph);

            foreach (Project project in Solution.Projects)
            {
                MainParagraph.Inlines.Add(new InlineUIContainer(new ProjectControl
                {
                    DataContext = new ProjectItem(this, project),
                    Margin = new Thickness(3)
                }));
            }

            SelectedConfiguration = solution.SolutionConfigurationPlatforms.FirstOrDefault();
        }

        public void OnSelectedConfigurationChanged()
        {
            foreach (ProjectItem projectItem in ProjectItems)
            {
                string guid = $"{projectItem.Project.Guid:B}".ToUpperInvariant();
                string entryNameActiveConfig = $"{guid}.{SelectedConfiguration}.ActiveCfg";
                string entryNameBuild = $"{guid}.{SelectedConfiguration}.Build.0";

                if (Solution.ProjectConfigurationPlatforms.TryGetValue(entryNameActiveConfig, out string activeCfg))
                {
                    projectItem.SelectedConfiguration = activeCfg;
                    projectItem.IsActive = true;

                    if (Solution.ProjectConfigurationPlatforms.TryGetValue(entryNameBuild, out activeCfg))
                    {
                        projectItem.BuildState = BuildState.Waiting;
                        projectItem.IsActive = true;
                    }
                    else
                    {
                        projectItem.BuildState = BuildState.Inactive;
                        projectItem.IsActive = false;
                    }
                }
            }
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

        public void AddProject(ProjectItem projectItem)
        {
            _ProjectItems.Add(projectItem);
        }

        public override string ToString()
        {
            return Solution.Title;
        }
    }
}
