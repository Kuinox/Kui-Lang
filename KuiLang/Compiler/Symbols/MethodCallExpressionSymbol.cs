using KuiLang.Semantic;
using KuiLang.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuiLang.Compiler.Symbols
{
    public class MethodCallExpressionSymbol : IExpression
    {
        public MethodCallExpressionSymbol(  ISymbol parent, Ast.Expression.FuncCall.MethodCall ast )
        {
            Parent = parent;
            Ast = ast;
        }

        public ISymbol Parent { get; }
        public Ast.Expression.FuncCall.MethodCall Ast { get; }
        public IReadOnlyList<IExpression> Arguments { get; internal set; } = null!;

        public TypeSymbol ReturnType => TargetMethod.ReturnType;
        public MethodSymbol TargetMethod { get; internal set; } = null!; // resolve member step.
        public IExpression CallTarget { get; internal set; } = null!;
    }
}
