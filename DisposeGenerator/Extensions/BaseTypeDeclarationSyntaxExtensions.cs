using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DisposeGenerator.Extensions
{
    internal static class BaseTypeDeclarationSyntaxExtensions
    {
        /// <summary>
        /// Gets whether a base type declaration implements a given interface.
        /// </summary>
        /// <param name="declaration">The base type declaration.</param>
        /// <param name="context">The context.</param>
        /// <param name="interfaceSymbol">The interface symbol.</param>
        /// <returns><see langword="true"/> if the base type declaration implements the interface, <see langword="false"/> otherwise.</returns>
        public static bool ImplementsInterface(this BaseTypeDeclarationSyntax declaration, GeneratorSyntaxContext context, INamedTypeSymbol interfaceSymbol) =>
            context.SemanticModel.GetDeclaredSymbol(declaration).ImplementsInterface(interfaceSymbol);
    }
}
