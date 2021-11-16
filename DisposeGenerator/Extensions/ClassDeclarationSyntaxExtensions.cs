using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DisposeGenerator.Extensions
{
    internal static class ClassDeclarationSyntaxExtensions
    {
        /// <summary>
        /// Finds all methods of a class declaration that have a given attribute attached.
        /// </summary>
        /// <param name="declaration">The class declaration.</param>
        /// <param name="context">The context.</param>
        /// <param name="attributeFullyQualifiedName">The fully qualified name of the attribute.</param>
        /// <returns>The list of methods that have the attribute attached.</returns>
        public static IEnumerable<MethodDeclarationSyntax> FindMethodsWithAttribute(this ClassDeclarationSyntax declaration, GeneratorSyntaxContext context, string attributeFullyQualifiedName)
        {
            return declaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(x => x.HasAttribute(context, attributeFullyQualifiedName));
        }
    }
}
