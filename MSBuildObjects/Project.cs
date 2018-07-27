using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using MSBuildObjects.Annotations;

namespace MSBuildObjects
{
    public class Project : _SolutionItem, INotifyPropertyChanged
    {
        public Project(Solution solution, Guid guid)
            : base(solution, null, guid)
        {
        }

        public ImageSource Icon32 { get; private set; }
        public ImageSource Icon16 { get; private set; }

        public Guid Type { get; internal set; }
        public string Path { get; internal set; }

        public void OnPathChanged()
        {
            string ext = System.IO.Path.GetExtension(Path);
            Icon32 = GetIcon(ext, IconExtensions.IconSize.Large);
            Icon16 = GetIcon(ext, IconExtensions.IconSize.Small);
        }

        public HashSet<Project> DependentProjects { get; } = new HashSet<Project>();
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
