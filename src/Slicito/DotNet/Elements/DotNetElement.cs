using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet.Elements;

public abstract class DotNetElement : IElement
{
    internal DotNetElement(string id)
    {
        Id = id;
    }

    public string Id { get; }
}

public class DotNetProject : DotNetElement
{
    internal DotNetProject(Project project, Compilation compilation, string id) : base(id)
    {
        Project = project;
        Compilation = compilation;
    }

    public Project Project { get; }
    public Compilation Compilation { get; }
}

public abstract class DotNetSymbolElement : DotNetElement
{
    internal DotNetSymbolElement(ISymbol symbol, string id) : base(id)
    {
        Symbol = symbol;
    }

    public ISymbol Symbol { get; }
}

public class DotNetNamespace : DotNetSymbolElement
{
    internal DotNetNamespace(INamespaceSymbol symbol, string id) : base(symbol, id)
    {
    }

    public new INamespaceSymbol Symbol => (INamespaceSymbol) base.Symbol;
}

public class DotNetType : DotNetSymbolElement
{
    internal DotNetType(ITypeSymbol symbol, string id) : base(symbol, id)
    {
    }

    public new ITypeSymbol Symbol => (ITypeSymbol) base.Symbol;
}

public abstract class DotNetTypeMember : DotNetSymbolElement
{
    internal DotNetTypeMember(ISymbol symbol, string id) : base(symbol, id)
    {
    }
}

public class DotNetMethod : DotNetSymbolElement
{
    internal DotNetMethod(IMethodSymbol symbol, string id) : base(symbol, id)
    {
    }

    public new IMethodSymbol Symbol => (IMethodSymbol) base.Symbol;
}

public abstract class DotNetStorageMember : DotNetSymbolElement
{
    internal DotNetStorageMember(ISymbol symbol, string id) : base(symbol, id)
    {
    }
}

public class DotNetProperty : DotNetSymbolElement
{
    internal DotNetProperty(IPropertySymbol symbol, string id) : base(symbol, id)
    {
    }

    public new IPropertySymbol Symbol => (IPropertySymbol) base.Symbol;
}

public class DotNetField : DotNetSymbolElement
{
    internal DotNetField(IFieldSymbol symbol, string id) : base(symbol, id)
    {
    }

    public new IFieldSymbol Symbol => (IFieldSymbol) base.Symbol;
}
