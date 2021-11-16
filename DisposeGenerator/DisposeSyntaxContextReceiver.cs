using System;
using System.Collections.Generic;
using System.Linq;
using DisposeGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DisposeGenerator
{
    internal class DisposeSyntaxContextReceiver : ISyntaxContextReceiver
    {
        private const string TYPE_IDISPOSABLE = "System.IDisposable";
        private const string TYPE_IASYNCDISPOSABLE = "System.IAsyncDisposable";


        public IList<DisposableClassInfo> DisposableClasses { get; } = new List<DisposableClassInfo>();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
                // Skip non-classes
                return;

            if (!classDeclarationSyntax.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
                // Skip non-partial classes
                return;

            // Get IDisposable type
            var iDisposable = context.SemanticModel.Compilation.GetTypeByMetadataName(TYPE_IDISPOSABLE);
            if (iDisposable is null)
                throw new InvalidOperationException($"Unexpected error: Type {TYPE_IDISPOSABLE} was not found");

            // Get IAsyncDisposable type
            // Note: IAsyncDisposable may not exist <.NET Standard 2.1 or without Microsoft.Bcl.AsyncInterfaces
            var iAsyncDisposable = context.SemanticModel.Compilation.GetTypeByMetadataName(TYPE_IASYNCDISPOSABLE);

            bool implementsDisposable = classDeclarationSyntax.ImplementsInterface(context, iDisposable);
            bool implementsAsyncDisposable = iAsyncDisposable is not null && classDeclarationSyntax.ImplementsInterface(context, iAsyncDisposable);
            if (!implementsDisposable && !implementsAsyncDisposable)
                // Skip classes that do not implement IDisposable and/or IAsyncDisposable
                return;

            // Get the dispose generator mode
            // The DisposeAllAttribute will make the generator act where all disposable fields are included by default;
            // without the attribute all disposable fields are excluded by default.
            bool disposeByDefault = classDeclarationSyntax.HasAttribute(context, EmbeddedFiles.DisposeAllAttributeFQN);

            var disposableMembers = FindAllDisposableMembers(context, classDeclarationSyntax, iDisposable, iAsyncDisposable, disposeByDefault).ToList();
            if (disposableMembers.Any(x => x.ImplementsAsyncDisposable))
                implementsAsyncDisposable = true; // Override to have the requested type also implement IAsyncDisposable

            //var baseDisposeMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "Dispose")
            //    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword)))
            //    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new[]
            //    {
            //        SyntaxFactory.Parameter(
            //            SyntaxFactory.List<AttributeListSyntax>(),
            //            SyntaxFactory.TokenList(),
            //            SyntaxFactory.ParseTypeName("bool"),
            //            SyntaxFactory.Identifier("disposing"),
            //            null)
            //    })));

            var baseSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax)?.BaseType;
            bool baseImplementsDisposable = baseSymbol is not null && baseSymbol.ImplementsInterface(iDisposable);
            bool baseImplementsAsyncDisposable = baseSymbol is not null && iAsyncDisposable is not null && baseSymbol.ImplementsInterface(iAsyncDisposable);

            //TODO: Check if base classes properly implement the IDisposable / IAsyncDisposable pattern.
            //      For now, assume they do.

            //bool hasOverrideableBaseDisposeMethod = ImplementsMethodInBase(
            //    context,
            //    classDeclarationSyntax,
            //    "Dispose",
            //    context.SemanticModel.Compilation.GetTypeByMetadataName("void"),
            //    new[] { context.SemanticModel.Compilation.GetTypeByMetadataName("bool")! });

            var disposerMethodNames = classDeclarationSyntax
                .FindMethodsWithAttribute(context, EmbeddedFiles.DisposerAttributeFQN)
                .Select(x => x.Identifier.ToString());
            var finalizerMethodNames = classDeclarationSyntax
                .FindMethodsWithAttribute(context, EmbeddedFiles.FinalizerAttributeFQN)
                .Select(x => x.Identifier.ToString());
            var asyncDisposerMethodNames = classDeclarationSyntax
                .FindMethodsWithAttribute(context, EmbeddedFiles.AsyncDisposerAttributeFQN)
                .Select(x => x.Identifier.ToString());

            var info = new DisposableClassInfo(classDeclarationSyntax)
            {
                ImplementsAsyncDisposable = implementsAsyncDisposable,
                BaseImplementsDisposable = baseImplementsDisposable,
                BaseImplementsAsyncDisposable = baseImplementsAsyncDisposable,
                ManagedMembers = { disposableMembers },
                DisposerMethodNames = { disposerMethodNames },
                FinalizerMethodNames = { finalizerMethodNames },
                AsyncDisposerMethodNames = { asyncDisposerMethodNames }
            };

            this.DisposableClasses.Add(info);
        }

        private static IEnumerable<DisposableMemberInfo> FindAllDisposableMembers(GeneratorSyntaxContext context, ClassDeclarationSyntax declaration,
            INamedTypeSymbol iDisposable, INamedTypeSymbol? iAsyncDisposable, bool disposeByDefault)
        {
            return declaration.Members
                .OfType<FieldDeclarationSyntax>()
                .Where(x =>
                {
                    bool implementsInterface = x.Declaration.Type.ImplementsInterface(context, iDisposable);
                    bool hasInclude = x.HasAttribute(context, EmbeddedFiles.IncludeDisposeAttributeFQN);
                    bool hasExclude = x.HasAttribute(context, EmbeddedFiles.ExcludeDisposeAttributeFQN);

                    return disposeByDefault
                        ? implementsInterface && !hasExclude
                        : implementsInterface && hasInclude && !hasExclude;
                })
                .Select(x =>
                {
                    bool fieldImplementsAsyncDisposable = iAsyncDisposable is not null && x.Declaration.Type.ImplementsInterface(context, iAsyncDisposable);
                    return new DisposableMemberInfo(x)
                    {
                        ImplementsAsyncDisposable = fieldImplementsAsyncDisposable
                    };
                });
        }

        //private static bool ImplementsMethodInBase(GeneratorSyntaxContext context, ClassDeclarationSyntax declaration, string name,
        //    ITypeSymbol? returnType, IEnumerable<INamedTypeSymbol>? parameterTypes)
        //{
        //    var baseSymbol = context.SemanticModel.GetDeclaredSymbol(declaration)?.BaseType;
        //    while (baseSymbol is not null)
        //    {
        //        var members = baseSymbol
        //            .GetMembers(name)
        //            .OfType<IMethodSymbol>()
        //            .Where(x => x.IsVirtual)
        //            .Where(x => x.ReturnType.Equals(returnType, SymbolEqualityComparer.Default))
        //            .Where(x => x.Parameters.Equals(parameterTypes));

        //        if (members.Any())
        //            return true;

        //        baseSymbol = baseSymbol?.BaseType;
        //    }

        //    return false;
        //}
    }
}
