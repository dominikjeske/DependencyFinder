using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace DependencyFinder.Core.Models
{
    public class StructDetails : TypeDetails
    {
        public StructDetails(string name, IEnumerable<Member> members, ISymbol symbol)
        {
            Name = name;
            Members = members;
            Symbol = symbol;
        }
    }
}