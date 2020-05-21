# NewOrbit.PackageTemplate

- Source assemblies are stored under `/src`
- Test assemblies are stored under `/tests`
- Assemblies should reference code analysis packages:
  - `Microsoft.CodeQuality.Analyzers`
  - `StyleCop.Analyzers`
  - And include a reference to the `NewOrbit.Package.ruleset` file
  - A sample project is included in this repo setup accordingly
- Package should be built and published on ADO
  - A sample `azure-pipelines.yml` is included
  - The sample project is set up for versioning through ADO

## Using this template

- Click the `Use this template` button at the top of this repo
- Enter the name for your repository, prefixed with `NewOrbit.`
- Clone your new repository
- Open the folder in vs code and do a global find/replace for `PackageTemplate` with your new package name
- Rename the project and solution files and folders also
- Delete this section of the readme and you're done

> Readme template follows:
-----------------

## Installation

```cmd
dotnet add NewOrbit.PackageTemplate
```

## Usage

*Example usage and code snippets here.*
