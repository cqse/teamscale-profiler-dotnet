#include "MethodEnter.h"
#include <iostream>
#include "Debug.h"

namespace {
	UIntSet* calledFunctionSet;
	bool isTestCaseRecording = false;
	CRITICAL_SECTION* methodSetSynchronization;
	Queue* methodQueue;
}

extern "C" void _stdcall EnterCpp(UINT64 covId) {
	if (isTestCaseRecording && !calledFunctionSet->contains(covId)) {
		methodQueue->push(covId);
	}
}

void setCalledMethodsSet(UIntSet* setToUse) {
	calledFunctionSet = setToUse;
}

void setCriticalSection(CRITICAL_SECTION* methodSetSync) {
	methodSetSynchronization = methodSetSync;
}

void setMethodIdQueue(Queue* methodIdQueue) {
	methodQueue = methodIdQueue;
}

void setTestCaseRecording(bool testCaseRecording) {
	isTestCaseRecording = testCaseRecording;
}

#ifdef _WIN64

void __fastcall FnEnterCallback(UINT64 covId) {
	EnterCpp(covId);
}

#else

void __declspec(naked) FnEnterCallback(UINT64 covId) {
	__asm {
		PUSH EAX
		PUSH ECX
		PUSH EDX
		PUSH[ESP + 16]
		CALL EnterCpp
		POP EDX
		POP ECX
		POP EAX
		RET 4
	}
}

#endif