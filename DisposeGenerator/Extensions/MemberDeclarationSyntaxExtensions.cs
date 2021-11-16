using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DisposeGenerator.Extensions
{
    internal static class MemberDeclarationSyntaxExtensions
    {
        /// <summary>
        /// Gets whether a member declaration has a given attribute attached.
        /// </summary>
        /// <param name="declaration">The member declaration.</param>
        /// <param name="context">The context.</param>
        /// <param name="attributeFullyQualifiedName">The fully qualified name of the attribute.</param>
        /// <returns><see langword="true"/> if the member declaration has the attribute attached, <see langword="false"/> otherwise.</returns>
        public static bool HasAttribute(this MemberDeclarationSyntax declaration, GeneratorSyntaxContext context, string attributeFullyQualifiedName)
        {
            var attributes = declaration.AttributeLists.SelectMany(x => x.Attributes);
            var attributeNames = attributes.Select(x => context.SemanticModel.GetTypeInfo(x).ConvertedType?.ToDisplayString());
            return attributeNames.Any(x => x == attributeFullyQualifiedName);
        }
    }
}
