#include "CProfilerWorker.h"

CProfilerWorker::CProfilerWorker(Config* config, TraceLog* traceLog, std::unordered_set<FunctionID>* calledMethodIds, CRITICAL_SECTION* methodSetSynchronization) {
	this->traceLog = traceLog;
	this->calledMethodIds = calledMethodIds;
	this->methodSetSynchronization = methodSetSynchronization;
	setMethodIdVector(this->primary);
	this->workerThread = new std::thread(&CProfilerWorker::methodIdThreadLoop, this);
}

CProfilerWorker::~CProfilerWorker() {
	this->shutdown = true;
	if (this->workerThread->joinable()) {
		this->workerThread->join();
	}
	delete backing;
	delete primary;
}

void CProfilerWorker::methodIdThreadLoop() {
	while (!this->shutdown) {
		if (backing->empty() && primary->empty()) {
			std::this_thread::sleep_for(std::chrono::milliseconds(10));
			continue;
		}
		EnterCriticalSection(methodSetSynchronization);
		swapVectors();
		LeaveCriticalSection(methodSetSynchronization);
	}
}

void CProfilerWorker::prepareMethodIdSetForWriting() {
	swapVectors();
}

void CProfilerWorker::transferMethodIds(concurrency::concurrent_vector<UINT_PTR>* methodIds) {
	size_t size = methodIds->size();
	for (unsigned int i = 0; i < size; i++) {
		this->calledMethodIds->insert((*methodIds)[i]);
	}
	methodIds->clear();
	if (methodIds->capacity() > 2'000'000) {
		methodIds->shrink_to_fit();
	}
}

void CProfilerWorker::logError(std::string message) {
	std::string error = message;
	traceLog->info(error);
}

void CProfilerWorker::swapVectors() {
	// Must be called from synchronized context
	concurrency::concurrent_vector<FunctionID>* temp = backing;
	backing = primary;
	primary = temp;
	setMethodIdVector(this->primary);
	// Since the reassignment is technically not thread safe, we wait a little bit
	// to be sure that everyone is done with writing to the now backing vector.
	std::this_thread::sleep_for(std::chrono::milliseconds(5));
	transferMethodIds(this->backing);
}
