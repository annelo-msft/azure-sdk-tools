// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Azure.ClientSdk.Analyzers
{
    public abstract class ClientAnalyzerBase : SymbolAnalyzerBase
    {
        protected const string ClientSuffix = "Client";

        public override SymbolKind[] SymbolKinds { get; } = new []{ SymbolKind.NamedType};

        public override void Analyze(ISymbolAnalysisContext context)
        {
            var typeSymbol = (INamedTypeSymbol)context.Symbol;
            if (typeSymbol.TypeKind != TypeKind.Class || !typeSymbol.Name.EndsWith(ClientSuffix) || typeSymbol.DeclaredAccessibility != Accessibility.Public)
            {
                return;
            }

            AnalyzeCore(context);
        }

        protected class ParameterEquivalenceComparer: IEqualityComparer<IParameterSymbol>
        {
            public static ParameterEquivalenceComparer Default { get; } = new ParameterEquivalenceComparer();

            public bool Equals(IParameterSymbol x, IParameterSymbol y)
            {
                return x.Type.Equals(y.Type) && x.Name.Equals(y.Name);
            }

            public int GetHashCode(IParameterSymbol obj)
            {
                return obj.Type.GetHashCode() ^ obj.Name.GetHashCode();
            }
        }

        protected IMethodSymbol FindMethod(IEnumerable<IMethodSymbol> methodSymbols, ImmutableArray<IParameterSymbol> parameters)
        {
            return methodSymbols.SingleOrDefault(symbol => parameters.SequenceEqual(symbol.Parameters, ParameterEquivalenceComparer.Default));
        }

        protected IMethodSymbol FindMethod(IEnumerable<IMethodSymbol> methodSymbols, ImmutableArray<IParameterSymbol> parameters, Func<IParameterSymbol, bool> lastParameter)
        {

            return methodSymbols.SingleOrDefault(symbol => {

                if (!symbol.Parameters.Any())
                {
                    return false;
                }

                var allButLast = symbol.Parameters.RemoveAt(symbol.Parameters.Length - 1);

                return allButLast.SequenceEqual(parameters, ParameterEquivalenceComparer.Default) && lastParameter(symbol.Parameters.Last());
            });
        }

        public abstract void AnalyzeCore(ISymbolAnalysisContext context);
    }
}