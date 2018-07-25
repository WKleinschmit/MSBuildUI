using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSBuildObjects
{
    public class ProjectFolder : _SolutionItem
    {
        public ProjectFolder(Solution solution, string name, Guid guid)
        : base(solution, name, guid) { }
    }
}
