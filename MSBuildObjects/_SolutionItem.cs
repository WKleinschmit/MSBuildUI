using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media;

namespace MSBuildObjects
{
    public abstract class _SolutionItem : IEquatable<_SolutionItem>
    {
        public Solution Solution { get; }
        public string Name { get; internal set; }
        public Guid Guid { get; }
        public _SolutionItem Parent { get; internal set; }

        private static readonly Dictionary<string, ImageSource> iconCache = new Dictionary<string, ImageSource>();

        public static ImageSource GetIcon(string extension, IconExtensions.IconSize size)
        {
            string key = $"{size}{extension}";
            if (!iconCache.TryGetValue(key, out ImageSource icon))
                icon = iconCache[key] = IconExtensions.GetFileIcon($"*{extension}", size, false).ToImageSource();
            return icon;
        }

        protected _SolutionItem(Solution solution, string name, Guid guid)
        {
            Solution = solution;
            Name = name;
            Guid = guid;
        }

        public bool Equals(_SolutionItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            return ReferenceEquals(this, other) || Guid.Equals(other.Guid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            _SolutionItem other = obj as _SolutionItem;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public static bool operator ==(_SolutionItem left, _SolutionItem right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(_SolutionItem left, _SolutionItem right)
        {
            return !Equals(left, right);
        }
    }
}
