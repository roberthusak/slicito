using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.DotNet.Implementation;

internal record FlowGraphGroup(IFlowGraph RootFlowGraph, ImmutableDictionary<ElementId, IFlowGraph> ElementIdToNestedFlowGraph);
