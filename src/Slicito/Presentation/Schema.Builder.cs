using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Msagl.Drawing;

using Slicito.Abstractions;

namespace Slicito.Presentation;

public partial class Schema
{
    public class Builder
    {
        private Graph? _graph = new();

        private readonly List<IUriProvider> _uriProviders = new();
        private readonly List<ILabelProvider> _labelProviders = new();

        public Builder AddUriProvider(IUriProvider uriProvider)
        {
            _uriProviders.Add(uriProvider);

            return this;
        }

        public Builder AddLabelProvider(ILabelProvider labelProvider)
        {
            _labelProviders.Add(labelProvider);

            return this;
        }

        public Builder AddNode<TElement>(
            TElement element,
            TElement? containingElement = null,
            Action<TElement, Node>? nodeCustomizer = null)
            where TElement : class, IElement
        {
            EnsureValidState();

            if (_graph.SubgraphMap.TryGetValue(element.Id, out _))
            {
                return this;
            }

            var containingSubgraph = GetContainingSubgraph();

            var subgraph = new Subgraph(element.Id);

            var label = _labelProviders
                .Select(p => p.TryGetLabelForElement(element, containingElement))
                .FirstOrDefault(label => label is not null);
            if (label is not null)
            {
                subgraph.LabelText = label;
            }

            subgraph.Attr.Uri = _uriProviders
                .Select(p => p.TryGetUriForElement(element))
                .FirstOrDefault(uri => uri is not null)
                ?.ToString();

            containingSubgraph.AddSubgraph(subgraph);

            // FIXME Hack to force SubgraphMap refresh
            var retrievedNode = _graph.AddNode(subgraph.Id);
            Debug.Assert(ReferenceEquals(subgraph, retrievedNode));

            nodeCustomizer?.Invoke(element, subgraph);

            return this;

            Subgraph GetContainingSubgraph()
            {
                if (containingElement is null
                    || !_graph.SubgraphMap.TryGetValue(containingElement.Id, out var subgraph))
                {
                    return _graph.RootSubgraph;
                }

                return subgraph;
            }
        }

        public Builder AddNodes<TElement>(
            IEnumerable<TElement> elements,
            TElement? containingElement = null,
            Action<TElement, Node>? nodeCustomizer = null)
            where TElement : class, IElement
        {
            EnsureValidState();

            foreach (var element in elements)
            {
                AddNode(element, containingElement, nodeCustomizer);
            }

            return this;
        }

        public Builder AddNode<TElement, THierarchyData>(
            TElement element,
            IRelation<TElement, TElement, THierarchyData>? hierarchy,
            Action<TElement, Node>? nodeCustomizer = null)
            where TElement: class, IElement
        =>
            AddNode(
                element,
                hierarchy?.GetIncoming(element).FirstOrDefault()?.Source,
                nodeCustomizer);

        public Builder AddNodes<TElement, THierarchyData>(
            IEnumerable<TElement> elements,
            IRelation<TElement, TElement, THierarchyData>? hierarchy,
            Action<TElement, Node>? nodeCustomizer = null)
            where TElement: class, IElement
        {
            EnsureValidState();

            if (hierarchy is null)
            {
                return AddNodes(elements, null, nodeCustomizer);
            }

            var elementSet = elements.ToHashSet();

            var stack = new Stack<TElement>();

            var roots =
                elementSet.Where(e =>
                    !hierarchy.GetIncoming(e).Any(pair => elementSet.Contains(pair.Source)));

            foreach (var root in roots)
            {
                AddNode(root, null, nodeCustomizer);
                stack.Push(root);
            }

            while (stack.Count > 0)
            {
                var containingElement = stack.Pop();

                foreach (var pair in hierarchy.GetOutgoing(containingElement))
                {
                    var containedElement = pair.Target;
                    if (!elementSet.Contains(containedElement))
                    {
                        continue;
                    }

                    AddNode(containedElement, containingElement, nodeCustomizer);
                    stack.Push(containedElement);
                }
            }

            return this;
        }

        public Builder AddNodes<TElement, THierarchyData>(
            IRelation<TElement, TElement, THierarchyData> hierarchy,
            Action<TElement, Node>? nodeCustomizer = null)
            where TElement: class, IElement
        =>
            AddNodes(hierarchy.GetElements(), hierarchy, nodeCustomizer);


        public Builder AddEdge<TSourceElement, TTargetElement, TData>(
            IPair<TSourceElement, TTargetElement, TData> pair,
            Action<IPair<TSourceElement, TTargetElement, TData>, Edge>? edgeCustomizer = null)
            where TSourceElement : class, IElement
            where TTargetElement : class, IElement
        {
            EnsureValidState();

            if (!_graph.SubgraphMap.ContainsKey(pair.Source.Id)
                || !_graph.SubgraphMap.ContainsKey(pair.Target.Id))
            {
                return this;
            }

            var edge = _graph.AddEdge(pair.Source.Id, pair.Target.Id);

            edge.LabelText = _labelProviders
                .Select(p => p.TryGetLabelForPair(pair))
                .FirstOrDefault(label => label is not null);

            edge.Attr.Uri = _uriProviders
                .Select(p => p.TryGetUriForPair(pair))
                .FirstOrDefault(uri => uri is not null)
                ?.ToString();

            edgeCustomizer?.Invoke(pair, edge);

            return this;
        }

        public Builder AddEdge<TSourceElement, TTargetElement>(
            TSourceElement source,
            TTargetElement target,
            Action<IPair<TSourceElement, TTargetElement, EmptyStruct>, Edge>? edgeCustomizer = null)
            where TSourceElement : class, IElement
            where TTargetElement : class, IElement
        =>
            AddEdge(Pair.Create(source, target, new EmptyStruct()), edgeCustomizer);

        public Builder AddEdges<TSourceElement, TTargetElement, TData>(
            IEnumerable<IPair<TSourceElement, TTargetElement, TData>> pairs,
            Action<IPair<TSourceElement, TTargetElement, TData>, Edge>? edgeCustomizer = null)
            where TSourceElement : class, IElement
            where TTargetElement : class, IElement
        {
            EnsureValidState();

            foreach (var pair in pairs)
            {
                AddEdge(pair, edgeCustomizer);
            }

            return this;
        }

        public Builder AddEdges<TSourceElement, TTargetElement, TData>(
            IRelation<TSourceElement, TTargetElement, TData> relation,
            Action<IPair<TSourceElement, TTargetElement, TData>, Edge>? edgeCustomizer = null)
            where TSourceElement : class, IElement
            where TTargetElement : class, IElement
        =>
            AddEdges(relation.Pairs, edgeCustomizer);

        public Schema Build()
        {
            EnsureValidState();

            var graph = _graph;
            _graph = null;

            return new Schema(graph);
        }

        [MemberNotNull(nameof(_graph))]
        private void EnsureValidState()
        {
            if (_graph is null)
            {
                throw new InvalidOperationException($"It is impossible to modify the builder after it has already produced a schema.");
            }
        }
    }
}
