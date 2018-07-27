using System;

namespace MSBuildObjects
{
    public class SolutionItem : _SolutionItem
    {
        public SolutionItem(Solution solution, string name)
            : base(solution, name, Guid.Empty)
        {
        }
    }
}
