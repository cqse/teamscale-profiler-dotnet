#pragma once
#include "FunctionInfo.h"
#include <cor.h>
#include <corprof.h>
#include <windows.h>
#include <functional>
#include <utils/UIntSet/UIntSet.h>
#include <utils/atomic_queue/atomic_queue.h>

#include <iostream>
#include <fstream>

FunctionID constexpr NIL = static_cast<UINT64>(-1);
using Queue = atomic_queue::AtomicQueueB<UINT64, std::allocator<UINT64>, NIL>;

/**
 * Sets the vector to be filled with methodIds from called methods at this time.
 */
void setCalledMethodsSet(UIntSet*);

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
EXTERN_C void FnEnterCallback(UINT64);
#else
void FnEnterCallback(UINT64);
#endif
