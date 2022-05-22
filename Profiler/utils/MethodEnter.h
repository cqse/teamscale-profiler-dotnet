#pragma once
#include "FunctionInfo.h"
#include <cor.h>
#include <corprof.h>
#include <windows.h>
#include <functional>
#include <utils/functionID_set/functionID_set.h>
#include <utils/atomic_queue/atomic_queue.h>


FunctionID constexpr NIL = static_cast<FunctionID>(-1);
using Queue = atomic_queue::AtomicQueueB<FunctionID, std::allocator<FunctionID>, NIL>;

/**
 * Sets the vector to be filled with methodIds from called methods at this time.
 */
void setCalledMethodsSet(functionID_set*);

void setCriticalSection(CRITICAL_SECTION*);

void setMethodIdQueue(Queue*);

/**
 * Sets the state of test case recording i.e. whether a test case is currently in progress or not.
 */
void setTestCaseRecording(bool);

/**
 * The callback function that is run on a method enter event.
 */
#ifdef _WIN64
EXTERN_C void FnEnterCallback(FunctionIDOrClientID);
#else
void FnEnterCallback(FunctionIDOrClientID);
#endif
