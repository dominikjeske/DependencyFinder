using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace DependencyFinder.Core.Models
{
    public class EnumDetails : TypeDetails
    {
        public EnumDetails(string name, IEnumerable<Member> members, ISymbol symbol)
        {
            Name = name;
            Members = members;
            Symbol = symbol;
        }
    }
}