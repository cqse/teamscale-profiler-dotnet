#pragma once
#include "config/Config.h"
#include <thread>
#include "log/TraceLog.h"
#include <chrono>
#include "utils/MethodEnter.h"
#include <concurrent_vector.h>
#include <unordered_set>

class CProfilerWorker
{
public:
	CProfilerWorker(Config*, TraceLog*, std::unordered_set<FunctionID>*, CRITICAL_SECTION*);
	virtual ~CProfilerWorker();

	/**
	 * Prepares the set of methodIds from called methods for writing, i.e. transfers 
	 * the content of the vectors that have been used for intermittent storage into the set.
	 */
	void prepareMethodIdSetForWriting();
private:
	// Variables
	std::thread* workerThread = NULL;
	TraceLog* traceLog = NULL;
	bool shutdown = false;
	concurrency::concurrent_vector<FunctionID> vector1;
	concurrency::concurrent_vector<FunctionID> vector2;
	std::unordered_set<FunctionID>* calledMethodIds;

	CRITICAL_SECTION* methodSetSynchronization;


	// Methods
	void methodIdThreadLoop();
	void logError(std::string);
	void transferMethodIds(concurrency::concurrent_vector<FunctionID>&);

};

