Slicito - Visual Studio Extension for Analysis of C# Code
=========================================================

**Slice your code into edible pieces**

Understanding a complex codebase is time-consuming and difficult.
Slicito aims to be a toolbox of program analysis and visualization techniques which you can use to make this task much faster and simpler.
**It's currently in the research prototype stage, so only a subset of C# features is supported.**

Publications
------------

Although there's currently no web-based documentation, the most important ideas behind the tool are explained in the following publications:

- [Towards Efficient Software Comprehension with Code Analysis](docs/icsoft_dc_2022_paper.pdf) (ICSOFT 2022 doctoral consortium): The motivation behind the project.
- [Slicito: Using Computational Notebooks for Program Comprehension](https://ieeexplore.ieee.org/document/10174103) (ICPC 2023): The initial version of the tool which was originally based on computational notebooks and implemented as a VS Code extension (tag [`v0.1.0`](https://github.com/roberthusak/slicito/tree/v0.1.0)).
- [Combining Static Analysis Techniques for Program Comprehension Using Slicito](https://arxiv.org/pdf/2503.15675) (ICPC 2025 preprint): The current version of the tool which is a Visual Studio extension (tag [`v0.2.1`](https://github.com/roberthusak/slicito/tree/v0.2.1)).
- [Using Combination of Static Program Analysis Techniques for Program Comprehension of Large Systems](docs/doctoral_thesis.pdf) (doctoral thesis): The current version of the tool in more detail and its evaluation on a real-life project (tag [`v0.3.0`](https://github.com/roberthusak/slicito/tree/v0.3.0)).

Sample usage
------------

**Requirements:**

- Visual Studio 2022 (tested with the version 17.12.3)

**Steps:**

1. Download and install the latest VSIX file from the [releases page](https://github.com/roberthusak/slicito/releases) to your Visual Studio.

2. Clone or download this repository and open the sample solution `tests/inputs/AnalysisSamples/AnalysisSamples.sln` in Visual Studio.

3. In the main menu, select `Tools` > `Slicito` to open the Slicito tool window.
   It should appear at the same panel as the Solution Explorer.

4. In the Slicito tool window, click the button *Open* located right from the label *Script*.
   A new C# script file `slicito.csx` should be created in the solution directory with the content similar to:

```csharp
#r "c:\users\husak\appdata\local\microsoft\visualstudio\17.0_ae45ee82exp\extensions\robert husak\slicito\0.2\Slicito.Abstractions.dll"
#r "c:\users\husak\appdata\local\microsoft\visualstudio\17.0_ae45ee82exp\extensions\robert husak\slicito\0.2\Slicito.ProgramAnalysis.dll"
#r "c:\users\husak\appdata\local\microsoft\visualstudio\17.0_ae45ee82exp\extensions\robert husak\slicito\0.2\Slicito.Common.dll"
#r "c:\users\husak\appdata\local\microsoft\visualstudio\17.0_ae45ee82exp\extensions\robert husak\slicito\0.2\Slicito.DotNet.dll"

using Slicito.Common;
using Slicito.DotNet;
using Slicito.ProgramAnalysis;

using static Slicito.Common.Extensibility.ScriptContext;
```

5. This is a central "plumbing" code which loads the Slicito libraries and provides the context for the script.
   You can use the `SlicitoContext` property of the `ScriptContext` class to access the Slicito features via the fluent interface.
   Insert the following code for creating a call graph starting with the method `Caller` and displaying it:

```csharp
var slice = SlicitoContext.WholeSlice;

var method = await SlicitoContext.FindSingleMethodAsync(slice, "Caller");

var callGraph = await SlicitoContext.CreateCallGraphBuilder(slice)
    .AddCallerRoot(method)
    .BuildAsync();

return SlicitoContext.CreateCallGraphExplorer(callGraph);
```

6. Click the *Run* button in the Slicito tool window to execute the script.
   The call graph should be displayed in a newly opened tool window.

Current plans
-------------

- Add examples of using data-flow analysis and symbolic execution.
- Extend the set of supported C# features to handle real-life codebases.
- Evaluate on real program comprehension tasks in industry.

Contact me
----------

If you have any questions, ideas or concerns, [e-mail me](mailto:robert@husak.cloud), [create an issue](https://github.com/roberthusak/slicito/issues/new/choose) or [start a discussion](https://github.com/roberthusak/slicito/discussions/new).