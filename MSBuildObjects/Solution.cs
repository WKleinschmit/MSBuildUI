using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace MSBuildObjects
{
    public class Solution
    {
        private const string SolutionFileHeader = @"Microsoft Visual Studio Solution File, Format Version ";

        private const string VSTag = @"# Visual Studio ";

        class NSResolv : IXmlNamespaceResolver
        {
            private const string NSMsbuild = @"http://schemas.microsoft.com/developer/msbuild/2003";

            public IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
            {
                throw new NotImplementedException();
            }

            public string LookupNamespace(string prefix)
            {
                return prefix == "p" ? NSMsbuild : null;
            }

            public string LookupPrefix(string namespaceName)
            {
                return namespaceName == NSMsbuild ? "p" : null;
            }
        }

        private static readonly Guid FolderGuid = new Guid("{2150E333-8FDC-42A3-9474-1A3956D46DE8}");

        private static readonly Regex rxProperty = new Regex(
            @"(?<Name>[a-z]+) = (?<Value>.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex rxProject = new Regex(
            @"Project\(""(?<Type>{[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}})""\)\s*=\s*""(?<Name>[^""]+)"",\s*""(?<Path>[^""]+)"",\s*""(?<Guid>{[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}})""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex rxProjectSection = new Regex(
            @"ProjectSection\((?<Name>[a-z]+)\)\s*=\s*.*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex rxGlobalSection = new Regex(
            @"GlobalSection\((?<Name>[a-z]+)\)\s*=\s*.*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex rxValueEqValue = new Regex(
            @"(?<Value1>\S+)\s*=\s*(?<Value2>\S+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Dictionary<string, MethodInfo> _SectionHandlers = new Dictionary<string, MethodInfo>();

        private static bool FindProjectSectionHandler(string name, out MethodInfo methodInfo)
        {
            name = $"ParseProjectSection_{name}";
            if (!_SectionHandlers.TryGetValue(name, out methodInfo))
            {
                methodInfo = _SectionHandlers[name] = typeof(Solution).GetMethod(name,
                    BindingFlags.Static | BindingFlags.NonPublic, null,
                    new[] { typeof(Solution), typeof(_SolutionItem), typeof(TextReader) },
                    null);
            }

            return methodInfo != null || FindProjectSectionHandler("", out methodInfo);
        }

        private static bool FindGlobalSectionHandler(string name, out MethodInfo methodInfo)
        {
            name = $"ParseGlobalSection_{name}";
            if (!_SectionHandlers.TryGetValue(name, out methodInfo))
            {
                methodInfo = _SectionHandlers[name] = typeof(Solution).GetMethod(name,
                    BindingFlags.Static | BindingFlags.NonPublic, null,
                    new[] { typeof(Solution), typeof(TextReader) },
                    null);
            }

            return methodInfo != null || FindGlobalSectionHandler("", out methodInfo);
        }

        public Solution()
        : this(@"d:\Depot\ESPRIT\Source\Fact.Nt\EspritDotNetModules_PreESPRITBuild.sln")
        {

        }

        private Solution(string filename)
        {
            Filename = Path.GetFullPath(filename);
            Directory = Path.GetDirectoryName(Filename);
            Title = Path.GetFileNameWithoutExtension(Filename);

            Icon32 = _SolutionItem.GetIcon(".sln", IconExtensions.IconSize.Large);
            Icon16 = _SolutionItem.GetIcon(".sln", IconExtensions.IconSize.Small);
        }

        public ImageSource Icon32 { get; }
        public ImageSource Icon16 { get; }

        public string Filename { get; }
        public string Directory { get; }
        public string Title { get; }
        public Version FormatVersion { get; private set; }
        public string VisualStudioTag { get; private set; }

        private Dictionary<string, string> _Properties { get; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> Properties => _Properties;

        private Dictionary<Guid, _SolutionItem> _SolutionItemsDict { get; } = new Dictionary<Guid, _SolutionItem>();

        private List<_SolutionItem> _SolutionItems { get; } = new List<_SolutionItem>();
        public IReadOnlyList<_SolutionItem> SolutionItems => _SolutionItems;

        private List<Project> _Projects { get; } = new List<Project>();
        public IReadOnlyList<Project> Projects => _Projects;

        private HashSet<string> _SolutionConfigurationPlatforms { get; } = new HashSet<string>();
        public IReadOnlyCollection<string> SolutionConfigurationPlatforms => _SolutionConfigurationPlatforms;

        private HashSet<string> _ProjectConfigurationPlatforms { get; } = new HashSet<string>();
        public IReadOnlyCollection<string> ProjectConfigurationPlatforms => _ProjectConfigurationPlatforms;

        private Project FindProject(Guid guid)
        {
            if (_SolutionItemsDict.TryGetValue(guid, out _SolutionItem solutionItem) && solutionItem is Project project)
                return project;

            project = new Project(this, guid);
            _SolutionItemsDict.Add(guid, project);
            return project;
        }

        private ProjectFolder FindProjectFolder(Guid guid)
        {
            if (_SolutionItemsDict.TryGetValue(guid, out _SolutionItem solutionItem) && solutionItem is ProjectFolder projectFolder)
                return projectFolder;

            projectFolder = new ProjectFolder(this, guid);
            _SolutionItemsDict.Add(guid, projectFolder);
            return projectFolder;
        }

        public static Solution OpenSolution(string filename)
        {
            Solution solution = new Solution(filename);

            using (TextReader textReader = new StreamReader(solution.Filename))
            {
                for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
                {
                    if (line == "")
                        continue;

                    if (line.StartsWith(SolutionFileHeader))
                    {
                        solution.FormatVersion = new Version(line.Substring(SolutionFileHeader.Length));
                        ParseSolutionFile(solution, textReader);
                        break;
                    }

                    throw new ParseException("Solution file header not found.");
                }
            }

            solution._SolutionItems.AddRange(solution._SolutionItemsDict.Values.Where(si => si.Parent == null));
            solution._SolutionItems.Sort((a, b) =>
            {
                if (a is Project && b is Project)
                    return string.Compare(a.Name, b.Name, StringComparison.InvariantCulture);
                if (a is ProjectFolder && b is ProjectFolder)
                    return string.Compare(a.Name, b.Name, StringComparison.InvariantCulture);

                return (a is ProjectFolder) ? -1 : 1;
            });

            solution._Projects.AddRange(solution._SolutionItemsDict.Values.OfType<Project>());
            solution._Projects.Sort(((a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture)));

            return solution;
        }

        private static void ParseSolutionFile(Solution solution, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "")
                    continue;

                if (line.StartsWith(VSTag))
                {
                    solution.VisualStudioTag = line.Substring(2);
                    continue;
                }

                Match M = rxProperty.Match(line);
                if (M.Success)
                {
                    solution._Properties[M.Groups["Name"].Value] = M.Groups["Value"].Value;
                    continue;
                }

                M = rxProject.Match(line);
                if (M.Success)
                {
                    ParseProject(solution, textReader,
                        new Guid(M.Groups["Type"].Value),
                        M.Groups["Name"].Value,
                        M.Groups["Path"].Value,
                        new Guid(M.Groups["Guid"].Value));
                    continue;
                }

                if (line != "Global")
                    throw new ParseException($"Unexpected line: {line}");

                ParseGlobal(solution, textReader);
                break;
            }
        }

        #region ParseGlobal

        private static void ParseGlobal(Solution solution, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "")
                    continue;

                if (line == "EndGlobal")
                    return;

                Match M = rxGlobalSection.Match(line);
                if (M.Success && FindGlobalSectionHandler(M.Groups["Name"].Value, out MethodInfo sectionHandler))
                {
                    sectionHandler.Invoke(solution, new object[] { solution, textReader });
                    continue;
                }

                throw new ParseException($"Unexpected line: {line}");
            }
            throw new ParseException("Unexpected end of file");
        }

        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedParameter.Local
        private static void ParseGlobalSection_(Solution solution, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndGlobalSection")
                    return;
            }
            throw new ParseException("Unexpected end of file");
        }
        // ReSharper restore UnusedParameter.Local

        private static void ParseGlobalSection_SolutionConfigurationPlatforms(Solution solution, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndGlobalSection")
                    return;

                Match M = rxValueEqValue.Match(line);
                if (!M.Success)
                    throw new ParseException($"Unexpected line: {line}");

                solution._SolutionConfigurationPlatforms.Add(M.Groups["Value1"].Value);
            }
            throw new ParseException("Unexpected end of file");
        }

        private static void ParseGlobalSection_ProjectConfigurationPlatforms(Solution solution, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndGlobalSection")
                    return;

                Match M = rxValueEqValue.Match(line);
                if (!M.Success)
                    throw new ParseException($"Unexpected line: {line}");

                solution._ProjectConfigurationPlatforms.Add($"{M.Groups["Value1"].Value}={M.Groups["Value2"].Value}");
            }
            throw new ParseException("Unexpected end of file");
        }

        private static void ParseGlobalSection_SolutionProperties(Solution solution, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndGlobalSection")
                    return;

                Match M = rxValueEqValue.Match(line);
                if (!M.Success)
                    throw new ParseException($"Unexpected line: {line}");

                solution._Properties.Add(M.Groups["Value1"].Value, M.Groups["Value2"].Value);
            }
            throw new ParseException("Unexpected end of file");
        }

        private static void ParseGlobalSection_NestedProjects(Solution solution, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndGlobalSection")
                    return;

                Match M = rxValueEqValue.Match(line);
                if (!M.Success)
                    throw new ParseException($"Unexpected line: {line}");

                Guid guidChild = new Guid(M.Groups["Value1"].Value);
                Guid guidParent = new Guid(M.Groups["Value2"].Value);

                if (!solution._SolutionItemsDict.TryGetValue(guidChild, out _SolutionItem child))
                    continue;

                if (!solution._SolutionItemsDict.TryGetValue(guidParent, out _SolutionItem solutionItem)
                || !(solutionItem is ProjectFolder parent))
                    continue;

                parent.AddChild(child);
            }
            throw new ParseException("Unexpected end of file");
        }

        private static void ParseGlobalSection_ExtensibilityGlobals(Solution solution, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndGlobalSection")
                    return;

                Match M = rxValueEqValue.Match(line);
                if (!M.Success)
                    throw new ParseException($"Unexpected line: {line}");

                solution._Properties.Add(M.Groups["Value1"].Value, M.Groups["Value2"].Value);
            }
            throw new ParseException("Unexpected end of file");
        }
        // ReSharper restore UnusedMember.Local

        #endregion

        #region ParseProject

        private static void ParseProject(Solution solution, TextReader textReader, Guid type, string name, string path, Guid guid)
        {
            if (type == FolderGuid)
            {
                ProjectFolder projectFolder = solution.FindProjectFolder(guid);
                projectFolder.Name = name;
                ParseProjectSections(solution, textReader, projectFolder);
            }
            else
            {
                Project project = solution.FindProject(guid);
                project.Name = name;
                project.Type = type;
                project.Path = Path.GetFullPath(Path.Combine(solution.Directory, path));

                XDocument doc = XDocument.Load(project.Path);
                IEnumerable<XElement> referencedProjects = doc.XPathSelectElements("//p:ProjectReference/p:Project", new NSResolv());
                foreach (XElement referencedProject in referencedProjects)
                    project.DependentProjects.Add(solution.FindProject(new Guid(referencedProject.Value)));

                ParseProjectSections(solution, textReader, project);
            }
        }

        private static void ParseProjectSections(Solution solution, TextReader textReader, _SolutionItem solutionItem)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "")
                    continue;

                if (line == "EndProject")
                    return;

                Match M = rxProjectSection.Match(line);
                if (M.Success && FindProjectSectionHandler(M.Groups["Name"].Value, out MethodInfo sectionHandler))
                {
                    sectionHandler.Invoke(solution, new object[] { solution, solutionItem, textReader });
                    continue;
                }

                throw new ParseException($"Unexpected line: {line}");
            }
            throw new ParseException("Unexpected end of file");
        }

        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedParameter.Local
        private static void ParseProjectSection_(Solution solution, _SolutionItem solutionItem, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndProjectSection")
                    return;
            }
            throw new ParseException("Unexpected end of file");
        }
        // ReSharper restore UnusedParameter.Local

        private static void ParseProjectSection_ProjectDependencies(Solution solution, _SolutionItem solutionItem, TextReader textReader)
        {
            if (!(solutionItem is Project project))
                return;

            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndProjectSection")
                    return;

                Match M = rxValueEqValue.Match(line);
                if (!M.Success)
                    throw new ParseException($"Unexpected line: {line}");

                project.DependentProjects.Add(solution.FindProject(new Guid(M.Groups["Value1"].Value)));
            }
            throw new ParseException("Unexpected end of file");
        }

        private static void ParseProjectSection_SolutionItems(Solution solution, _SolutionItem solutionItem, TextReader textReader)
        {
            if (!(solutionItem is ProjectFolder projectFolder))
                return;

            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndProjectSection")
                    return;

                Match M = rxValueEqValue.Match(line);
                if (!M.Success)
                    throw new ParseException($"Unexpected line: {line}");

                projectFolder.AddChild(new SolutionItem(solution, M.Groups["Value1"].Value));
            }
            throw new ParseException("Unexpected end of file");
        }
        // ReSharper restore UnusedMember.Local

        #endregion
    }
}
