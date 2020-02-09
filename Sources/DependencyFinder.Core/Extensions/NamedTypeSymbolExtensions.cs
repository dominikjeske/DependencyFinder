using DependencyFinder.Core.Models;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace DependencyFinder
{
    public static class NamedTypeSymbolExtensions
    {
        public static IEnumerable<Member> GetClassMembers(this INamedTypeSymbol namedTypeSymbol)
        {
            return ((IEnumerable<Member>)(namedTypeSymbol.GetMembers()
                                        .Where(x => x.CanBeReferencedByName && x.Kind == SymbolKind.Method)
                                        .Select(y => new MethodMember { Name = y.Name })
                                        )).Union
                                        (namedTypeSymbol.GetMembers()
                                        .Where(x => x.CanBeReferencedByName && x.Kind == SymbolKind.Property)
                                        .Select(y => new PropertyMember { Name = y.Name }))
                                        .Union
                                        (namedTypeSymbol.GetMembers()
                                        .Where(x => x.CanBeReferencedByName && x.Kind == SymbolKind.Field)
                                        .Select(y => new FieldMember { Name = y.Name }))
                                        .Union
                                        (namedTypeSymbol.GetMembers()
                                        .Where(x => x.CanBeReferencedByName && x.Kind == SymbolKind.Event)
                                        .Select(y => new EventMember { Name = y.Name }));
        }

        public static IEnumerable<Member> GetMethodMembers(this INamedTypeSymbol namedTypeSymbol)
        {
            return namedTypeSymbol.GetMembers()
                                            .Where(x => x.CanBeReferencedByName)
                                            .Select(y => new MethodMember
                                            {
                                                Name = y.Name
                                            });
        }

        public static IEnumerable<Member> GetPropertyMembers(this INamedTypeSymbol namedTypeSymbol)
        {
            return namedTypeSymbol.GetMembers()
                                               .Where(x => x.CanBeReferencedByName)
                                               .Select(y => new PropertyMember
                                               {
                                                   Name = y.Name
                                               });
        }
    }
}