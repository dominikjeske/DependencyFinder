using CSharpFunctionalExtensions;
using Microsoft.CodeAnalysis;
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

        public string Kind { get; set; }
        public int LineNumber { get; set; }

        public int SelectionStart { get; set; }

        public int SelectionLenght { get; set; }

        public DerivedReference DerivedReference { get; set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return FilePath;
            yield return ClassName;
            yield return LineNumber;
        }
    }

    public class DerivedReference
    {
        public string DerivedTypeName { get; set; }
        public ISymbol DerivedSymbol { get; set; }
    }
}