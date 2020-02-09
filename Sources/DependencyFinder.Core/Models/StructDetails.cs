using System.Collections.Generic;

namespace DependencyFinder.Core.Models
{
    public class StructDetails : TypeDetails
    {
        public StructDetails(string name, IEnumerable<Member> members)
        {
            Name = name;
            Members = members;
        }
    }
}