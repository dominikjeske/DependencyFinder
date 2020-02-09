using CSharpFunctionalExtensions;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace DependencyFinder.Core.Models
{
    public class TypeDetails : ValueObject
    {
        public string Name { get; set; }

        public IEnumerable<Member> Members { get; set; } = Enumerable.Empty<Member>();

        public ISymbol Symbol { get; set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
        }
    }
}