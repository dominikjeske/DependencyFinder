using CSharpFunctionalExtensions;
using System.Collections.Generic;
using System.Linq;

namespace DependencyFinder.Core.Models
{
    public class TypeDetails : ValueObject
    {
        public string Name { get; set; }

        public string Kind { get; set; }

        public IEnumerable<Member> Members { get; set; } = Enumerable.Empty<Member>();

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
        }
    }
}