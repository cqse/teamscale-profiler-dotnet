#pragma once
#include "FunctionInfo.h"
#include <cor.h>
#include <corprof.h>
#include <windows.h>
#include <functional>
#include <concurrent_vector.h>

/**
 * Sets the vector to be filled with methodIds from called methods at this time.
 */
void setMethodIdVector(concurrency::concurrent_vector<FunctionID>*);

/**
 * Sets the state of test case recording i.e. whether a test case is currently in progress or not.
 */
void setTestCaseRecording(bool);

/**
 * Sets whether profiling is enabled or not. If not, not methodIds will be recorded.
 */
void setProfilingEnabled(bool);

/**
 * The callback function that is run on a method enter event.
 */
#ifdef _WIN64
EXTERN_C void FnEnterCallback(FunctionIDOrClientID);
#else
void FnEnterCallback(FunctionIDOrClientID);
#endif
