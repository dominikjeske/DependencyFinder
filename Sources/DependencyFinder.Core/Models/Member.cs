using CSharpFunctionalExtensions;
using System.Collections.Generic;

namespace DependencyFinder.Core.Models
{
    public class Member : ValueObject
    {
        public string Name { get; set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
        }
    }

    public class MethodMember : Member
    {
    }

    public class PropertyMember : Member
    {
    }

    public class EventMember : Member
    {
    }

    public class FieldMember : Member
    {
    }
}