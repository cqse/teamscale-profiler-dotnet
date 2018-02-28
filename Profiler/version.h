#pragma once

#define VERSION_STRING "1.0.0.0"
#define VERSION_ARRAY 1,0,0,0
#define VERSION_COPYRIGHT_YEAR "2018"
#define VERSION_COMPANY "CQSE GmbH"
#define VERSION_COPYRIGHT "Copyright (c) " VERSION_COPYRIGHT_YEAR " " VERSION_COMPANY
#define VERSION_PRODUCT "Cqse.Teamscale.Profiler.Dotnet"
#define VERSION_DESCRIPTION_PREFIX "Teamscale .NET Profiler v" VERSION_STRING

#ifdef _WIN64
#define VERSION_FILENAME "Profiler64.dll"
#define VERSION_DESCRIPTION VERSION_DESCRIPTION_PREFIX " (64bit)"
#else
#define VERSION_FILENAME "Profiler32.dll"
#define VERSION_DESCRIPTION VERSION_DESCRIPTION_PREFIX " (32bit)"
#endif
