_Work in progress._ The [paper from the doctoral consortium at ICSOFT 2022](docs/icsoft_dc_2022_paper.pdf) explains the motivation behind the project.

Slicito
=======

**Slice your code into edible pieces**

Understanding a complex codebase is time-consuming and difficult.
Slicito aims to be a toolbox which you can use to make this task much faster and simpler.
Currently, it serves only for simple visualisations of project structure, but more features are on their way.

Sample usage
------------

Compile the solution:

```
dotnet build Slicito.sln
```

Install the extension [Polyglot Notebooks](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode) to VS Code and open the notebook [`samples/notebooks/NamespaceHierarchy.ipynb`](samples/notebooks/NamespaceHierarchy.ipynb).
When you execute its first cell, an image similar to this should appear:

![Sample hierarchy of Slicito itself](docs/hierarchy_sample.svg)

It shows the namespaces of Slicito itself, and allows you to navigate through its structure.
If you have an instance of Microsoft Visual Studio opened, the elements you click in the schema will be shown to you there.
Replace the reference to `Slicito.csproj` with your C# project to inspect the structure of your code.
You can also extract and visualize dependencies as shown in the second cell of the [sample notebook](samples/notebooks/NamespaceHierarchy.ipynb).

Plans
-----

* Make turning code into graphs as straightforward as possible.
* Document all the provided abstraction and visualization techniques.
* Use advanced techniques from [AskTheCode](https://github.com/roberthusak/AskTheCode) to reason precisely about program behaviour.

Disclaimer
----------

There has been no release of the tool yet.
Therefore, the public interface is currently under rapid development, and as such, it can change without warning.

Contact me
----------

If you have any questions, ideas or concerns, [e-mail me](mailto:robert@husak.cloud), [create an issue](https://github.com/roberthusak/slicito/issues/new/choose) or [start a discussion](https://github.com/roberthusak/slicito/discussions/new).