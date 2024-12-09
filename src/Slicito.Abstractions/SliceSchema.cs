using System.Collections.Immutable;

namespace Slicito.Abstractions;

public record SliceSchema(
    ImmutableArray<ElementType> ElementTypes,
    ImmutableDictionary<LinkType, ImmutableArray<LinkElementTypes>> LinkTypes,
    ImmutableDictionary<ElementType, ImmutableArray<string>> ElementAttributes,
    ImmutableArray<ElementType> RootElementTypes,
    LinkType? HierarchyLinkType);

public record struct LinkElementTypes(ElementType SourceType, ElementType TargetType);
