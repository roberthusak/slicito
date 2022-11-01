using Slicito.Abstractions;

namespace Slicito.DotNet.Elements;

public class DotNetElement : IElement
{
    internal DotNetElement(string id)
    {
        Id = id;
    }

    public string Id { get; }
}

public class DotNetNamespace : DotNetElement
{
    internal DotNetNamespace(string id) : base(id)
    {
    }
}

public class DotNetType : DotNetElement
{
    internal DotNetType(string id) : base(id)
    {
    }
}

public class DotNetTypeMember : DotNetElement
{
    internal DotNetTypeMember(string id) : base(id)
    {
    }
}

public class DotNetMethod : DotNetTypeMember
{
    internal DotNetMethod(string id) : base(id)
    {
    }
}

public class DotNetStorageMember : DotNetTypeMember
{
    internal DotNetStorageMember(string id) : base(id)
    {
    }
}

public class DotNetProperty : DotNetStorageMember
{
    internal DotNetProperty(string id) : base(id)
    {
    }
}

public class DotNetField : DotNetStorageMember
{
    internal DotNetField(string id) : base(id)
    {
    }
}
