These are the sources of the profiler for .NET framework 4.0 and up.
They can be compiled with Visual Studio 2013 for 32bit and 64bit (target: "Release").

The "light" variant uses less CPU but does not force rejitting, i.e. pre-jitted methods will not be picked up(target: "Release Light").

# Profiling .NET framework 2.0 - 3.5 applications

For these, you need to use the old profiler binary, since the interface has changed drastically.
The current profiler code CANNOT be used to profile these applications!

[Info about changes to the profiling API in .NET 4.0][1]


    [1]: https://msdn.microsoft.com/en-us/library/vstudio/dd778910%28v=vs.100%29.aspx