_Work in progress_

Slicito
=======

**Slice your code into edible pieces**

Understanding a complex codebase is time-consuming and difficult.
Slicito aims to be a toolbox which you can use to make this task much faster and simpler.
Currently, it serves only for simple visualisations of project structure, but more features are on their way.

Sample usage
------------

Compile the _Debug_ version of _Slicito.csproj_:

```
dotnet build src/Slicito/Slicito.csproj
```

Install the extension [.NET Interactive Notebooks](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode) to VS Code and open the notebook [`samples/notebooks/NamespaceHierarchy.ipynb`](samples/notebooks/NamespaceHierarchy.ipynb).
When you execute its first and only cell, an image similar to this should appear:

![Sample hierarchy of Slicito itself](docs/hierarchy_sample.svg)

It shows the classes of Slicito itself, and (for now) it's pretty simple.
Replace the reference to `Slicito.csproj` with your C# project to see its structure.
Next steps:

* Use the power of [Microsoft Roslyn](https://github.com/dotnet/roslyn) to analyse your C# projects. Extract interesting parts of the code and their relations.
* Create customised and well-arranged graphs using [Microsoft Automated Graph Layout (MSAGL)](https://github.com/microsoft/automatic-graph-layout).

Plans
-----

* Enable turning code into graphs as straightforward as possible.
* Make graphs interactive. E.g., by clicking on a node, the related code element should open in the IDE.
* Add advanced techniques to analyse dependencies between different parts of the code.
* Use advanced techniques from [AskTheCode](https://github.com/roberthusak/AskTheCode) to reason precisely about program behaviour.

Disclaimer
----------

There has been no release of the tool yet.
Therefore, the public interface is currently under rapid development, and as such, it can change without warning.

Contact me
----------

If you have any questions, ideas or concerns, [e-mail me](mailto:robert@husak.cloud), [create an issue](https://github.com/roberthusak/slicito/issues/new/choose) or [start a discussion](https://github.com/roberthusak/slicito/discussions/new).