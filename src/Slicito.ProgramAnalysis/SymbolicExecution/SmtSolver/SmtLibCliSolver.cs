using System.Diagnostics;
using Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

public sealed class SmtLibCliSolver : ISolver
{
    private readonly Process _process;
    private readonly StreamWriter _input;
    private readonly StreamReader _output;
    private readonly Action<string>? _linePrinter;
    private readonly HashSet<string> _declaredFunctions = new();
    private bool _isDisposed;

    internal static async ValueTask<SmtLibCliSolver> CreateAsync(string pathToSolver, string[]? arguments, Action<string>? linePrinter)
    {
        var startInfo = new ProcessStartInfo(pathToSolver)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (arguments != null)
        {
            startInfo.Arguments = string.Join(" ", arguments);
        }

        var process = new Process { StartInfo = startInfo };
        process.Start();

        var input = process.StandardInput;
        var output = process.StandardOutput;

        // Configure solver options

        await SendCommandAsync(input, "(set-option :print-success true)", linePrinter);
        await ExpectSuccessAsync(output, linePrinter);

        await SendCommandAsync(input, "(set-logic ALL)", linePrinter); // Using ALL to support all features
        await ExpectSuccessAsync(output, linePrinter);

        return new SmtLibCliSolver(process, input, output, linePrinter);
    }

    private SmtLibCliSolver(Process process, StreamWriter input, StreamReader output, Action<string>? linePrinter)
    {
        _process = process;
        _input = input;
        _output = output;
        _linePrinter = linePrinter;
    }

    public async ValueTask AssertAsync(Term term)
    {
        ThrowIfDisposed();

        await DeclareUnknownFunctionsAsync(term);

        await SendCommandAsync($"(assert {SerializeTerm(term)})");
        await ExpectSuccessAsync();
    }

    public async ValueTask<SolverResult> CheckSatisfiabilityAsync(Func<IModel, ValueTask>? onSat = null)
    {
        ThrowIfDisposed();
        
        await SendCommandAsync("(check-sat)");

        var result = await _output.ReadLineAsync();

        _linePrinter?.Invoke(result);
        
        switch (result?.Trim())
        {
            case "sat":
                if (onSat != null)
                {
                    using var model = new SmtLibModel(this);
                    await onSat(model);
                }
                return SolverResult.Satisfiable;
                
            case "unsat":
                return SolverResult.Unsatisfiable;

            case "unknown":
                return SolverResult.Unknown;
                
            default:
                throw new InvalidOperationException($"Unexpected solver result: {result}");
        }
    }

    internal async ValueTask<Term> EvaluateAsync(Term term)
    {
        await SendCommandAsync($"(get-value ({SerializeTerm(term)}))");

        var response = await _output.ReadLineAsync();

        _linePrinter?.Invoke(response);

        return ParseValue(response ?? throw new InvalidOperationException("No response from solver"));
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        
        try
        {
            const string exitCommand = "(exit)";

            _input.WriteLine(exitCommand);

            _linePrinter?.Invoke(exitCommand);

            _input.Dispose();
            _output.Dispose();
            _process.Dispose();
        }
        finally
        {
            _isDisposed = true;
        }
    }

    private async ValueTask DeclareUnknownFunctionsAsync(Term term)
    {
        if (term is Term.FunctionApplication app)
        {
            var functionName = app.function.Name;

            if (!app.function.IsBuiltIn && _declaredFunctions.Add(functionName))
            {
                var declaration = app.function switch
                {
                    Function.Nullary f => $"(declare-const {functionName} {SerializeSort(f.ResultSort)})",
                    Function.Unary f => $"(declare-fun {functionName} ({SerializeSort(f.ArgumentSort)}) {SerializeSort(f.ResultSort)})",
                    Function.Binary f => $"(declare-fun {functionName} ({SerializeSort(f.ArgumentSort1)} {SerializeSort(f.ArgumentSort2)}) {SerializeSort(f.ResultSort)})",
                    Function.Ternary f => $"(declare-fun {functionName} ({SerializeSort(f.ArgumentSort1)} {SerializeSort(f.ArgumentSort2)} {SerializeSort(f.ArgumentSort3)}) {SerializeSort(f.ResultSort)})",
                    _ => throw new ArgumentException($"Unsupported function type: {app.function.GetType()}")
                };
                
                await SendCommandAsync(declaration);
                await ExpectSuccessAsync();
            }

            foreach (var arg in app.Arguments)
            {
                await DeclareUnknownFunctionsAsync(arg);
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(SmtLibCliSolver));
        }
    }

    private static string SerializeSort(Sort sort) => sort switch
    {
        Sort.Bool => "Bool",
        Sort.BitVec bv => $"(_ BitVec {bv.Width})",
        _ => throw new ArgumentException($"Unsupported sort: {sort.GetType()}")
    };

    private static string SerializeTerm(Term term) => term switch
    {
        Term.Constant.Bool b => b.Value.ToString().ToLowerInvariant(),
        Term.Constant.BitVec bv => $"(_ bv{bv.Value} {bv.BitVecSort.Width})",
        Term.FunctionApplication app when app.function is Function.Nullary => app.function.Name,
        Term.FunctionApplication app => $"({app.function.Name} {string.Join(" ", app.Arguments.Select(SerializeTerm))})",
        _ => throw new ArgumentException($"Unsupported term: {term.GetType()}")
    };

    private static Term ParseValue(string response)
    {
        // Basic parsing of SMT-LIB response like ((x true))
        var value = response.Trim('(', ')').Split(' ')[1];
        
        if (bool.TryParse(value, out var boolValue))
        {
            return new Term.Constant.Bool(boolValue);
        }
        
        if (value.StartsWith("(_ bv"))
        {
            var parts = value.Trim('(', ')').Split(' ');
            if (parts.Length == 3 && ulong.TryParse(parts[1].Substring(2), out var bitVecValue) && int.TryParse(parts[2], out var width))
            {
                return new Term.Constant.BitVec(bitVecValue, new Sort.BitVec(width));
            }
        }
        else if (value.StartsWith("#b"))
        {
            var binaryStr = value.Substring(2);
            var width = binaryStr.Length;
            var bitVecValue = Convert.ToUInt64(binaryStr, 2);
            return new Term.Constant.BitVec(bitVecValue, new Sort.BitVec(width));
        }
        else if (value.StartsWith("#x"))
        {
            var hexStr = value.Substring(2);
            var width = hexStr.Length * 4;
            var bitVecValue = Convert.ToUInt64(hexStr, 16);
            return new Term.Constant.BitVec(bitVecValue, new Sort.BitVec(width));
        }

        throw new InvalidOperationException($"Failed to parse SMT-LIB value: {value}");
    }

    private static async Task SendCommandAsync(StreamWriter input, string command, Action<string>? linePrinter)
    {
        await input.WriteLineAsync(command);
        await input.FlushAsync();

        linePrinter?.Invoke(command);
    }

    private async Task SendCommandAsync(string command) => await SendCommandAsync(_input, command, _linePrinter);

    private static async Task ExpectSuccessAsync(StreamReader output, Action<string>? linePrinter)
    {
        var response = await output.ReadLineAsync();

        linePrinter?.Invoke(response);

        if (response != "success")
        {
            throw new InvalidOperationException($"Expected 'success', got: {response}");
        }
    }
    
    private async Task ExpectSuccessAsync() => await ExpectSuccessAsync(_output, _linePrinter);

    private sealed class SmtLibModel : IModel
    {
        private readonly SmtLibCliSolver _solver;
        private bool _isDisposed;

        public SmtLibModel(SmtLibCliSolver solver)
        {
            _solver = solver;
        }

        public async ValueTask<Term> EvaluateAsync(Term term)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SmtLibModel));
            }

            return await _solver.EvaluateAsync(term);
        }

        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}
