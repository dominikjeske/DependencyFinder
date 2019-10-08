using CommandLine;

namespace DependencyFinder
{
    [Verb("reference", HelpText = "Finds all references of object")]
    public class ReferencesOptions
    {
        [Value(0, HelpText = "Root folder of solutions", Required = true)]
        public string RootPath { get; set; }   
    }
}