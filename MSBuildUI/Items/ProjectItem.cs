using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MSBuildObjects;
using MSBuildUI.Annotations;

namespace MSBuildUI.Items
{
    public class ProjectItem : INotifyPropertyChanged
    {
        public SolutionItem SolutionItem { get; }
        public Project Project { get; }
        public bool IsActive { get; set; } = true;
        public string SelectedConfiguration { get; set; }
        public BuildState BuildState { get; set; } = BuildState.Waiting;

        public ProjectItem(SolutionItem solutionItem, Project project)
        {
            SolutionItem = solutionItem;
            solutionItem.AddProject(this);
            Project = project;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Project.Name;
        }
    }
}
