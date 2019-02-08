#include "Debug.h"
#include "StackWalker.h"

class CustomStackWalker : public StackWalker {
public:
	std::string output;
	void OnOutput(LPCSTR szText) {
		output += szText;
		output += "\r\n";
	}
};

void Debug::log(std::string message)
{
	getInstance().logInternal(message);
}

Debug::Debug() {
	InitializeCriticalSection(&loggingSynchronization);
	logFile = CreateFile("C:\\Users\\Public\\profiler_debug.log", GENERIC_WRITE, FILE_SHARE_READ,
		NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
}

Debug::~Debug()
{
	if (logFile != INVALID_HANDLE_VALUE) {
		CloseHandle(logFile);
	}
	DeleteCriticalSection(&loggingSynchronization);
}

void Debug::logInternal(std::string message)
{
	if (logFile == INVALID_HANDLE_VALUE) {
		return;
	}
	EnterCriticalSection(&loggingSynchronization);
	message += "\r\n";
	WriteFile(logFile, message.c_str(), (DWORD)strlen(message.c_str()), NULL, NULL);
	LeaveCriticalSection(&loggingSynchronization);
}

void Debug::logStacktrace(std::string context) {
	log("Stacktrace: " + context);
	CustomStackWalker stackWalker;
	stackWalker.ShowCallstack();
	log(stackWalker.output);
}