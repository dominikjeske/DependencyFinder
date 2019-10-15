using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace DependencyFinder
{
    [Verb("list", HelpText = "List content of solutions")]
    public class SolutionOptions
    {
        [Value(0, HelpText = "Root folder of solutions")]
        public string RootPath { get; set; }

        [Option('p', "projects", HelpText = "List projects")]
        public bool ListProjects { get; set; }

        [Option('n', "nugets", HelpText = "List nugets")]
        public bool ListNugets { get; set; }

        [Option('i', "ignore", HelpText = "Ignore system nugets")]
        public bool IgnoreSystemNugets { get; set; }

        [Option('c', "core", HelpText = "Show only .NET core projects", Default = false)]
        public bool ShowOnlyCore { get; set; }

        [Option('f', "full", HelpText = "Show only .NET full framework projects", Default = false)]
        public bool ShowOnlyFull { get; set; }

        [Option('m', "pname", HelpText = "Search only for project names")]
        public string ProjectName { get; set; }

        [Option('g', "nname", HelpText = "Search only for nuget names")]
        public string NugetName { get; set; }

        [Usage(ApplicationAlias = "DependencyFinder.exe")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Solution list", new SolutionOptions { });
                yield return new Example("Solution list with projects", new SolutionOptions { ListProjects = true });
            }
        }
    }
}