using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DisposeGenerator
{
    internal class DisposableClassInfo
    {
        public DisposableClassInfo(ClassDeclarationSyntax syntax)
        {
            this.Syntax = syntax;
        }

        public ClassDeclarationSyntax Syntax { get; set; }

        public string? Namespace =>
            this.Syntax.FirstAncestorOrSelf<NamespaceDeclarationSyntax>()?.Name.ToString();

        public string Name =>
            this.Syntax.Identifier.ToString();

        public string Modifiers =>
            this.Syntax.Modifiers.ToString();

        public bool IsPartial =>
            this.Syntax.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword));

        public bool IsSealed =>
            this.Syntax.Modifiers.Any(x => x.IsKind(SyntaxKind.SealedKeyword));

        public bool ImplementsAsyncDisposable { get; set; }

        public bool BaseImplementsDisposable { get; set; }

        public bool BaseImplementsAsyncDisposable { get; set; }

        //public bool HasOverrideableBaseDisposeMethod { get; set; }

        public IList<DisposableMemberInfo> ManagedMembers { get; } = new List<DisposableMemberInfo>();

        public IList<string> DisposerMethodNames { get; } = new List<string>();

        public IList<string> FinalizerMethodNames { get; } = new List<string>();

        public IList<string> AsyncDisposerMethodNames { get; } = new List<string>();
    }
}
