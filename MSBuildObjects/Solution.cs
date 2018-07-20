using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSBuildObjects
{
    public class Solution
    {
        private Solution()
        {
            Directory = Path.GetDirectoryName(Path.GetFullPath(Filename));
            Title = Path.GetFileNameWithoutExtension(Filename);
        }

        public string Filename { get; private set; }
        public string Directory { get; }
        public string Title { get; }

        public static Solution OpenSolution(string filename)
        {
            Solution solution = new Solution
            {
                Filename = filename,
            };


            return solution;
        }
    }
}
