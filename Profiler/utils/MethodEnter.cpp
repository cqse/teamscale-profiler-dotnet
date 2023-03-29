#include "MethodEnter.h"
#include <iostream>
#include "Debug.h"

namespace {
	functionID_set* calledFunctionSet;
	bool isTestCaseRecording = false;
	CRITICAL_SECTION* methodSetSynchronization;
	Queue* methodQueue;
}

extern "C" void _stdcall EnterCpp(FunctionID funcId) {
	if (isTestCaseRecording && !calledFunctionSet->contains(funcId)) {
		methodQueue->push(funcId);
	}
}

void setCalledMethodsSet(functionID_set* setToUse) {
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

void __fastcall FnEnterCallback(FunctionID funcId) {
	EnterCpp(funcId);
}

#else

void __declspec(naked) FnEnterCallback(FunctionID funcId) {
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
