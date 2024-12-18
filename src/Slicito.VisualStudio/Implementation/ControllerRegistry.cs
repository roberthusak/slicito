using System.Collections.Concurrent;
using System.Threading;

using Slicito.Abstractions;

namespace Slicito.VisualStudio.Implementation;

internal class ControllerRegistry
{
    private readonly ConcurrentDictionary<int, IController> _controllers = new();

    private int _nextId = 0;

    public int Register(IController controller)
    {
        var id = Interlocked.Increment(ref _nextId);
        _controllers[id] = controller;
        return id;
    }

    public bool TryGet(int id, out IController controller) => _controllers.TryGetValue(id, out controller);
}
