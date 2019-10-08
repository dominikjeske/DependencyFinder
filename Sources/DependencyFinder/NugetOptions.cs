using CommandLine;

namespace DependencyFinder
{
    [Verb("nuget", HelpText = "List nuget packages")]
    public class NugetOptions
    {
        [Value(0, HelpText = "Root folder of solutions", Required = true)]
        public string RootPath { get; set; }

        [Option('i', "ignore", HelpText = "Ignore system nugets")]
        public bool IgnoreSystemNugets { get; set; }

        [Option('d', "different", HelpText = "Show only nuget with different versions")]
        public bool OnlyDiff { get; set; }
    }
}