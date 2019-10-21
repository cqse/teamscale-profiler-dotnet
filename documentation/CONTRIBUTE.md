# Debugging (WinDbg)

The following script allows to debug the profiler with `windbg` on Windows 10 with the Windows SDK installed:

    SET COR_ENABLE_PROFILING=1
    SET COR_PROFILER={DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}
    SET COR_PROFILER_PATH=E:\Profiler\bin\Debug\Profiler64.dll

    "C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\windbg.exe" "E:\test-data\test-programs\GeneratedTest.exe"

When `windbg` starts up, hit 'Insert and remove breakpoints' (`F9`) and add a breakpoint to

    bu Profiler64!CProfilerCallback::Initialize

Then hit 'Go' (`F9`).
The program will stop at the invocation of our initialization and open a source view showing this location.
From this point you can debug with step over/into/out as usual.

### Notes

* `windbg` normally finds the source files automagically.
  If it doesn't, setting `File > Source File Path...` probably helps.
* You may [set more specific breakpoints][breakpoints].
* Credits to David Broman and his blog post [explaining how to debug profilers using `windbg`][debugProfilers].

  [debugProfilers]: https://blogs.msdn.microsoft.com/davbr/2007/12/11/debugging-your-profiler-i-activation/
  [breakpoints]: https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/breakpoint-syntax
