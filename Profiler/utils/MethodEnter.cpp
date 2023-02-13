#include "MethodEnter.h"
#include <iostream>
#include "Debug.h"

namespace {
	functionID_set* calledFunctionSet;
	bool isTestCaseRecording = false;
	CRITICAL_SECTION* methodSetSynchronization;
	Queue* methodQueue;
}

extern "C" void _stdcall EnterCpp(FunctionIDOrClientID funcId) {
	if (isTestCaseRecording && !calledFunctionSet->contains(funcId.functionID)) {
		methodQueue->push(funcId.functionID);
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

void __fastcall FnEnterCallback(FunctionIDOrClientID funcId) {
	EnterCpp(funcId);
}

#else

void __declspec(naked) FnEnterCallback(FunctionIDOrClientID funcId) {
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
