using CSharpFunctionalExtensions;
using System.Collections.Generic;

namespace DependencyFinder.Core
{
    public class Reference : ValueObject
    {
        public string SolutionName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ProjectName { get; set; }
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        public string Block { get; set; }
        public int LineNumber { get; set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return FilePath;
            yield return ClassName;
            yield return LineNumber;
        }
    }
}