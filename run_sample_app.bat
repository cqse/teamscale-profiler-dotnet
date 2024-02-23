REM Example batch-file to profile a 64-bit-application.
REM For full documentation check out
REM https://github.com/cqse/teamscale-profiler-dotnet/blob/master/documentation/userguide.md

pushd %~dp0
set script_dir=%CD%
popd

REM Register the profiler (do not change this).
set COR_PROFILER={DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}
set CORCLR_PROFILER={DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}

REM Enable the profiler, set to 0 to start the application without profiling.
set COR_ENABLE_PROFILING=1
set CORCLR_ENABLE_PROFILING=1

REM Path to profiler DLL, use Profiler32.dll for 32-bit applications (do not use quotation marks).
set COR_PROFILER_PATH=%script_dir%\Profiler\bin\Debug\Profiler64.dll
set CORCLR_PROFILER_PATH_32=%script_dir%\Profiler\bin\Debug\Profiler32.dll
set CORCLR_PROFILER_PATH_64=%script_dir%\Profiler\bin\Debug\Profiler64.dll

REM Path where to store the traces (do not use quotation marks).
set COR_PROFILER_TARGETDIR=%script_dir%
set CORCLR_PROFILER_TARGETDIR=%script_dir%

REM If you use ngen.exe to build the application, disable this line.
set COR_PROFILER_LIGHT_MODE=1
set CORCLR_PROFILER_LIGHT_MODE=1

REM log variables
set COR

REM Start the application.
cd .\test-data\test-programs
.\PdfizerConsole.exe