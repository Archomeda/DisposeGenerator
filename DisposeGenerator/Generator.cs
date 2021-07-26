using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DisposeGenerator.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DisposeGenerator
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        private const string GENERATOR_NAMESPACE = nameof(DisposeGenerator) + ".Attributes";
        private const string DISPOSE_ALL_ATTRIBUTE = "DisposeAllAttribute";
        private const string EXCLUDE_DISPOSE_ATTRIBUTE = "ExcludeDisposeAttribute";
        private const string INCLUDE_DISPOSE_ATTRIBUTE = "IncludeDisposeAttribute";
        private const string DISPOSER_ATTRIBUTE = "DisposerAttribute";
        private const string FINALIZER_ATTRIBUTE = "FinalizerAttribute";
        private const string ASYNC_DISPOSER_ATTRIBUTE = "AsyncDisposerAttribute";

        private const string TYPE_IDISPOSABLE = "System.IDisposable";
        private const string TYPE_IASYNCDISPOSABLE = "System.IAsyncDisposable";

        private static string GetAttributeFileName(string attribute) =>
            $"{GENERATOR_NAMESPACE}.{attribute}.cs";

        private static string GetAttributeName(string attribute) =>
            $"{GENERATOR_NAMESPACE}.{attribute}";

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(x =>
            {
                AddSourceCopy(x.AddSource, GetAttributeFileName(DISPOSE_ALL_ATTRIBUTE));
                AddSourceCopy(x.AddSource, GetAttributeFileName(EXCLUDE_DISPOSE_ATTRIBUTE));
                AddSourceCopy(x.AddSource, GetAttributeFileName(INCLUDE_DISPOSE_ATTRIBUTE));
                AddSourceCopy(x.AddSource, GetAttributeFileName(DISPOSER_ATTRIBUTE));
                AddSourceCopy(x.AddSource, GetAttributeFileName(FINALIZER_ATTRIBUTE));
                AddSourceCopy(x.AddSource, GetAttributeFileName(ASYNC_DISPOSER_ATTRIBUTE));
            });

            context.RegisterForSyntaxNotifications(() => new DisposeSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not DisposeSyntaxReceiver receiver)
                return;

            foreach (var classInfo in receiver.DisposableClasses)
            {
                var builder = new StringBuilder();

                // Usings
                builder.AppendLine("using System;");
                builder.AppendLine("using System.Threading.Tasks;");

                // >>> Open namespace
                builder.AppendLine($"namespace {classInfo.Namespace}");
                builder.AppendLine("{");

                // >>> Open class
                string interfaces = $"IDisposable {(classInfo.ImplementsAsyncDisposable ? ", IAsyncDisposable" : "")}";
                builder.AppendLine($"{classInfo.Modifiers} class {classInfo.Name} : {interfaces}");
                builder.AppendLine("{");

                builder.AppendLine("/// <summary>");
                builder.AppendLine($"/// This value indicates whether this <see cref=\"{classInfo.Name}\"/> instance has been disposed in order to detect redundant calls.");
                builder.AppendLine("/// </summary>");
                builder.AppendLine("private bool isDisposed = false;");
                builder.AppendLine();

                AppendDisposeMethod(builder, classInfo);
                if (classInfo.ImplementsAsyncDisposable)
                    AppendDisposeAsyncMethod(builder, classInfo);

                builder.AppendLine();

                // <<< Close class
                builder.AppendLine("}");

                // <<< Close namespace
                builder.AppendLine("}");

                // Add generated source
                string source = CSharpSyntaxTree
                    .ParseText(builder.ToString())
                    .GetRoot()
                    .NormalizeWhitespace()
                    .ToFullString();
                context.AddSource($"{classInfo.Name}.g.cs", source);
            }
        }

        private static void AppendDisposeMethod(StringBuilder builder, DisposableClassInfo classInfo)
        {
            if (classInfo.FinalizerMethodNames.Count > 0)
            {
                // Add finalizer
                builder.AppendLine($"~{classInfo.Name}()");
                builder.AppendLine("{");
                builder.AppendLine("this.Dispose(false);");
                builder.AppendLine("}");
            }


            if (!classInfo.BaseImplementsDisposable)
            {
                // >>> Open Dispose()
                builder.AppendLine("/// <summary>");
                builder.AppendLine("/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.");
                builder.AppendLine("/// </summary>");
                builder.AppendLine("public void Dispose()");
                builder.AppendLine("{");

                if (classInfo.IsSealed)
                {
                    // Sealed classes can implement the full dispose in the main Dispose()
                    AppendDisposeCheck();
                    AppendDisposeCalls();
                    AppendFinalizeCalls();
                    AppendSetDisposed();
                }
                else
                {
                    // Non-sealed classes should redirect the dispose to Dispose(bool)
                    builder.AppendLine("this.Dispose(true);");
                    builder.AppendLine("GC.SuppressFinalize(this);");
                }

                // <<< Close Dispose()
                builder.AppendLine("}");
            }


            if (!classInfo.IsSealed)
            {
                // Only non-sealed classes should implement Dispose(bool)

                // >>> Open Dispose(bool)
                builder.AppendLine("/// <summary>");
                builder.AppendLine("/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.");
                builder.AppendLine("/// This method can be overridden in derived classes.");
                builder.AppendLine("/// </summary>");
                builder.AppendLine("/// <param name=\"disposing\">The parameter indicating whether managed resources should be disposed.</param>");
                if (!classInfo.BaseImplementsDisposable)
                    builder.AppendLine("protected virtual void Dispose(bool disposing)");
                else
                    builder.AppendLine("protected override void Dispose(bool disposing)");
                builder.AppendLine("{");

                AppendDisposeCheck();
                builder.AppendLine("if (disposing)");
                builder.AppendLine("{");
                AppendDisposeCalls();
                builder.AppendLine("}");
                AppendFinalizeCalls();
                AppendClear();
                AppendSetDisposed();

                if (classInfo.BaseImplementsDisposable)
                    builder.AppendLine("base.Dispose(disposing);");

                // <<< Close Dispose(bool)
                builder.AppendLine("}");
            }


            void AppendDisposeCheck()
            {
                builder.AppendLine("if (this.isDisposed)");
                builder.AppendLine("{");
                builder.AppendLine("return;");
                builder.AppendLine("}");
            }

            void AppendDisposeCalls()
            {
                foreach (var member in classInfo.ManagedMembers)
                {
                    foreach (string? name in member.Names)
                        builder.AppendLine($"(this.{name} as IDisposable)?.Dispose();");
                }

                // Call additional manual dispose methods
                foreach (string name in classInfo.DisposerMethodNames)
                    builder.AppendLine($"this.{name}();");
            }

            void AppendFinalizeCalls()
            {
                foreach (string name in classInfo.FinalizerMethodNames)
                    builder.AppendLine($"this.{name}();");
            }

            void AppendClear()
            {
                foreach (var member in classInfo.ManagedMembers)
                {
                    foreach (string? name in member.Names)
                        builder.AppendLine($"this.{name} = null;");
                }
            }

            void AppendSetDisposed()
            {
                builder.AppendLine("this.isDisposed = true;");
            }
        }

        private static void AppendDisposeAsyncMethod(StringBuilder builder, DisposableClassInfo classInfo)
        {
            if (!classInfo.BaseImplementsAsyncDisposable)
            {
                // >>> Open DisposeAsync()
                builder.AppendLine("/// <summary>");
                builder.AppendLine("/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.");
                builder.AppendLine("/// </summary>");
                builder.AppendLine("/// <returns>A task that represents the asynchronous dispose operation.</returns>");
                builder.AppendLine("public async ValueTask DisposeAsync()");
                builder.AppendLine("{");

                if (classInfo.IsSealed)
                {
                    // Sealed classes can implement the full async dispose in the main DisposeAsync()
                    AppendAsyncDisposeCalls();
                }
                else
                {
                    // Non-sealed classes should redirect the async dispose to DisposeAsyncCore() and Dispose(bool)
                    builder.AppendLine("await this.DisposeAsyncCore();");
                    builder.AppendLine("this.Dispose(false);");
                    builder.AppendLine("GC.SuppressFinalize(this);");
                }

                // <<< Close DisposeAsync()
                builder.AppendLine("}");
            }


            if (!classInfo.IsSealed)
            {
                // Only non-sealed classes should implement DisposeAsyncCore()

                // >>> Open DisposeAsyncCore()
                builder.AppendLine("/// <summary>");
                builder.AppendLine("/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.");
                builder.AppendLine("/// This method can be overridden in derived classes.");
                builder.AppendLine("/// </summary>");
                builder.AppendLine("/// <returns>A task that represents the asynchronous dispose operation.</returns>");
                if (!classInfo.BaseImplementsAsyncDisposable)
                    builder.AppendLine("protected virtual async ValueTask DisposeAsyncCore()");
                else
                    builder.AppendLine("protected override async ValueTask DisposeAsyncCore()");
                builder.AppendLine("{");

                AppendAsyncDisposeCalls();
                AppendClear();

                if (classInfo.BaseImplementsAsyncDisposable)
                    builder.AppendLine("await base.DisposeAsyncCore();");

                // <<< Close Dispose(bool)
                builder.AppendLine("}");
            }


            void AppendAsyncDisposeCalls()
            {
                foreach (var member in classInfo.ManagedMembers)
                {
                    foreach (string? name in member.Names)
                    {
                        builder.AppendLine($"if (this.{name} is IAsyncDisposable asyncDisposable_{name})");
                        builder.AppendLine("{");
                        builder.AppendLine($"await asyncDisposable_{name}.DisposeAsync().ConfigureAwait(false);");
                        builder.AppendLine("}");
                        builder.AppendLine("else");
                        builder.AppendLine("{");
                        builder.AppendLine($"this.{name}?.Dispose();");
                        builder.AppendLine("}");
                    }
                }

                // Call additional manual async dispose methods
                foreach (string name in classInfo.AsyncDisposerMethodNames)
                    builder.AppendLine($"await this.{name}().ConfigureAwait(false);");

                // Call additional manual non-async dispose methods
                foreach (string name in classInfo.DisposerMethodNames)
                    builder.AppendLine($"this.{name}();");
            }

            void AppendClear()
            {
                foreach (var member in classInfo.ManagedMembers)
                {
                    foreach (string? name in member.Names)
                        builder.AppendLine($"this.{name} = null;");
                }
            }
        }


        private static void AddSourceCopy(Action<string, string> addSourceAction, string fileName)
        {
            var type = typeof(Generator).Assembly;
            using var stream = type.GetManifestResourceStream(fileName) ?? throw new InvalidOperationException($"{fileName} does not exist as embedded resource");

            using var reader = new StreamReader(stream);
            string text = reader.ReadToEnd();
            addSourceAction(fileName, text);
        }

        private class DisposeSyntaxReceiver : ISyntaxContextReceiver
        {
            public IList<DisposableClassInfo> DisposableClasses { get; } = new List<DisposableClassInfo>();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
                    // Skip non-classes
                    return;

                if (!classDeclarationSyntax.Modifiers.Any(x => x.Kind() == SyntaxKind.PartialKeyword))
                    // Skip non-partial classes
                    return;

                // Get IDisposable type
                var iDisposable = context.SemanticModel.Compilation.GetTypeByMetadataName(TYPE_IDISPOSABLE);
                if (iDisposable is null)
                    throw new InvalidOperationException($"Unexpected error: Type {TYPE_IDISPOSABLE} was not found");

                // Get IAsyncDisposable type
                // Note: IAsyncDisposable may not exist <.NET Standard 2.1 or without Microsoft.Bcl.AsyncInterfaces
                var iAsyncDisposable = context.SemanticModel.Compilation.GetTypeByMetadataName(TYPE_IASYNCDISPOSABLE);

                bool implementsDisposable = ImplementsInterface(context, classDeclarationSyntax, iDisposable);
                bool implementsAsyncDisposable = iAsyncDisposable is not null && ImplementsInterface(context, classDeclarationSyntax, iAsyncDisposable);
                if (!implementsDisposable && !implementsAsyncDisposable)
                    // Skip classes that do not implement IDisposable and/or IAsyncDisposable
                    return;

                // Get the dispose generator mode
                // The DisposeAllAttribute will make the generator act where all disposable fields are included by default;
                // without the attribute all disposable fields are excluded by default.
                bool disposeByDefault = HasAttribute(context, classDeclarationSyntax, GetAttributeName(DISPOSE_ALL_ATTRIBUTE));

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
                bool baseImplementsDisposable = baseSymbol is not null && ImplementsInterface(baseSymbol, iDisposable);
                bool baseImplementsAsyncDisposable = baseSymbol is not null && iAsyncDisposable is not null && ImplementsInterface(baseSymbol, iAsyncDisposable);

                //TODO: Check if base classes properly implement the IDisposable / IAsyncDisposable pattern.
                //      For now, assume they do.

                //bool hasOverrideableBaseDisposeMethod = ImplementsMethodInBase(
                //    context,
                //    classDeclarationSyntax,
                //    "Dispose",
                //    context.SemanticModel.Compilation.GetTypeByMetadataName("void"),
                //    new[] { context.SemanticModel.Compilation.GetTypeByMetadataName("bool")! });

                var disposerMethodNames = FindAllNamesOfMethodsWithAttribute(context, classDeclarationSyntax, GetAttributeName(DISPOSER_ATTRIBUTE)).ToList();
                var finalizerMethodNames = FindAllNamesOfMethodsWithAttribute(context, classDeclarationSyntax, GetAttributeName(FINALIZER_ATTRIBUTE)).ToList();
                var asyncDisposerMethodNames = FindAllNamesOfMethodsWithAttribute(context, classDeclarationSyntax, GetAttributeName(ASYNC_DISPOSER_ATTRIBUTE)).ToList();

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
                        bool implementsInterface = ImplementsInterface(context, x.Declaration.Type, iDisposable);
                        bool hasInclude = HasAttribute(context, x, GetAttributeName(INCLUDE_DISPOSE_ATTRIBUTE));
                        bool hasExclude = HasAttribute(context, x, GetAttributeName(EXCLUDE_DISPOSE_ATTRIBUTE));

                        return disposeByDefault
                            ? implementsInterface && !hasExclude
                            : implementsInterface && hasInclude && !hasExclude;
                    })
                    .Select(x =>
                    {
                        bool fieldImplementsAsyncDisposable = iAsyncDisposable is not null && ImplementsInterface(context, x.Declaration.Type, iAsyncDisposable);
                        return new DisposableMemberInfo(x)
                        {
                            ImplementsAsyncDisposable = fieldImplementsAsyncDisposable
                        };
                    });
            }

            private static IEnumerable<string> FindAllNamesOfMethodsWithAttribute(GeneratorSyntaxContext context, ClassDeclarationSyntax declaration,
                string attributeName)
            {
                return declaration.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Where(x => HasAttribute(context, x, attributeName))
                    .Select(x => x.Identifier.ToString());
            }

            private static bool HasAttribute(GeneratorSyntaxContext context, MemberDeclarationSyntax declaration, string fullyQualifiedTypeName)
            {
                var attributes = declaration.AttributeLists.SelectMany(x => x.Attributes);
                var attributeNames = attributes.Select(x => context.SemanticModel.GetTypeInfo(x).ConvertedType?.ToDisplayString());
                return attributeNames.Any(x => x == fullyQualifiedTypeName);
            }

            private static bool ImplementsInterface(GeneratorSyntaxContext context, TypeSyntax type, INamedTypeSymbol interfaceSymbol)
            {
                var typeSymbol = context.SemanticModel.GetTypeInfo(type).ConvertedType;
                return ImplementsInterface(typeSymbol, interfaceSymbol);
            }

            private static bool ImplementsInterface(GeneratorSyntaxContext context, BaseTypeDeclarationSyntax declaration, INamedTypeSymbol interfaceSymbol)
            {
                var typeSymbol = context.SemanticModel.GetDeclaredSymbol(declaration);
                return ImplementsInterface(typeSymbol, interfaceSymbol);
            }

            private static bool ImplementsInterface(ITypeSymbol? typeSymbol, INamedTypeSymbol interfaceSymbol)
            {
                return ImplementsInterface(typeSymbol?.AllInterfaces, interfaceSymbol);
            }

            private static bool ImplementsInterface(IEnumerable<INamedTypeSymbol>? interfaces, INamedTypeSymbol interfaceSymbol)
            {
                return interfaces?.Any(x => x.Equals(interfaceSymbol, SymbolEqualityComparer.Default)) ?? false;
            }

            private static bool ImplementsMethodInBase(GeneratorSyntaxContext context, ClassDeclarationSyntax declaration, string name,
                ITypeSymbol? returnType, IEnumerable<INamedTypeSymbol>? parameterTypes)
            {
                var baseSymbol = context.SemanticModel.GetDeclaredSymbol(declaration)?.BaseType;
                while (baseSymbol is not null)
                {
                    var members = baseSymbol
                        .GetMembers(name)
                        .OfType<IMethodSymbol>()
                        .Where(x => x.IsVirtual)
                        .Where(x => x.ReturnType.Equals(returnType, SymbolEqualityComparer.Default))
                        .Where(x => x.Parameters.Equals(parameterTypes));

                    if (members.Any())
                        return true;

                    baseSymbol = baseSymbol?.BaseType;
                }

                return false;
            }
        }
    }
}
