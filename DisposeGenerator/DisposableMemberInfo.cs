using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DisposeGenerator
{
    internal class DisposableMemberInfo
    {
        public DisposableMemberInfo(MemberDeclarationSyntax syntax)
        {
            this.Syntax = syntax;
        }

        public MemberDeclarationSyntax Syntax { get; set; }

        public IEnumerable<string> Names
        {
            get
            {
                if (this.Syntax is FieldDeclarationSyntax field)
                    return field.Declaration.Variables.Select(x => x.Identifier.ToString());
                else if (this.Syntax is PropertyDeclarationSyntax property)
                    return new[] { property.Identifier.ToString() };

                throw new NotSupportedException($"Type {this.Syntax.GetType().Name} is not yet supported for generating disposing members automatically");
            }
        }

        public bool SetNull { get; set; }

        public bool ImplementsAsyncDisposable { get; set; }
    }
}
