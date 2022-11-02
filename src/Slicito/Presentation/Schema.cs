using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Html;
using Microsoft.Msagl.Drawing;

using Slicito.Abstractions;
using Slicito.Abstractions.Relations;

namespace Slicito.Presentation;

public class Schema : IHtmlContent
{
    private readonly byte[] _contents;

    private Schema(byte[] contents)
    {
        _contents = contents;
    }

    public Stream GetContents() => new MemoryStream(_contents);

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        var contentsString = Encoding.UTF8.GetString(_contents);
        writer.Write(contentsString);
    }

    public Task<string> UploadToServerAsync(string name = "schema") =>
        ServerUtils.UploadFileAsync(name + ".svg", GetContents());

    public class Builder
    {
        private readonly Graph _graph = new();

        private readonly List<IUriProvider> _uriProviders = new();

        public Builder AddUriProvider(IUriProvider uriProvider)
        {
            _uriProviders.Add(uriProvider);

            return this;
        }

        public Builder AddNode<TElement>(
            TElement element,
            TElement? containingElement = null,
            Action<TElement, Node>? nodeCustomizer = null)
            where TElement : class, IElement
        {
            if (_graph.SubgraphMap.TryGetValue(element.Id, out _))
            {
                return this;
            }

            var containingSubgraph = GetContainingSubgraph();

            var subgraph = new Subgraph(element.Id);

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
            foreach (var element in elements)
            {
                AddNode(element, containingElement, nodeCustomizer);
            }

            return this;
        }

        public Builder AddNode<TElement, THierarchyData>(
            TElement element,
            IBinaryRelation<TElement, TElement, THierarchyData>? hierarchy,
            Action<TElement, Node>? nodeCustomizer = null)
            where TElement: class, IElement
        =>
            AddNode(
                element,
                hierarchy?.GetIncoming(element).FirstOrDefault()?.Source,
                nodeCustomizer);

        public Builder AddNodes<TElement, THierarchyData>(
            IEnumerable<TElement> elements,
            IBinaryRelation<TElement, TElement, THierarchyData>? hierarchy,
            Action<TElement, Node>? nodeCustomizer = null)
            where TElement: class, IElement
        {
            if (hierarchy is null)
            {
                return AddNodes(elements, null, nodeCustomizer);
            }

            var elementSet = elements.ToHashSet();

            var stack = new Stack<TElement>();

            foreach (var root in elementSet.Where(e => !hierarchy.GetIncoming(e).Any()))
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

        public Builder AddEdge<TSourceElement, TTargetElement, TData>(
            IPair<TSourceElement, TTargetElement, TData> pair,
            Action<IPair<TSourceElement, TTargetElement, TData>, Edge>? edgeCustomizer = null)
            where TSourceElement : class, IElement
            where TTargetElement : class, IElement
        {
            if (!_graph.SubgraphMap.ContainsKey(pair.Source.Id)
                || !_graph.SubgraphMap.ContainsKey(pair.Target.Id))
            {
                return this;
            }

            var edge = _graph.AddEdge(pair.Source.Id, pair.Target.Id);

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
            foreach (var pair in pairs)
            {
                AddEdge(pair, edgeCustomizer);
            }

            return this;
        }

        public Builder AddEdges<TSourceElement, TTargetElement, TData>(
            IBinaryRelation<TSourceElement, TTargetElement, TData> relation,
            Action<IPair<TSourceElement, TTargetElement, TData>, Edge>? edgeCustomizer = null)
            where TSourceElement : class, IElement
            where TTargetElement : class, IElement
        =>
            AddEdges(relation.Pairs, edgeCustomizer);

        public Schema BuildSvg(LayoutOrientation orientation = LayoutOrientation.Vertical)
        {
            var ms = _graph.RenderSvgToStream(orientation);

            return new Schema(ms.ToArray());
        }
    }
}
