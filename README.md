Teamscale Ephemeral .NET Profiler
[![Build status](https://ci.appveyor.com/api/projects/status/pamrby3g6dm26074/branch/master?svg=true)](https://ci.appveyor.com/project/cqse/teamscale-profiler-dotnet/branch/master)
===================================

Lightweight profiler for .NET applications that provides method-level coverage information to support [Test Gap Analysis](https://www.cqse.eu/en/consulting/software-test-control/) in Teamscale.

## Download

The latest (and previous) releases can be downloaded from the [GitHib Release](https://github.com/cqse/teamscale-profiler-dotnet/releases) website.

## Documentation

Documentation on installation and usage of the profiler can be found in the [GitHub Wiki](https://github.com/cqse/teamscale-profiler-dotnet/wiki).

## Contributing

The coverage profiler can be compiled using Visual Studio 2017. Ensure to have the Windows and .NET SDK installed.

Unit tests exist in the form of .NET NUnit tests. These are included in the same solution as the profiler. Ensure to build both flavors of the Profiler (`Win32` and `x64`) before running the tests.

Automatic build and testing is performed with an AppVeyor build definition.

## Release Process

The release process is outomated with GitHub Releases and Appveyor:

We use `YY.MM.revison` version scheme, e.g. `v18.2.0`.

1. Edit `.appveyor.yml` and adust the `version` property.
2. Go to GitHub Releases and draft a new Release
3. Enter the Tag name and Release name. It is good practice to give both the same name, e.g. `v18.2.0`.
4. Enter a short description of changes
5. Publish the release

AppVeyor will then take care of attaching the release binary.
