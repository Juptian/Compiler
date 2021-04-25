﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Uranium.CodeAnalysis.Syntax;
using Uranium.CodeAnalysis.Syntax.Expression;
using Uranium.CodeAnalysis.Syntax.Statement;
using Uranium.CodeAnalysis.Text;
using Uranium.Logging;
using Uranium.CodeAnalysis.Binding.NodeKinds;
using Uranium.CodeAnalysis.Binding.Statements;

namespace Uranium.CodeAnalysis.Binding
{
    internal sealed class Binder
    {
        public Binder(BoundScope? parent = null)
        {
            _scope = new(parent);
        }

        //Diagnostics, pretty neat not gonna lie
        private readonly DiagnosticBag _diagnostics = new();
        private BoundScope _scope;

        //Public diagnostics that nobody can edit :C
        public DiagnosticBag Diagnostics => _diagnostics;

        //Binding the expression
        private BoundExpression BindExpression(ExpressionSyntax syntax)
            => syntax.Kind switch // Calling the correct function based off of the syntax kind and returning it's value.
            {
                //Base expressions
                SyntaxKind.BinaryExpression => BindBinaryExpression( (BinaryExpressionSyntax)syntax ),
                SyntaxKind.UnaryExpression => BindUnaryExpression( (UnaryExpressionSyntax)syntax ),
                SyntaxKind.LiteralExpression => BindLiteralExpression( (LiteralExpressionSyntax)syntax ),
                SyntaxKind.ParenthesizedExpression => BindParenthesizedExpression( (ParenthesizedExpressionSyntax)syntax ),

                //Name + Assignments
                SyntaxKind.NameExpression => BindNameExpression( (NameExpressionSyntax)syntax ),
                SyntaxKind.AssignmentExpression => BindAssignmentExpression( (AssignmentExpressionSyntax)syntax ),
                _ => throw new($"Unexpected syntax {syntax.Kind}"),
            };

        private BoundExpression BindExpression(ExpressionSyntax syntax, Type targetType)
        {
            var result = BindExpression(syntax);
            if (result.Type != targetType)
            {
                _diagnostics.ReportCannotConvert(syntax.Span, result.GetType(), targetType);
            }
            return result;
        }
        //Binding the Statement 
        //After making the binder, we call to bind the statement
        private BoundStatement BindStatement(StatementSyntax syntax)
            => syntax.Kind switch // Calling the correct function based off of the syntax kind and returning it's value.
            {
                //Base expressions
                SyntaxKind.BlockStatement => BindBlockStatement( (BlockStatementSyntax)syntax ),
                SyntaxKind.ExpressionStatement => BindExpressionStatement( (ExpressionStatementSyntax)syntax ),
                SyntaxKind.VariableDeclaration => BindVariableDeclaration( (VariableDeclarationSyntax)syntax ),
                SyntaxKind.IfStatement => BindIfStatement( (IfStatementSyntax)syntax ),
                SyntaxKind.WhileStatement => BindWhileStatement( (WhileStatementSyntax)syntax ),
                //We can throw here because this is all that we allow for now
                //And if we get here, we've exhausted all our options
                _ => throw new($"Unexpected syntax {syntax.Kind}"),
            };

        //It calls this method
        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, CompilationUnitSyntax syntax)
        {
            //This method allows for scope!
            var parentScope = CreateParentScopes(previous);

            //When binding to the global scope, there is no parent to the binder
            //So if parent scope is null, that's perfectly fine!
            var binder = new Binder(parentScope);

            var statement = binder.BindStatement(syntax.Statement);
            //Getting declared variables to allow us to properly report errors of already defined variables
            var variables = binder._scope.GetDeclaredVariables();
            
            //Making the diagnostics immutable allows for less potential bugs
            var diagnostics = binder.Diagnostics.ToImmutableArray();
        
            if(previous is not null)
            {
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);
            }

            return new(previous, diagnostics, variables, statement);
        }

        private static BoundScope? CreateParentScopes(BoundGlobalScope? previous)
        {
            var stack = new Stack<BoundGlobalScope>();
            
            //Pushing our previous scopes onto the stack this way we can get them into reverse order
            while(previous is not null)
            {
                stack.Push(previous);
                previous = previous.Previous ?? null;
            }

            BoundScope? parent = null;

            //Removing the items from stack, while also declaring variables
            while (stack.Count > 0)
            {
                previous = stack.Pop();
                var scope = new BoundScope(parent);
                foreach(var variable in previous.Variables)
                {
                    scope.TryDeclare(variable);
                }

                parent = scope;
            }
            return parent;
        }
        
        //Scoping
        private BoundStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            //Creating a new immutable array builder
            //So that we can return a BoundBlockStatement with an immutable array parameter
            var statements = ImmutableArray.CreateBuilder<BoundStatement>();

            var nextScope = new BoundScope(_scope);
            _scope = nextScope;

            //Adding each and every thing within the current syntax's statements 
            //Into the array before making it immutable
            foreach(var statementSyntax in syntax.Statements)
            {
                var statement = BindStatement(statementSyntax);
                statements.Add(statement);
            }
            _scope = _scope.Parent ?? _scope;
            return new BoundBlockStatement(statements.ToImmutable());
        }

        //Expressions
        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            var expression = BindExpression(syntax.Expression);
            return new BoundExpressionStatement(expression);
        }

        //Variable declaration
        private BoundStatement BindVariableDeclaration(VariableDeclarationSyntax syntax)
        {
            var name = syntax.Identifier.Text;
            var isReadOnly = syntax.KeywordToken.Kind == SyntaxKind.LetConstKeyword || syntax.KeywordToken.Kind == SyntaxKind.ConstKeyword;
            var initializer = BindExpression(syntax.Initializer);
            var variable = new VariableSymbol(name, isReadOnly, initializer.Type);
            
            if(!_scope.TryDeclare(variable))
            {
                _diagnostics.ReportVariableAlreadyDeclared(syntax.Identifier.Span, name);
            }

            return new BoundVariableDeclaration(variable, initializer);
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition, typeof(bool));
            var thenStatement = BindStatement(syntax.ThenStatement);
            var elseStatement = syntax.ElseClause is null ? null : BindStatement(syntax.ElseClause.ElseStatement);
            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }
        
        private BoundStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Expression, typeof(bool));
            var body = BindStatement(syntax.Body);
            return new BoundWhileStatement(condition, body);

        }

        //Value is being parsed into a nullable int
        //That then gets checked to see if it's null, and gets assigned to 0 if it is.
        private static BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax) => new BoundLiteralExpression(syntax.Value ?? 0);

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
        {
            var boundOperand = BindExpression(syntax.Operand);
            var boundOperatorKind = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);

            //Checking to see if the result of our BindUnaryOperatorKind call is null
            //And reporting it to the diagnostics
            //Then returning our boundOperand
            if (boundOperatorKind is null)
            {
                _diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text ?? "null", boundOperand.Type);
                return boundOperand;
            }
            return new BoundUnaryExpression(boundOperatorKind, boundOperand);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            var boundLeft = BindExpression(syntax.Left);
            var boundRight = BindExpression(syntax.Right);

            var boundOperatorKind = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);

            //Same as in the BindUnaryExpression but we return our boundLeft instead
            if (boundOperatorKind is null)
            {
                Console.WriteLine(syntax.OperatorToken.Text);
                _diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return boundLeft;
            }
            return new BoundBinaryExpression(boundLeft, boundOperatorKind, boundRight);
        }

        //Just to stay consistant tbh
        private BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax) => BindExpression(syntax.Expression);

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax)
        {
            var name = syntax.IdentifierToken.Text;

            //Trying to get the value, if it returns then great, if not we report it
            if (!_scope.TryLookup(name, out var variable))
            {
                _diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name ?? "name is null");
                return new BoundLiteralExpression(0);
            }
            return new BoundVariableExpression(variable);
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
        {
            var name = syntax.IdentifierToken.Text;
            var boundExpression = BindExpression(syntax.Expression);

            if(!_scope.TryLookup(name, out var variable))
            {
                //Null check on the name so that we can find the object
                _diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name);
                return boundExpression;
            }

            if(variable.IsReadOnly)
            {
                _diagnostics.ReportCannotAssign(syntax.IdentifierToken.Span, syntax.EqualsToken.Span, name);
            }

            //A variable cannot have their type reassigned.
            if(boundExpression.Type != variable.Type)
            {
                _diagnostics.ReportCannotConvert(syntax.Expression.Span, boundExpression.Type, variable.Type);
                return boundExpression;
            }
            return new BoundAssignmentExpression(variable, boundExpression);
        }
    }
}
