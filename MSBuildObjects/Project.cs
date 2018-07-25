using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSBuildObjects
{
    public class Project : _SolutionItem
    {
        public Project(Solution solution, Guid guid)
            : base(solution, null, guid) { }

        public Guid Type { get; internal set; }
        public string Path { get; internal set; }

        public HashSet<Project> DependentProjects { get; } = new HashSet<Project>();
    }
}
