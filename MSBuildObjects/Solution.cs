using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MSBuildObjects
{
    public class Solution
    {
        private const string SolutionFileHeader = @"Microsoft Visual Studio Solution File, Format Version ";

        private const string VSTag = @"# Visual Studio ";

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

        private static readonly Regex rxGuidEqGuid = new Regex(
            @"(?<Guid1>{[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}})\s*=\s*(?<Guid2>{[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Dictionary<string, MethodInfo> _SectionHandlers = new Dictionary<string, MethodInfo>();

        private static bool FindProjectSectionHandler(string name, out MethodInfo methodInfo)
        {
            name = $"ParseProjectSection_{name}";
            if (!_SectionHandlers.TryGetValue(name, out methodInfo))
            {
                methodInfo = _SectionHandlers[name] = typeof(Solution).GetMethod(name,
                    BindingFlags.Static | BindingFlags.NonPublic, null,
                    new[] { typeof(Solution), typeof(Project), typeof(TextReader) },
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

        private Solution(string filename)
        {
            Filename = Path.GetFullPath(filename);
            Directory = Path.GetDirectoryName(Filename);
            Title = Path.GetFileNameWithoutExtension(Filename);
        }

        public string Filename { get; }
        public string Directory { get; }
        public string Title { get; }
        public Version FormatVersion { get; private set; }
        public string VisualStudioTag { get; private set; }

        private Dictionary<string, string> _Properties { get; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> Properties => _Properties;

        private Dictionary<Guid, _SolutionItem> _SolutionItems { get; } = new Dictionary<Guid, _SolutionItem>();

        private Project FindProject(Guid guid)
        {
            if (_SolutionItems.TryGetValue(guid, out _SolutionItem solutionItem) && solutionItem is Project project)
                return project;

            project = new Project(this, guid);
            _SolutionItems.Add(guid, project);
            return project;
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
            throw new ParseException($"Unexpected end of file");
        }

        // ReSharper disable UnusedMember.Local
        private static void ParseGlobalSection_(Solution solution, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndGlobalSection")
                    return;
            }
            throw new ParseException($"Unexpected end of file");
        }

        private static void ParseGlobalSection_SolutionConfigurationPlatforms(Solution solution, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndGlobalSection")
                    return;
            }
            throw new ParseException($"Unexpected end of file");
        }
        // ReSharper restore UnusedMember.Local

        #region MyRegion

        private static void ParseProject(Solution solution, TextReader textReader, Guid type, string name, string path, Guid guid)
        {
            if (type == FolderGuid)
            {
                solution._SolutionItems.Add(guid, new ProjectFolder(solution, name, guid));

                for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
                {
                    switch (line)
                    {
                        case "":
                            continue;
                        case "EndProject":
                            return;
                        default:
                            throw new ParseException($"Unexpected line: {line}");
                    }
                }
                throw new ParseException($"Unexpected end of file");
            }
            else
            {
                Project project = solution.FindProject(guid);
                project.Name = name;
                project.Type = type;
                project.Path = Path.GetFullPath(Path.Combine(solution.Directory, path));

                for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
                {
                    if (line == "")
                        continue;

                    if (line == "EndProject")
                        return;

                    Match M = rxProjectSection.Match(line);
                    if (M.Success && FindProjectSectionHandler(M.Groups["Name"].Value, out MethodInfo sectionHandler))
                    {
                        sectionHandler.Invoke(solution, new object[] { solution, project, textReader });
                        continue;
                    }

                    throw new ParseException($"Unexpected line: {line}");
                }
                throw new ParseException($"Unexpected end of file");
            }
        }

        // ReSharper disable UnusedMember.Local
        private static void ParseProjectSection_(Solution solution, Project project, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndProjectSection")
                    return;
            }
            throw new ParseException($"Unexpected end of file");
        }

        private static void ParseProjectSection_ProjectDependencies(Solution solution, Project project, TextReader textReader)
        {
            for (string line = textReader.ReadLine()?.Trim(); line != null; line = textReader.ReadLine()?.Trim())
            {
                if (line == "EndProjectSection")
                    return;

                Match M = rxGuidEqGuid.Match(line);
                if (M.Success)
                {
                    project.DependentProjects.Add(solution.FindProject(new Guid(M.Groups["Guid1"].Value)));
                    continue;
                }

                throw new ParseException($"Unexpected line: {line}");
            }
            throw new ParseException($"Unexpected end of file");
        }
        // ReSharper restore UnusedMember.Local

        #endregion
    }
}
