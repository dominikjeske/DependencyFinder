using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DependencyFinder.Core.Models
{
    public class ProjectDetails : ValueObject
    {
        public static readonly ProjectDetails Empty = new ProjectDetails();

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string RelativePath { get; set; }
        public string AbsolutePath { get; set; }
        public string Type { get; set; }
        public string Solution { get; set; }
        public bool IsNetCore { get; set; }
        public bool IsMultiTarget { get; set; }

        [ExpandableObject]
        public AssemblyInfo AssemblyInfo { get; set; }

        public List<NugetPackage> Nugets { get; set; } = new List<NugetPackage>();
        public List<ProjectReference> ProjectReferences { get; set; } = new List<ProjectReference>();
        public List<ProjectTarget> ProjectTargets { get; set; } = new List<ProjectTarget>();
        public List<ProjectReference> DirectReferences { get; set; } = new List<ProjectReference>();
        public List<string> SourceCodes { get; set; } = new List<string>();

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return AbsolutePath;
        }
    }
}