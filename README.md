Teamscale .NET Profiler
[![Build status](https://github.com/cqse/teamscale-profiler-dotnet/actions/workflows/actions.yml/badge.svg)](https://github.com/cqse/teamscale-profiler-dotnet/actions/workflows/actions.yml)
===================================

Lightweight profiler for .NET applications that provides method-level coverage information to support [Test Gap Analysis](https://teamscale.com/test-gap-analysis) in Teamscale.

## System Requirements

.NET Framework 4.5 or newer must be installed.

## Download

The latest (and previous) releases can be downloaded from the [GitHub Release](https://github.com/cqse/teamscale-profiler-dotnet/releases) website.

## Documentation

Documentation on installation and usage of the profiler can be found [here](./documentation/userguide.md).

## Contributing

The coverage profiler can be compiled using Visual Studio 2017. Ensure to have the Windows and .NET SDK installed.
Please also install [CodeMaid][codemaid] to enable formatting the source code automatically on save. This keeps the
code consistently formatted for everyone.

Unit tests exist in the form of .NET NUnit tests. These are included in the same solution as the profiler.
During development, always build the `Win32` variant as this is configured to also build the x64 variant as well.
This way, the integration tests are run correctly against both variants.

Automatic build and testing is performed in a GitHub action.

For debugging tipps, please have a look [here](./documentation/userguide.md).

### Linking against System Libraries

We use `#pragma comment(lib, "LIBRARYNAME.lib")` to specify link-time dependencies directly in the source file
that needs the dependency. This way, the dependencies are versioned more explicitly with the code and it's immediately
clear which parts of the code need the linked library.

So please don't add any linked libraries to the solution configuration unless absolutely necessary.

## Release Process

The release process is automated with GitHub Releases and Appveyor:

We use `YY.MM.revison` version scheme, e.g. `v18.2.0`.

1. Edit `CHANGELOG.md` and create a new release section with all changes in the release.
2. Commit the changes to master directly.
3. Go to [GitHub Releases](https://github.com/cqse/teamscale-profiler-dotnet/releases) and draft a new release.
4. Enter the tag name and release name. Give both the same name, e.g. `v18.2.0`.
5. Copy-paste the `CHANGELOG.md` content of this release into the description.
6. Publish the release.
7. THe GitHub action will then take care of attaching the release binary. Ensure this is done properly.

[codemaid]: http://www.codemaid.net/

