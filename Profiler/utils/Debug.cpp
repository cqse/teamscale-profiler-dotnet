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

/**
 * Implements a thread-safe singleton pattern and ensures that the instance
 * is properly destroyed and thus the file handle freed.
 * See https://stackoverflow.com/a/1008289/1396068
 */
Debug& Debug::getInstance() {
	static Debug debug;
	return debug;
}

Debug::Debug() {
	InitializeCriticalSection(&loggingSynchronization);
	logFile = CreateFile("C:\\Users\\Public\\profiler_debug.log", GENERIC_WRITE, FILE_SHARE_READ,
		NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
}

void Debug::log(std::string message)
{
	if (logFile == INVALID_HANDLE_VALUE) {
		return;
	}

	message += "\r\n";

	EnterCriticalSection(&loggingSynchronization);
	DWORD dwWritten = 0;
	WriteFile(logFile, message.c_str(), static_cast<DWORD>(strlen(message.c_str())), &dwWritten, NULL);
	LeaveCriticalSection(&loggingSynchronization);
}

void Debug::logErrorWithStracktrace(std::string context) {
	log("Error in " + context + ". Stacktrace: ");
	CustomStackWalker stackWalker;
	stackWalker.ShowCallstack();
	log(stackWalker.output);
}

Debug::~Debug()
{
	if (logFile != INVALID_HANDLE_VALUE) {
		CloseHandle(logFile);
	}
	DeleteCriticalSection(&loggingSynchronization);
}
