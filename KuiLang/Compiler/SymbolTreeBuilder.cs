using KuiLang.Compiler.Symbols;
using KuiLang.Diagnostics;
using KuiLang.Semantic;
using KuiLang.Syntax;
using System;
using System.Linq;
using static KuiLang.Syntax.Ast.Expression;
using static KuiLang.Syntax.Ast.Expression.Literal;
using static KuiLang.Syntax.Ast.Expression.FuncCall;
using static KuiLang.Syntax.Ast.Statement.Definition;
using static KuiLang.Syntax.Ast.Statement.Definition.Typed;

namespace KuiLang.Compiler
{
    public class SymbolTreeBuilder : AstVisitor<object>
    {
        readonly DiagnosticChannel _diagnostics;
        public SymbolTreeBuilder( DiagnosticChannel diagnostics )
        {
            _diagnostics = diagnostics;
        }

        ISymbol _current = null!;

        public override ProgramRootSymbol Visit( Ast ast )
        {
            var root = new ProgramRootSymbol( ast );
            _current = root;
            root.Add( HardcodedSymbols.NumberType );
            base.Visit( ast );
            _current = null!;
            return root;
        }

        // Definitions :

        protected override object Visit( Method method )
        {
            var current = _current switch
            {
                ISymbolWithMethods methodHolder => methodHolder,
                StatementBlockSymbol block when block.Parent is ProgramRootSymbol root => root,
                _ => throw new InvalidOperationException( "Unknown method parent" )
            };
            var symbol = new MethodSymbol( current, method );
            current.Methods.Add( symbol.Name, symbol );
            _current = symbol;
            base.Visit( method );
            _current = symbol.Parent;
            return default!;
        }

        protected override object Visit( Parameter ast )
        {
            var curr = (MethodSymbol)_current;
            var symbol = new MethodParameterSymbol( ast, curr );

            curr.ParameterSymbols.Add( ast.Name, symbol );

            return default!;
        }

        protected override object Visit( Ast.Statement.Definition.Type type )
        {
            var current = (ProgramRootSymbol)_current.Parent!;
            var symbol = new TypeSymbol( current, type );
            current.Add( symbol );
            _current = symbol;
            base.Visit( type );
            _current = symbol.Parent;
            return default!;
        }

        protected override object Visit( Field field )
        {
            if( _current is TypeSymbol type )
            {
                var fieldSymbol = new FieldSymbol( field, type );
                type.Fields.Add( fieldSymbol.Name, fieldSymbol );
                _current = fieldSymbol;
                fieldSymbol.InitValue = field.InitValue != null ? Visit( field.InitValue ) : null;
                _current = fieldSymbol.Parent;
                return default!;
            }
            var symbol = new VariableSymbol( _current, null!, field );
            _current = symbol;
            symbol.InitValue = field.InitValue != null ? Visit( field.InitValue ) : null;
            _current = symbol.Parent!;

            if( _current is ISymbolWithAStatement singleStatement )
            {
                _diagnostics.EmitDiagnostic( Diagnostic.FieldSingleStatement( field ) );
            }
            return default!;
        }

        // Statements:

        protected override object Visit( Ast.Statement.Block ast )
        {
            var symbol = new StatementBlockSymbol( _current, ast );
            _current = symbol;
            var res = base.Visit( ast );
            _current = symbol.Parent!;
            return res;
        }

        protected override object Visit( Ast.Statement statement )
        {
            return base.Visit( statement );
        }


        protected override object Visit( Ast.Statement.FieldAssignation assignation )
        {
            var symbol = new FieldAssignationStatementSymbol
            (
                (StatementSymbol)_current,
                assignation
            );
            var prev = _current;
            _current = symbol;
            symbol.FieldOwner = (IdentifierValueExpressionSymbol)Visit( assignation.FieldSelector );
            symbol.NewFieldValue = Visit( assignation.NewFieldValue );
            base.Visit( assignation );
            _current = prev;
            return default!;
        }

        protected override object Visit( Ast.Statement.If @if )
        {
            var symbol = new IfStatementSymbol( (StatementSymbol)_current, @if );
            var prev = _current;
            _current = symbol;
            symbol.Condition = Visit( @if.Condition );
            var res = Visit( @if.TheStatement );
            _current = prev;
            return res;
        }

        protected override object Visit( Ast.Statement.Return returnStatement )
        {
            var symbol = new ReturnStatementSymbol( _current, returnStatement );
            var prev = _current;
            _current = symbol;
            symbol.ReturnedValue = returnStatement.ReturnedValue != null ? Visit( returnStatement.ReturnedValue ) : null;
            _current = prev;
            return default!;
        }

        protected override object Visit( Ast.Statement.MethodCallStatement methodCallStatement )
        {
            var symbol = new MethodCallStatementSymbol( _current, methodCallStatement );
            var prev = _current;
            _current = symbol;
            symbol.MethodCallExpression = Visit( methodCallStatement.MethodCallExpression );
            _current = prev;
            return default!;
        }

        // Expressions:

        protected override IExpression Visit( Ast.Expression expression ) => (IExpression)base.Visit( expression );

        protected override FunctionCallExpressionSymbol Visit( FuncCall methodCall )
        {
            var prev = _current;
            var expr = new FunctionCallExpressionSymbol( _current, methodCall );
            _current = expr;
            expr.Arguments = methodCall.Arguments.Select( Visit ).ToList();
            _current = prev;
            return expr;
        }

        protected override IdentifierValueExpressionSymbol Visit( IdentifierValue variable )
            => new( _current, variable );

        protected override IExpression Visit( Literal literal ) => (IExpression)base.Visit( literal );
        protected override NumberLiteralSymbol Visit( Number constant ) => new( _current, constant );


        protected override FunctionCallExpressionSymbol Visit( Operator @operator )
        {
            var prev = _current;
            var expr = new FunctionCallExpressionSymbol( _current, @operator );
            Visit( @operator.Left );
            _current = expr;
            expr.Arguments = new[]
            {
                Visit( @operator.Right )
            };
            _current = prev;
            return expr;
        }

    }
}
