#include "CProfilerWorker.h"

CProfilerWorker::CProfilerWorker(Config* config, TraceLog* traceLog, functionID_set* calledMethodIds, CRITICAL_SECTION* methodSetSynchronization) {
	this->traceLog = traceLog;
	this->calledMethodIds = calledMethodIds;
	this->methodSetSynchronization = methodSetSynchronization;
	setCriticalSection(methodSetSynchronization);
	setCalledMethodsSet(calledMethodIds);
	setMethodIdQueue(&methodIdQueue);
	this->workerThread = new std::thread(&CProfilerWorker::methodIdThreadLoop, this);
}

CProfilerWorker::~CProfilerWorker() {
	this->shutdown = true;
	if (this->workerThread->joinable()) {
		this->workerThread->join();
	}
}

void CProfilerWorker::methodIdThreadLoop() {
	while (!this->shutdown) {
		if (methodIdQueue.was_empty()) {
			std::this_thread::sleep_for(std::chrono::milliseconds(10));
		}
		EnterCriticalSection(methodSetSynchronization);
		transferMethodIds();
		LeaveCriticalSection(methodSetSynchronization);
	}
}

void CProfilerWorker::transferMethodIds() {
	FunctionID i;
	while (methodIdQueue.try_pop(i)) {
		calledMethodIds->insert(i);
	}
}

void CProfilerWorker::logError(std::string message) {
	std::string error = message;
	traceLog->info(error);
}
