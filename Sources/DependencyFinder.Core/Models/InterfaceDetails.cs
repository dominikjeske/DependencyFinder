using System.Collections.Generic;

namespace DependencyFinder.Core.Models
{
    public class InterfaceDetails : TypeDetails
    {
        public InterfaceDetails(string name, IEnumerable<Member> members)
        {
            Name = name;
            Members = members;
        }
    }
}