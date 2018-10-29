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

void Debug::logStacktraceAndCreateMinidump(std::string context, struct _EXCEPTION_POINTERS *exceptionPointers) {
	log("Stacktrace: " + context);
	CustomStackWalker stackWalker;
	stackWalker.ShowCallstack();
	log(stackWalker.output);
	writeMinidump(exceptionPointers);
}

void Debug::writeMinidump(struct _EXCEPTION_POINTERS *exceptionPointers)
{
	HMODULE dllHandle = LoadLibrary("DBGHELP.DLL");
	LPCTSTR szResult = NULL;

	if (!dllHandle)
	{
		Debug::log("Couldn't find dbghelp.dll\n");
		return;
	}

	MINIDUMPWRITEDUMP dumpFunction = (MINIDUMPWRITEDUMP)::GetProcAddress(dllHandle, "MiniDumpWriteDump");
	if (!dumpFunction)
	{
		Debug::log("Couldn't find MiniDumpWriteDump function\n");
		return;
	}

	HANDLE hFile = CreateFile("C:\\Users\\Public\\profiler.minidump", GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS,
		FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile == INVALID_HANDLE_VALUE)
	{
		Debug::log("Couldn't create minidump file\n");
		return;
	}

	_MINIDUMP_EXCEPTION_INFORMATION exceptionInfo;

	exceptionInfo.ThreadId = GetCurrentThreadId();
	exceptionInfo.ExceptionPointers = exceptionPointers;
	exceptionInfo.ClientPointers = NULL;

	BOOL dumpSucceeded = dumpFunction(GetCurrentProcess(), GetCurrentProcessId(), hFile, (MINIDUMP_TYPE)(MiniDumpNormal | MiniDumpWithDataSegs), &exceptionInfo, NULL, NULL);
	if (!dumpSucceeded)
	{
		Debug::log("Failed to write minidump file\n");
	}

	CloseHandle(hFile);
}