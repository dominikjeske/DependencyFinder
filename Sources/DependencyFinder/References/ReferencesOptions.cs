using CommandLine;

namespace DependencyFinder
{
    [Verb("reference", HelpText = "Finds all references of object")]
    public class ReferencesOptions
    {
        [Value(0, HelpText = "Root folder of solutions", Required = true)]
        public string RootPath { get; set; }

        [Option('p', "project", HelpText = "Project where searched item is defined")]
        public string SourceProject { get; set; }

        [Option('c', "class", HelpText = "Name of the class we are searching for")]
        public string ClassName { get; set; }

        [Option('t', "table", HelpText = "Display result in table (result are displayed after all results are found)")]
        public bool IsTableView { get; set; }
    }
}