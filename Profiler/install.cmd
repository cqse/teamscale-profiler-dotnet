@echo off
setlocal EnableDelayedExpansion

rem check for admin rights
net session >nul 2>&1
if %errorLevel% NEQ 0 (
	echo Failure: Execute script as Administrator.
	pause
	exit /B 1
)


set InstallSource=%CD%
for /f "delims=" %%x in (%InstallSource%\install.cfg) do (
	set line=%%x
	if "!line:~0,1!" neq "#" (
		set !line!
	)
)

if "%InstallDir%"=="" (
	echo Failure: InstallDir not set.
	pause
	exit /B 1
)

echo Installing Profiler to: "%InstallDir%"

if exist "%InstallDir%" (
	if "%InstallUploadService%"=="true" (
		echo Uninstalling Upload Service
		pushd "%InstallDir%\UploadDaemon\service"
		UploadDaemonService stop
		UploadDaemonService uninstall
		popd
	)
) else (
	mkdir "%InstallDir%"
)

xcopy "%InstallSource%" "%InstallDir%" /e /y /q

if exist "%PROGRAMFILES(X86)%" (
	copy "%InstallDir%\Profiler64.dll" "%SystemRoot%\System32\TeamscaleProfiler.dll" /y
	copy "%InstallDir%\Profiler32.dll" "%SystemRoot%\SysWOW64\TeamscaleProfiler.dll" /y
) else (
	copy "%InstallDir%\Profiler32.dll" "%SystemRoot%\System32\TeamscaleProfiler.dll" /y
)

setx /m COR_PROFILER {DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}

REM Enable the profiler globally (the actual profiling is disabled in the Profiler.yml)
setx /m COR_ENABLE_PROFILING 1
setx /m CORCLR_ENABLE_PROFILING 1

REM Path to profiler DLL, this points to the 64-Bit dll, located in System32. It will automatically redirect to SysWOW64 for 32-Bit applications.
setx /m COR_PROFILER_PATH "%SystemRoot%\System32\TeamscaleProfiler.dll"
REM Register profiling for .net core, we can use the install location here
setx /m CORECLR_PROFILER_PATH_32 "%InstallDir%\Profiler32.dll"
setx /m CORECLR_PROFILER_PATH_64 "%InstallDir%\Profiler64.dll"

REM Path to profiler config file (same for .net std fw and core)
setx /m COR_PROFILER_CONFIG "%InstallDir%\Profiler.yml"

if "%InstallUploadService%"=="1" (
	echo Installing Upload Service
	pushd "%InstallDir%\UploadDaemon\service"
	UploadDaemonService install
	UploadDaemonService start
	popd
)

echo.
echo ======================================
echo Success: Teamscale Profiler installed.
echo ======================================
echo.

pause