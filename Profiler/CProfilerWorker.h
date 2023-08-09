#pragma once
#include "config/Config.h"
#include <thread>
#include "log/TraceLog.h"
#include <chrono>
#include "utils/MethodEnter.h"
#include <utils/UIntSet/UIntSet.h>
#include <utils/atomic_queue/atomic_queue.h>

class CProfilerWorker
{
public:
	CProfilerWorker(Config*, TraceLog*, UIntSet*, CRITICAL_SECTION*);
	virtual ~CProfilerWorker();
	void transferMethodIds();
private:

	// Variables
	std::thread* workerThread = NULL;
	TraceLog* traceLog = NULL;
	bool shutdown = false;

	// Warning from the use of alignas in the Atomic Queue.
#pragma warning( disable : 4316)
	UIntSet* calledMethodIds;
	Queue methodIdQueue = Queue(65'536);

	CRITICAL_SECTION* methodSetSynchronization;

	// Methods
	void methodIdThreadLoop();
	void logError(std::string);
};
