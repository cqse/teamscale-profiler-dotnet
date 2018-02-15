Teamscale Ephemeral .NET Profiler
[![Build status](https://ci.appveyor.com/api/projects/status/mhdeqjyg3u3osjm6/branch/master?svg=true)](https://ci.appveyor.com/project/mpdeimos/teamscale-profiler-dotnet/branch/master)
===================================

Lightweight profiler for .NET applications that provides method-level coverage information to support [Test Gap Analysis](https://www.cqse.eu/en/consulting/software-test-control/) in Teamscale.

## Documentation

Documentation on installation and usage of the profiler can be found in the [GitHub Wiki](https://github.com/cqse/teamscale-profiler-dotnet/wiki).

## Downloads

The latest (and previous) releases can be downloaded from the [GitHib Release](https://github.com/cqse/teamscale-profiler-dotnet/releases) website.

## Contributing

The coverage profiler can be compiled using Visual Studio 2017. Ensure to have the Windows and .NET SDK installed.

Unit tests exist in the form of .NET NUnit tests. These are included in the same solution as the profiler. Ensure to build both flavors of the Profiler (`Win32` and `x64`) before running the tests.

Automatic build and testing is performed with an Appveyour build definition.

## Release Process

The release process is outomated with GitHub Releases and Appveyor:

1. Go to GitHub Releases and draft a new Release
2. Enter the Tag name and Release name. It is good practice to give both the same name. We use `YY.MM.(minor)` version scheme, e.g. `v18.2.0`.
3. Enter a short description of changes
4. Publish the release

Appveyour will then take care of attaching the release binary.
