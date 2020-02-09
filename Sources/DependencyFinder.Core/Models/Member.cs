using CSharpFunctionalExtensions;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace DependencyFinder.Core.Models
{
    public class Member : ValueObject
    {
        public string Name { get;  }

        public ISymbol Symbol { get; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
        }

        public Member(string name, ISymbol symbol)
        {
            Name = name;
            Symbol = symbol;
        }
    }

    public class MethodMember : Member
    {
        public MethodMember(string name, ISymbol symbol) : base(name, symbol)
        {
        }
    }

    public class PropertyMember : Member
    {
        public PropertyMember(string name, ISymbol symbol) : base(name, symbol)
        {
        }
    }

    public class EventMember : Member
    {
        public EventMember(string name, ISymbol symbol) : base(name, symbol)
        {
        }
    }

    public class FieldMember : Member
    {
        public FieldMember(string name, ISymbol symbol) : base(name, symbol)
        {
        }
    }
}