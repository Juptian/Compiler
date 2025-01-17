﻿using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Uranium.CodeAnalysis.Syntax;

namespace Uranium.Tests.CodeAnalysis.Lexing
{
    public class LexerTests
    {
        //This is horrendous, but I couldn't figure out how to make it any better
        private static readonly IEnumerable<(SyntaxKind kind, string text)> _testCases;
        private static readonly IEnumerable<(TupleContainer left, TupleContainer right)> _pairTestCases = GetTokenPairs();

        //Constructor because
        // private static IEnumerable<(SyntaxKind kind, string text)> _testCases = GetOperatorTokens().Concat(GetSyntacticSymbols()).Concat(GetNumbers()).Concat(GetKeywords());
        // Is awful
        static LexerTests()
        {
            //Made a method so that it looks cleaner
            _testCases = Concatenate(GetSoloOperators(), GetCompoundOperators(), GetSyntacticSymbols(), GetNumbers(), GetKeywords());
        }
       
        [Theory]
        [MemberData(nameof(GetTokensData))] //When GetTokensData returns something, it'll be parsed into here
        public void LexerLexesToken(SyntaxKind kind, string text)
        {
            var tokens = SyntaxTree.LexTokens(text);

            var singleToken = Assert.Single(tokens);
            Assert.NotEmpty(singleToken.ToString());
            Assert.Equal(kind, singleToken.Kind);
            Assert.Equal(text, singleToken.Text);
        }
        
        [Theory]
        [InlineData("     ")]
        public void LexerLexesWhitespace(string text)
        {
            var tokens = SyntaxTree.LexTokens(text);
            Assert.Empty(tokens);
        }

        [Theory]
        [InlineData("\r\r")]
        public void LexerLexesLineBreak(string text)
        {
            var tokens = SyntaxTree.LexTokens(text);
            var token = Assert.Single(tokens);
            Assert.Equal(SyntaxKind.LineBreak, token.Kind);
        }

        [Theory]
        [InlineData("/* */", true)]
        [InlineData("//")]
        public void LexerLexesComments(string text, bool isMultiLine = false)
        {
            var tokens = SyntaxTree.LexTokens(text);
            var token = Assert.Single(tokens);
            Assert.Equal( isMultiLine ? SyntaxKind.MultiLineComment : SyntaxKind.SingleLineComment, token.Kind);
        }
        [Theory]
        [InlineData("    f")]
        public void LexerIgnoresWhitespace(string text)
        {
            var tokens = SyntaxTree.LexTokens(text);
            var token = Assert.Single(tokens);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind);
        }
        
        [Theory]
        [InlineData("100 233", typeof(int))]
        [InlineData("4294967299", typeof(long))]
        [InlineData("10.01", typeof(float))]
        [InlineData("10.1234567890", typeof(double))]
        public void LexerLexesNumbersProperly(string text, Type expectedType)
        {
            var tokens = SyntaxTree.LexTokens(text);
            var token = Assert.Single(tokens);
            Assert.Equal(expectedType, token.Value.GetType());
        }

        [Fact]
        public void LexerLexesSingleLineStringCorrectly()
        {
            var text = "\"abc123\"";
            var tokens = SyntaxTree.LexTokens(text);
            var token = Assert.Single(tokens);
            Assert.Equal("abc123", token!.Value);
        }
        [Fact]
        public void LexerLexesMultiLineStringCorreclty()
        {
            var textA = "#\"";
            var textB = @"abc
123";
            var toTest = string.Concat(textA, textB, "\"");

            var tokens = SyntaxTree.LexTokens(toTest);
            var token = Assert.Single(tokens);

            Assert.Equal(@"abc
123", token!.Value);
        }

        [Fact]
        public void LexerLexesLiteralInterpolatedStringCorrectly()
        {
            var textA = "#$\"";
            var textB = @"abcdef
ghijk";
            var toTest = string.Concat(textA, textB, "\"");
            var tokens = SyntaxTree.LexTokens(toTest);
            var token = Assert.Single(tokens);
            Assert.Equal(@"abcdef
ghijk", token.Value!);
        }

        [Fact]
        public void LexerLexesCharCorrectly()
        {
            var a = "'a'";
            var b = "''";

            var tokensA = SyntaxTree.LexTokens(a);
            var tokenA = Assert.Single(tokensA);
            var tokensB = SyntaxTree.LexTokens(b);
            var tokenB = Assert.Single(tokensB);

            Assert.Equal('a', tokenA.Value!);
            Assert.Equal(string.Empty, tokenB.Value!);

        }

        [Theory]
        [InlineData("\"abc\\n oneTwoThree\"", "abc\\n oneTwoThree")]
        [InlineData("\"abc\\r\\n oneTwoThree\"", "abc\\r\\n oneTwoThree")]
        [InlineData("\"abc\\\" oneTwoThree\"", "abc\\\" oneTwoThree")]
        [InlineData("'\\n'", "\\n")]
        public void LexerLexesEscapeCharactersCorrectly(string text, string expected)
        {
            var tokens = SyntaxTree.LexTokens(text);
            var token = Assert.Single(tokens);
            Assert.Equal(expected, token!.Value);
        }

        [Fact]
        public void LexerLexesEndOfFileCorrectly()
        {
            string text = "      \0";
            var tokens = SyntaxTree.LexTokens(text);
            Assert.Empty(tokens);
        }

        [Theory]
        [MemberData(nameof(GetTokenPairsData))]
        public void LexerLexesTokenPairs(TupleContainer left, TupleContainer right)
        {
            var leftText = left.Text;
            var rightText = right.Text;

            var leftKind = left.Kind;
            var rightKind = right.Kind;

            var text = leftText + rightText;
            var tokens = SyntaxTree.LexTokens(text).ToArray();
            
            Assert.Equal(2, tokens.Length);

            Assert.Equal(leftKind, tokens[0].Kind);
            Assert.Equal(rightKind, tokens[1].Kind);

            Assert.Equal(leftText, tokens[0].Text);
            Assert.Equal(rightText, tokens[1].Text);
        }


        public static IEnumerable<object[]> GetTokensData()
        {
            var arr = _testCases.ToArray();
            for(int i = 0; i < arr.Length; i++)
            {
                yield return new object[] { arr[i].kind, arr[i].text };
            }
        }

        public static IEnumerable<object[]> GetTokenPairsData()
        {
            var arr = _pairTestCases.ToArray();
            for(int i = 0; i < arr.Length; i++)
            {
                yield return new object[] { arr[i].left, arr[i].right };
            }
        }

        private static IEnumerable<(SyntaxKind kind, string text)> GetSoloOperators()
        {
            return new[]
            {
                (SyntaxKind.Equals, "="),
                (SyntaxKind.Plus, "+"),
                (SyntaxKind.Minus, "-"),
                (SyntaxKind.Divide, "/"),
                (SyntaxKind.Multiply, "*"),
                (SyntaxKind.Pow, "**"),
                (SyntaxKind.Percent, "%"),
                (SyntaxKind.Ampersand, "&"),
                (SyntaxKind.Pipe, "|"),
                (SyntaxKind.Hat, "^"),
                (SyntaxKind.GreaterThan, ">"), 
                (SyntaxKind.LesserThan, "<"),
                (SyntaxKind.Bang, "!"), 
            };
        }

        private static IEnumerable<(SyntaxKind kind, string text)> GetCompoundOperators()
        {
            return new[]
            {
                (SyntaxKind.DoubleEquals, "=="),
                (SyntaxKind.PlusPlus, "++"),
                (SyntaxKind.PlusEquals, "+="),
                (SyntaxKind.MinusMinus, "--"),
                (SyntaxKind.MinusEquals, "-="),
                (SyntaxKind.DivideEquals, "/="),
                (SyntaxKind.MultiplyEquals, "*="),
                (SyntaxKind.PowEquals, "**="),
                (SyntaxKind.PercentEquals, "%="),
                (SyntaxKind.DoubleAmpersand, "&&"),
                (SyntaxKind.DoublePipe, "||"),
                (SyntaxKind.HatEquals, "^="), 
                (SyntaxKind.GreaterThanEquals, ">="),
                (SyntaxKind.LesserThanEquals, "<="),
                (SyntaxKind.BangEquals, "!="),

            };
        }

        private static IEnumerable<(SyntaxKind kind, string text)> GetSyntacticSymbols()
        {
            return new[]
            {
                (SyntaxKind.Semicolon, ";"),
                (SyntaxKind.Colon, ":"),
                (SyntaxKind.Dot, "."),
                (SyntaxKind.Comma, ","),
                (SyntaxKind.Tilde, "~"),
                (SyntaxKind.OpenParenthesis, "("),
                (SyntaxKind.CloseParenthesis, ")"),
                (SyntaxKind.OpenCurlyBrace, "{"),
                (SyntaxKind.CloseCurlyBrace, "}"),
                (SyntaxKind.OpenBrackets, "["),
                (SyntaxKind.CloseBrackets, "]"),
                
            };
        }

        private static IEnumerable<(SyntaxKind kind, string text)> GetNumbers()
        {
            return new[]
            {
                (SyntaxKind.NumberToken, "123.4"),
                (SyntaxKind.NumberToken, "1"),
                (SyntaxKind.NumberToken, "10000"),
                (SyntaxKind.NumberToken, "10.04"),
                (SyntaxKind.NumberToken, "10")
            };
        }

        private static IEnumerable<(SyntaxKind kind, string text)> GetKeywords()
        {
            return new[]
            {
                (SyntaxKind.TrueKeyword, "true"),
                (SyntaxKind.FalseKeyword, "false"),
                (SyntaxKind.DoubleKeyword, "double"),
                (SyntaxKind.CharKeyword, "char"),
                (SyntaxKind.StringKeyword, "string"),
                (SyntaxKind.FloatKeyword, "float"),
                (SyntaxKind.IntKeyword, "int"),
                (SyntaxKind.LongKeyword, "long"),
                (SyntaxKind.BoolKeyword, "bool"),
                (SyntaxKind.VarKeyword, "var"),
                (SyntaxKind.StructKeyword, "struct"),
                (SyntaxKind.ClassKeyword, "class"),
                (SyntaxKind.NamespaceKeyword, "namespace"),
                (SyntaxKind.EnumKeywrod, "enum"),
                (SyntaxKind.TypeDefKeyword, "typedef"),
                (SyntaxKind.Null, "null"),
            };
        }

        //Bottom of the file because it does not need to be revisited very often
        private static bool RequiresSeparator(SyntaxKind leftKind, SyntaxKind rightKind)
        {
            var tokensThatRequire = Concatenate(GetSoloOperators(), GetKeywords(), GetNumbers()).ToArray();
            for(int i = 0; i < tokensThatRequire.Length; i++)
            {
                if(tokensThatRequire[i].kind == leftKind || tokensThatRequire[i].kind == rightKind)
                {
                    return true;
                }
            }
            return false;
        }

        //^^
        private static IEnumerable<(TupleContainer left, TupleContainer Right)> GetTokenPairs()
        {
            var cases = _testCases.ToArray();
            for(int i = 0; i < cases.Length; i++)
            {
                for(int x = 0; x < cases.Length; x++)
                {
                    if(!RequiresSeparator(cases[i].kind, cases[x].kind))
                    {
                        var left = new TupleContainer(cases[i].kind, cases[i].text);
                        var right = new TupleContainer(cases[x].kind, cases[x].text);
                        yield return new(left, right);
                    }
                }
            }
        }
       
        //^^
        private static IEnumerable<T> Concatenate<T>(params IEnumerable<T>[] list)
        {
            for(int i = 0; i < list.Length; i++)
            {
                var arr = list[i].ToArray();
                for(int x = 0; x < arr.Length; x++)
                {
                    yield return arr[x];
                }
            }
        }

    }
}
