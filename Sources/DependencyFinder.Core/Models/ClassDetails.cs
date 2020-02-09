using System.Collections.Generic;

namespace DependencyFinder.Core.Models
{
    public class ClassDetails : TypeDetails
    {
        public ClassDetails(string name, IEnumerable<Member> members)
        {
            Name = name;
            Members = members;
        }
    }
}