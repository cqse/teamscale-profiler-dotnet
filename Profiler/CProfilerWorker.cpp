#include "CProfilerWorker.h"


CProfilerWorker::CProfilerWorker(Config* config, TraceLog* traceLog, std::unordered_set<FunctionID>* calledMethodIds, CRITICAL_SECTION* methodSetSynchronization) {
	this->traceLog = traceLog;
	this->calledMethodIds = calledMethodIds;
	this->methodSetSynchronization = methodSetSynchronization;
	setMethodIdVector(this->vector1);
	this->workerThread = new std::thread(&CProfilerWorker::methodIdThreadLoop, this);
}

CProfilerWorker::~CProfilerWorker() {
	this->shutdown = true;
	if (this->workerThread->joinable()) {
		this->workerThread->join();
	}
}

void CProfilerWorker::methodIdThreadLoop() {
	// Toggle which vector to use
	bool vectorToggle = true;
	while (!this->shutdown) {
		if (vector1.empty() && vector2.empty()) {
			std::this_thread::sleep_for(std::chrono::milliseconds(10));
			continue;
		}
		if (vectorToggle) {
			EnterCriticalSection(methodSetSynchronization);
			setMethodIdVector(this->vector2);
			std::this_thread::sleep_for(std::chrono::milliseconds(5));
			transferMethodIds(this->vector1);
			LeaveCriticalSection(methodSetSynchronization);
		}
		else {
			EnterCriticalSection(methodSetSynchronization);
			setMethodIdVector(this->vector1);
			std::this_thread::sleep_for(std::chrono::milliseconds(5));
			transferMethodIds(this->vector2);
			LeaveCriticalSection(methodSetSynchronization);
		}
		vectorToggle = !vectorToggle;
	}
}

void CProfilerWorker::prepareMethodIdSetForWriting() {
	transferMethodIds(this->vector1);
	transferMethodIds(this->vector2);
}

void CProfilerWorker::transferMethodIds(concurrency::concurrent_vector<UINT_PTR>& methodIds) {
	size_t size = methodIds.size();
	for (unsigned int i = 0; i < size; i++) {
		this->calledMethodIds->insert(methodIds[i]);
	}
	methodIds.clear();
	if (methodIds.capacity() > 2'000'000) {
		methodIds.shrink_to_fit();
	}
}

void CProfilerWorker::logError(std::string message) {
	std::string error = message;
	traceLog->info(error);
}