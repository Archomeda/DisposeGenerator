using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DisposeGenerator
{
    [Generator]
    public class DisposeSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(x =>
            {
                GeneratorUtils.AddEmbeddedSourceCopy(x.AddSource, EmbeddedFiles.DISPOSE_ALL_ATTRIBUTE_NAME);
                GeneratorUtils.AddEmbeddedSourceCopy(x.AddSource, EmbeddedFiles.EXCLUDE_DISPOSE_ATTRIBUTE_NAME);
                GeneratorUtils.AddEmbeddedSourceCopy(x.AddSource, EmbeddedFiles.INCLUDE_DISPOSE_ATTRIBUTE_NAME);
                GeneratorUtils.AddEmbeddedSourceCopy(x.AddSource, EmbeddedFiles.DISPOSER_ATTRIBUTE_NAME);
                GeneratorUtils.AddEmbeddedSourceCopy(x.AddSource, EmbeddedFiles.FINALIZER_ATTRIBUTE_NAME);
                GeneratorUtils.AddEmbeddedSourceCopy(x.AddSource, EmbeddedFiles.ASYNC_DISPOSER_ATTRIBUTE_NAME);
            });

            context.RegisterForSyntaxNotifications(() => new DisposeSyntaxContextReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not DisposeSyntaxContextReceiver receiver)
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
    }
}
