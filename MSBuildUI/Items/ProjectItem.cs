using System;
using System.Linq;
using MSBuildObjects;

namespace MSBuildUI.Items
{
    public class ProjectItem
    {
        public SolutionItem SolutionItem { get; }
        public Project Project { get; }

        public BuildState BuildState { get; set; } = BuildState.Waiting;

        public ProjectItem(SolutionItem solutionItem, Project project)
        {
            SolutionItem = solutionItem;
            Project = project;
        }
    }
}
