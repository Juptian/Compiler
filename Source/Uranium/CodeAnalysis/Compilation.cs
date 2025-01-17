﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Uranium.CodeAnalysis.Syntax;
using Uranium.CodeAnalysis.Binding;
using Uranium.Logging;
using Uranium.CodeAnalysis.Text;
using Uranium.CodeAnalysis.Binding.Statements;
using Uranium.CodeAnalysis.Lowering;
using Uranium.CodeAnalysis.Symbols;

namespace Uranium.CodeAnalysis
{
    public sealed class Compilation
    {
        private BoundGlobalScope? _globalScope;
        public Compilation(SyntaxTree syntax)
            : this(null, syntax)
        {
        }

        private Compilation(Compilation? previous, SyntaxTree syntax)
        {
            Previous = previous;
            Syntax = syntax;
        }

        public SyntaxTree Syntax { get; }
        public Compilation? Previous { get; }

        internal BoundGlobalScope GlobalScope { 
            get
            {
                if(_globalScope is null)
                {
                    //We make a new global scope by default, because initially it will always be null
                    var scope = Binder.BindGlobalScope(Previous?.GlobalScope, Syntax.Root);
                    //Only allowing _globalScope to be assigned to scope when it's null
                    Interlocked.CompareExchange(ref _globalScope, scope, null);
                }
                return _globalScope;
            } 
        }

        public Compilation ContinueWith(SyntaxTree syntaxTree)
        {
            return new(this, syntaxTree);
        }

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object?> variables)
        {
            var globalScope = GlobalScope;
            var diagnostics = Syntax.Diagnostics.Concat(globalScope.Diagnostics).ToImmutableArray();

            if(diagnostics.Any())
            {
                return new(diagnostics, null);
            }

            var statement = GetStatement();

            var evaluator = new Evaluator(statement, variables);
            var value = evaluator.Evaluate();
            return new(ImmutableArray<Diagnostic>.Empty, value);
        }

        public void EmitTree(TextWriter writer)
        {
            var statement = GetStatement();
            statement.WriteTo(writer);
        }

        private BoundBlockStatement GetStatement()
        {
            var result = GlobalScope.Statement;
            return Lowerer.Lower(result);
        }
    }
}
