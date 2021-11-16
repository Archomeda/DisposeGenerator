using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DisposeGenerator.Extensions
{
    internal static class TypeSyntaxExtensions
    {
        /// <summary>
        /// Gets whether a type syntax implements a given interface.
        /// </summary>
        /// <param name="type">The type syntax.</param>
        /// <param name="context">The context.</param>
        /// <param name="interfaceSymbol">The interface symbol.</param>
        /// <returns><see langword="true"/> if the type syntax implements the interface, <see langword="false"/> otherwise.</returns>
        public static bool ImplementsInterface(this TypeSyntax type, GeneratorSyntaxContext context, INamedTypeSymbol interfaceSymbol) =>
            context.SemanticModel.GetTypeInfo(type).ConvertedType.ImplementsInterface(interfaceSymbol);
    }
}
