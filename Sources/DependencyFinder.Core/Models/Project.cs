using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;

namespace DependencyFinder.Core.Models
{
    public class Project : ValueObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string RelativePath { get; set; }
        public string AbsolutePath { get; set; }
        public string Type { get; set; }
        public string Solution { get; set; }
        public bool IsNetCore { get; set; }

        public List<NugetPackage> Nugets { get; set; } = new List<NugetPackage>();

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Id;
        }
    }
}