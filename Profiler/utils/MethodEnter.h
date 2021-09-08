#pragma once
#include "FunctionInfo.h"
#include <cor.h>
#include <corprof.h>
#include <windows.h>
#include <functional>
#include <concurrent_vector.h>

void setMethodIdVector(concurrency::concurrent_vector<FunctionID>&);
void setTestCaseRecording(bool);
void setProfilingEnabled(bool);

#ifdef _WIN64

EXTERN_C void FnEnterCallback(FunctionIDOrClientID);

#else

void FnEnterCallback(FunctionIDOrClientID);

#endif
