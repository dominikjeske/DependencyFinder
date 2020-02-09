using System.Collections.Generic;

namespace DependencyFinder.Core.Models
{
    public class EnumDetails : TypeDetails
    {
        public EnumDetails(string name, IEnumerable<Member> members)
        {
            Name = name;
            Members = members;
        }
    }
}