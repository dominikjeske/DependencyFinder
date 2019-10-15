namespace DependencyFinder.Core.Models
{
    public class NugetPackage
    {
        public string Name { get; set; }
        public string TargetFramework { get; set; }
        public VersionEx Version { get; set; }
    }
}