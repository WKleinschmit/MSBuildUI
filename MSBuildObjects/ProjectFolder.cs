using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSBuildObjects
{
    public class ProjectFolder : _SolutionItem
    {
        public ProjectFolder(Solution solution, Guid guid)
            : base(solution, null, guid) { }

        public ProjectFolder(Solution solution, string name, Guid guid)
            : base(solution, name, guid) { }

        private List<_SolutionItem> _Children { get; } = new List<_SolutionItem>();
        public IReadOnlyList<_SolutionItem> Children => _Children;

        internal void AddChild(_SolutionItem child)
        {
            _Children.Add(child);
            child.Parent = this;
        }
    }
}
