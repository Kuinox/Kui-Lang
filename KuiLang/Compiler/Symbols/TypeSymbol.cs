using KuiLang.Compiler;
using KuiLang.Compiler.Symbols;
using KuiLang.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuiLang.Semantic
{
    public class TypeSymbol : ISymbol, ISymbolWithMethods
    {
        public TypeSymbol( ProgramRootSymbol parent, Ast.Statement.Definition.Type ast )
        {
            Parent = parent;
            Ast = ast;
            Name = ast.Name;
        }

        public ProgramRootSymbol Parent { get; }

        ISymbol? ISymbol.Parent => Parent;

        public Ast.Statement.Definition.Type Ast { get; }
        public string Name { get; }

        public Dictionary<string, MethodSymbol> Methods { get; } = new();

        public OrderedDictionary<string, FieldSymbol> Fields { get; } = new();

        public MethodSymbol Constructor { get; internal set; } = null!;

        public Identifier Identifier => new(Name);
    }
}
