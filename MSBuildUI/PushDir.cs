using System;
using System.IO;

namespace MSBuildUI
{
    public class PushDir : IDisposable
    {
        private readonly string savedDirectory;

        public PushDir(string newDirectory)
        {
            savedDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(newDirectory);
        }

        public void Dispose()
        {
            Directory.SetCurrentDirectory(savedDirectory);
        }
    }
}
