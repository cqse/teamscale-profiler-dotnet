These are the sources of the profiler for .NET framework 4.0 and up.

# Compiling

The code can be compiled with Visual Studio 2013 for 32bit and 64bit (target: "Release").

The "light" variant uses less CPU but does not force re-jitting, i.e. pre-jitted methods (produced e.g. by ngen.exe) will not be picked up (target: "Release Light").

# Profiling .NET framework 2.0 - 3.5 applications

For these old applications, you need to use the old, no longer updated profiler binary under `resources/profiler-dotnet-v2`, since the interface has changed drastically.
The current profiler code CANNOT be used to profile these old applications!

[Info about changes to the profiling API in .NET 4.0][1]

# Tests

There is some code and precompiled binaries under test-data, but these were all produced with .NET framework 2.0 and are undocumented. Thus, they cannot be used to test the current profiler version.


    [1]: https://msdn.microsoft.com/en-us/library/vstudio/dd778910%28v=vs.100%29.aspx