using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

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

public class DotNetMethod : DotNetTypeMember
{
    internal DotNetMethod(IMethodSymbol symbol, ControlFlowGraph? controlFlowGraph, string id) : base(symbol, id)
    {
        ControlFlowGraph = controlFlowGraph;
    }

    public new IMethodSymbol Symbol => (IMethodSymbol) base.Symbol;

    public ControlFlowGraph? ControlFlowGraph { get; }
}

public abstract class DotNetStorageTypeMember : DotNetTypeMember
{
    internal DotNetStorageTypeMember(ISymbol symbol, string id) : base(symbol, id)
    {
    }
}

public class DotNetProperty : DotNetStorageTypeMember
{
    internal DotNetProperty(IPropertySymbol symbol, string id) : base(symbol, id)
    {
    }

    public new IPropertySymbol Symbol => (IPropertySymbol) base.Symbol;
}

public class DotNetField : DotNetStorageTypeMember
{
    internal DotNetField(IFieldSymbol symbol, string id) : base(symbol, id)
    {
    }

    public new IFieldSymbol Symbol => (IFieldSymbol) base.Symbol;
}

public class DotNetOperation : DotNetElement
{
    internal DotNetOperation(IOperation operation, string id) : base(id)
    {
        Operation = operation;
    }

    public IOperation Operation { get; }
}
