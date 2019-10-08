using System;

namespace DependencyFinder.Core.Models
{
    public class NugetPackage
    {
        public string Name { get; set; }
        public string TargetFramework { get; set; }
        public Version Version { get; set; }
    }
}