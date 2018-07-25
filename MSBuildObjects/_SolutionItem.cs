using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSBuildObjects
{
    public abstract class _SolutionItem
    {
        private Solution Solution { get; }
        public string Name { get; internal set; }
        public Guid Guid { get; }

        protected _SolutionItem(Solution solution, string name, Guid guid)
        {
            Solution = solution;
            Name = name;
            Guid = guid;
        }
    }
}
