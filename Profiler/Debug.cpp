#include "Debug.h"
#include "StackWalker.h"
#include <DbgHelp.h>

typedef BOOL(WINAPI *MINIDUMPWRITEDUMP)(HANDLE hProcess, DWORD dwPid, HANDLE hFile, MINIDUMP_TYPE DumpType,
	CONST PMINIDUMP_EXCEPTION_INFORMATION ExceptionParam,
	CONST PMINIDUMP_USER_STREAM_INFORMATION UserStreamParam,
	CONST PMINIDUMP_CALLBACK_INFORMATION CallbackParam
	);

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

Debug::~Debug()
{
	if (logFile == INVALID_HANDLE_VALUE) {
		CloseHandle(logFile);
	}
}

void Debug::logInternal(std::string message)
{
	message += "\r\n";
	if (logFile == INVALID_HANDLE_VALUE) {
		logFile = CreateFile("C:\\Users\\Public\\profiler_debug.log", GENERIC_WRITE, FILE_SHARE_READ,
			NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	}
	WriteFile(logFile, message.c_str(), (DWORD)strlen(message.c_str()), NULL, NULL);
}

void Debug::logStacktrace(std::string context) {
	log("Stacktrace: " + context);
	CustomStackWalker stackWalker;
	stackWalker.ShowCallstack();
	log(stackWalker.output);
}