using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace DependencyFinder.Core.Models
{
    public class ClassDetails : TypeDetails
    {
        public ClassDetails(string name, IEnumerable<Member> members, ISymbol symbol)
        {
            Name = name;
            Members = members;
            Symbol = symbol;
        }
    }
}