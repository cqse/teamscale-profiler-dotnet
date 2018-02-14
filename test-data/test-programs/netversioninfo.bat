@echo off
REM Credits, Code und Infos auf http://www.dzaebel.net/NetVersions.htm

if "%1"=="" goto Start
if "%1"=="/Single" goto Single
if "%1"=="/TempVersion" goto TempVersion
echo wrong Option %1
exit

:Start
echo. 
echo.Installed .NET Framework-Versions :
echo.
for /D %%i in (%windir%\Microsoft.NET\Framework\v?"."*) DO call %0 /Single %%i

set info=Hotfix for Microsoft .NET Framework 3.0 - KB958483 - Dez 2008
set asm=%ProgramFiles%\Reference Assemblies\Microsoft\Framework\v3.0\PresentationFramework.dll
if exist "%asm%" call "%OutExe%" "%asm%" "%info%" "3.0.6920.1500"

set info=Hotfix for Microsoft .NET Framework 3.5 - KB958484 - Dez 2008
set asm=%ProgramFiles%\Reference Assemblies\Microsoft\Framework\v3.5\system.data.services.client.dll
if exist "%asm%" call "%OutExe%" "%asm%" "%info%" "3.5.30729.196"

set info=New Data.Services for .NET Framework 3.5 SP1 - KB976127 - Jan 2010
set asm=%ProgramFiles%\Reference Assemblies\Microsoft\Framework\v3.5\system.data.services.client.dll
if exist "%asm%" call "%OutExe%" "%asm%" "%info%" "3.5.30729.5004"

set info=New Data.Services for .NET Framework 3.5 SP1 - KB976127 - Jan 2010
set asm=%ProgramFiles%\Reference Assemblies\Microsoft\Framework\v3.5\system.data.services.client.dll
if exist "%asm%" call "%OutExe%" "%asm%" "%info%" "3.5.30729.4466"

echo.
Goto Finish

:Single
set asm=mscorwks.dll
  if exist "%2\%asm%" call %0 /TempVersion %2 "%asm%"
set asm=Windows Communication Foundation\Infocard.exe
  if exist "%2\%asm%" call %0 /TempVersion %2 "%asm%"
set asm=Microsoft.Build.Tasks.v3.5.dll
  if exist "%2\%asm%" call %0 /TempVersion %2 "%asm%"
set asm=Microsoft.Build.Tasks.v4.0.dll
  if exist "%2\%asm%" call %0 /TempVersion %2 "%asm%"
Goto Finish

:TempVersion
set CsFile=%TEMP%\VersionTmp.cs
set OutExe=%TEMP%\VersionTmp.exe
if exist "%OutExe%" call "%OutExe%" "%2\%~3" && goto Finish

echo. using System; using System.Diagnostics;                           >"%CsFile%"
echo. class Class1                                                     >>"%CsFile%"
echo. { static void Main(string[] args){                               >>"%CsFile%"
echo.   string pfad = Environment.ExpandEnvironmentVariables(args[0]); >>"%CsFile%"
echo.   FileVersionInfo v=FileVersionInfo.GetVersionInfo(pfad);        >>"%CsFile%"
echo.   if (args.Length == 3){string info = args[1];                   >>"%CsFile%"
echo.     string vCheck = args[2];                                     >>"%CsFile%"
echo.     if (v.ProductVersion != vCheck) return;                      >>"%CsFile%"
echo.     Console.WriteLine(info);                                     >>"%CsFile%"
echo.   } Console.WriteLine(v.ToString());                             >>"%CsFile%"
echo. }}                                                               >>"%CsFile%"
"%2\csc.exe" /nologo /target:exe /out:"%OutExe%" "%CsFile%" 
"%OutExe%" "%2\%~3"

:Finish