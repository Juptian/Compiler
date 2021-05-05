﻿using System;
using System.Linq;
using Uranium.CodeAnalysis.Syntax;

namespace Uranium.CodeAnalysis.Lexing.LexerSupport
{
    internal static class LexNumbers
    {
        public static void ReadNum(Lexer lexer)
        {
            var hasSeparator = false;
            var isDecimal = false;
            var hasMultiDecimals = false;
            lexer.Start = lexer.Index;

            while (char.IsDigit(lexer.CurrentIndex) ||
                  (lexer.CurrentIndex == '_' || lexer.CurrentIndex == ' ') && char.IsDigit(lexer.NextIndex) ||
                  (lexer.CurrentIndex == '.' || lexer.CurrentIndex == ',') && char.IsDigit(lexer.NextIndex))
            {
                if (!hasSeparator && (lexer.CurrentIndex == '_' || lexer.CurrentIndex == ' '))
                {
                    hasSeparator = true;
                }

                if (lexer.CurrentIndex == '.' || lexer.CurrentIndex == ',')
                {
                    hasMultiDecimals = isDecimal;
                    isDecimal = true;
                }
                lexer.Index++;
            }

            var length = lexer.Index - lexer.Start;

            //Replacing , with . here so that I can parse it into a number
            //This allows a user to chose between , and . as their decimal separator
            var charArray = lexer.Source.ToString(lexer.Start, length).Replace(',', '.').ToCharArray();

            var text = string.Join("", charArray.Where(e => !char.IsWhiteSpace(e) && !e.Equals('_')));
            lexer.Text = text;

            //Numbers cannot have multiple .s or ,s.
            if (hasMultiDecimals)
            {
                lexer.diagnostics.ReportInvalidNumber(new(lexer.Start, length), text, typeof(double));
            }

            //var targetType = SyntaxFacts.GetKeywordType(lexer.PreviousIdentifier?.Kind ?? SyntaxKind.DoubleKeyword);

            if(lexer.PreviousIdentifier is not null && 
               !SyntaxFacts.IsVarKeyword(lexer.PreviousIdentifier!.Kind))
            {
                if(SyntaxFacts.IsFloatingPoint(lexer.PreviousIdentifier!.Kind))
                {
                    ParseDouble(text, length, lexer);
                }
                else
                {
                    if(isDecimal)
                    {
                        lexer.diagnostics.ReportInvalidDecimal(new(lexer.Start, length), text, lexer.PreviousIdentifier.Kind);
                    }
                    else
                    {
                        ParseLong(text, length, lexer);
                    }
                }
            }
            if(isDecimal)
            {
                ParseDouble(text, length, lexer);
            } 
            else
            {
                ParseLong(text, length, lexer);
            }
            lexer.Index--;
        }
   
        private static void ParseDouble(string text, int length, Lexer lexer)
        {
            if (!double.TryParse(text, out double value))
            {
                lexer.diagnostics.ReportInvalidNumber(new(lexer.Start, length), text, typeof(double));
            }
            else if (lexer.PreviousIdentifier is not null && !SyntaxFacts.IsVarKeyword(lexer.PreviousIdentifier.Kind))
            { 
                if(lexer.PreviousIdentifier!.Kind is SyntaxKind.DoubleKeyword)
                {
                    lexer.CurrentValue = value;
                }
                else
                {
                    lexer.CurrentValue = (float)value;
                }
            }
            else
            {
                if (value >= float.MinValue && value <= float.MaxValue)
                {
                    lexer.CurrentValue = (float)value;
                }
                else
                {
                    lexer.CurrentValue = value;
                }
            }
        }

        private static void ParseLong(string text, int length, Lexer lexer)
        {
            if (!ulong.TryParse(text, out ulong value))
            {
                lexer.diagnostics.ReportInvalidNumber(new(lexer.Start, length), text, typeof(long));
            }
            else if(lexer.PreviousIdentifier is not null && !SyntaxFacts.IsVarKeyword(lexer.PreviousIdentifier.Kind))
            {
                if(lexer.PreviousIdentifier!.Kind is SyntaxKind.IntKeyword)
                {
                    lexer.CurrentValue = (int)value;
                } 
                else
                {
                    lexer.CurrentValue = (long)value;
                }
            }
            else
            {
                if(value <= int.MaxValue)
                {
                    lexer.CurrentValue = (int)value;
                }
                else if(value <= uint.MaxValue && value >= uint.MinValue)
                {
                    lexer.CurrentValue = (uint)value;
                }
                else if(value <= long.MaxValue)
                {
                    lexer.CurrentValue = (long)value;
                }
                else
                {
                    lexer.CurrentValue = value;
                }
            }
        }
    }
}
