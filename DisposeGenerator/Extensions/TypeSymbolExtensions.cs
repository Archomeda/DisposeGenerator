using System.Linq;
using Microsoft.CodeAnalysis;

namespace DisposeGenerator.Extensions
{
    internal static class TypeSymbolExtensions
    {
        /// <summary>
        /// Gets whether a type symbol implements a given interface.
        /// </summary>
        /// <param name="typeSymbol">The type symbol.</param>
        /// <param name="interfaceSymbol">The interface symbol.</param>
        /// <returns><see langword="true"/> if the type symbol implements the interface, <see langword="false"/> otherwise.</returns>
        public static bool ImplementsInterface(this ITypeSymbol? typeSymbol, INamedTypeSymbol interfaceSymbol)
        {
            return typeSymbol?.AllInterfaces
                .Any(x => x.Equals(interfaceSymbol, SymbolEqualityComparer.Default)) ?? false;
        }
    }
}
