#include "MethodEnter.h"
#include <iostream>
#include "Debug.h"

namespace {
	concurrency::concurrent_vector<FunctionID>* vectorInUse;
	bool isTestCaseRecording = false;
}

extern "C" void _stdcall EnterCpp(
	FunctionIDOrClientID funcId) {
	if (isTestCaseRecording) {
		vectorInUse->push_back(funcId.functionID);
	}
}

void setMethodIdVector(concurrency::concurrent_vector<FunctionID>& vectorToUse) {
	vectorInUse = &vectorToUse;
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
