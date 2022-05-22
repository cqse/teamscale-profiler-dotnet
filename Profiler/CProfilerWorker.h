#pragma once
#include "config/Config.h"
#include <thread>
#include "log/TraceLog.h"
#include <chrono>
#include "utils/MethodEnter.h"
#include <utils/functionID_set/functionId_set.h>
#include <utils/atomic_queue/atomic_queue.h>

class CProfilerWorker
{
public:
	CProfilerWorker(Config*, TraceLog*, functionID_set*, CRITICAL_SECTION*);
	virtual ~CProfilerWorker();
private:
    
	// Variables
	std::thread* workerThread = NULL;
	TraceLog* traceLog = NULL;
	bool shutdown = false;

#pragma warning( disable : 4316)
	functionID_set* calledMethodIds;
	Queue* methodIdQueue = new Queue(65'536);

	CRITICAL_SECTION* methodSetSynchronization;

	// Methods
	void methodIdThreadLoop();
	void logError(std::string);
	void transferMethodIds();
};
