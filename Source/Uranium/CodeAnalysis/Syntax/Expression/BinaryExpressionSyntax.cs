﻿using Uranium.CodeAnalysis.Syntax.Expression;
using System.Collections.Generic;

namespace Uranium.CodeAnalysis.Syntax.Expression
{
    //Basically just the BoundBinaryExpression but it's syntax instead
    internal sealed class BinaryExpressionSyntax : ExpressionSyntax
    {
        public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
        {
            Left = left;
            OperatorToken = operatorToken;
            Right = right;
        }

        public ExpressionSyntax Left { get; }
        public SyntaxToken OperatorToken { get; }
        public ExpressionSyntax Right { get; }

        public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

    }
}
