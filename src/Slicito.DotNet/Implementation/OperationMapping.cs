using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.DotNet.Implementation;

internal class OperationMapping
{
    private readonly Dictionary<ElementId, Operation> _idToOperation;
    private readonly Dictionary<ElementId, SyntaxNode> _idToSyntax;
    private readonly Dictionary<Operation, ElementId> _operationToId;

    private OperationMapping(
        Dictionary<ElementId, Operation> idToOperation,
        Dictionary<ElementId, SyntaxNode> idToSyntax,
        Dictionary<Operation, ElementId> operationToId)
    {
        _idToOperation = idToOperation;
        _idToSyntax = idToSyntax;
        _operationToId = operationToId;
    }

    public Operation GetOperation(ElementId id)
    {
        if (!_idToOperation.TryGetValue(id, out var operation))
        {
            throw new KeyNotFoundException($"No operation found for ElementId: {id.Value}");
        }
        return operation;
    }

    public SyntaxNode GetSyntax(ElementId id)
    {
        if (!_idToSyntax.TryGetValue(id, out var syntax))
        {
            throw new KeyNotFoundException($"No syntax node found for ElementId: {id.Value}");
        }
        return syntax;
    }

    public ElementId GetId(Operation operation)
    {
        if (!_operationToId.TryGetValue(operation, out var id))
        {
            throw new KeyNotFoundException("No ElementId found for the given operation");
        }
        return id;
    }

    public class Builder(string operationIdPrefix)
    {
        private int _currentId = 0;
        private bool _isBuilt = false;
        
        private readonly Dictionary<ElementId, Operation> _idToOperation = [];
        private readonly Dictionary<ElementId, SyntaxNode> _idToSyntax = [];
        private readonly Dictionary<Operation, ElementId> _operationToId = [];

        public Builder AddOperation(Operation operation, SyntaxNode syntax)
        {
            if (_isBuilt)
            {
                throw new InvalidOperationException("Builder has already been used to build an OperationMapping");
            }

            if (_operationToId.ContainsKey(operation))
            {
                throw new ArgumentException("Operation has already been added", nameof(operation));
            }

            var elementId = new ElementId($"{operationIdPrefix}{_currentId++}");
            
            _idToOperation[elementId] = operation;
            _idToSyntax[elementId] = syntax;
            _operationToId[operation] = elementId;

            return this;
        }

        public OperationMapping Build()
        {
            if (_isBuilt)
            {
                throw new InvalidOperationException("Builder has already been used to build an OperationMapping");
            }

            _isBuilt = true;

            return new OperationMapping(_idToOperation, _idToSyntax, _operationToId);
        }
    }
}
