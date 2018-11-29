#include "Stacktrace.h"
#include <stdio.h>
#include <atlbase.h>
#include "dbghelp.h"
#include <signal.h>
#include "Debug.h"
#include <signal.h>
#include <windows.h>
#include <atomic>
#include <string>
#include <exception>

/** Where to write the stacktrace in case of a crash. */
static std::string OUTPUT_FILE_PATH("C:\\Users\\Public\\teamscale-dotnet-profiler.minidump");
static HANDLE OUTPUT_FILE = INVALID_HANDLE_VALUE;

/*// based on dbghelp.h
typedef BOOL(WINAPI *MINIDUMPWRITEDUMP)(HANDLE hProcess, DWORD dwPid, HANDLE hFile, MINIDUMP_TYPE DumpType,
	CONST PMINIDUMP_EXCEPTION_INFORMATION ExceptionParam,
	CONST PMINIDUMP_USER_STREAM_INFORMATION UserStreamParam,
	CONST PMINIDUMP_CALLBACK_INFORMATION CallbackParam
	);

class StackWalker2 : public StackWalker
{
public:
	std::string output;

	void OnOutput(LPCSTR text) {
		output += text;
		output += "\r\n";
	}
};

static std::string getStackTrace()
{
	StackWalker2 stackWalker;
	stackWalker.ShowCallstack();
	return stackWalker.output;
}

void abortHandler(int signum)
{
	Debug::log("abrt");
	const char* name = NULL;
	switch (signum) {
	case SIGABRT: name = "SIGABRT";  break;
	case SIGSEGV: name = "SIGSEGV";  break;
	case SIGILL:  name = "SIGILL";   break;
	case SIGFPE:  name = "SIGFPE";   break;
	}

	std::string message;

	if (name) {
		message = "Caught signal " + std::to_string(signum) + " (" + name + ")\r\n";
	}
	else {
		message = "Caught signal " + std::to_string(signum) + "\r\n";
	}

	message += getStackTrace();

	HANDLE logFile = CreateFile(OUTPUT_FILE.c_str(), GENERIC_WRITE, FILE_SHARE_READ,
		NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	WriteFile(logFile, message.c_str(), (DWORD)strlen(message.c_str()), NULL, NULL);

	exit(signum);
}

void Stacktrace::printStacktraceOnAbort()
{
	SetUnhandledExceptionFilter(topLevelFilter);
}

long Stacktrace::topLevelFilter(struct _EXCEPTION_POINTERS *pExceptionInfo)
{
	Debug::log("abrt");
	HMODULE hDll = LoadLibrary("DBGHELP.DLL");
	LPCTSTR szResult = NULL;

	if (!hDll)
	{
		Debug::log("Couldn't find dbghelp.dll\n");
		return EXCEPTION_CONTINUE_SEARCH;
	}

	MINIDUMPWRITEDUMP pDump = (MINIDUMPWRITEDUMP)::GetProcAddress(hDll, "MiniDumpWriteDump");
	if (!pDump)
	{
		Debug::log("Couldn't find MiniDumpWriteDump function\n");
		return EXCEPTION_CONTINUE_SEARCH;
	}

	HANDLE hFile = ::CreateFile(OUTPUT_FILE.c_str(), GENERIC_WRITE, FILE_SHARE_WRITE, NULL, CREATE_ALWAYS,
		FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile == INVALID_HANDLE_VALUE)
	{
		Debug::log("Couldn't create minidump file\n");
		return EXCEPTION_CONTINUE_SEARCH;
	}

	_MINIDUMP_EXCEPTION_INFORMATION ExInfo;

	ExInfo.ThreadId = GetCurrentThreadId();
	ExInfo.ExceptionPointers = pExceptionInfo;
	ExInfo.ClientPointers = NULL;

	BOOL bOK = pDump(GetCurrentProcess(), GetCurrentProcessId(), hFile, (MINIDUMP_TYPE)(MiniDumpNormal | MiniDumpWithDataSegs), &ExInfo, NULL, NULL);

	LONG returnValue;
	if (bOK)
	{
		returnValue = EXCEPTION_EXECUTE_HANDLER;
	}
	else
	{
		Debug::log("Failed to write minidump file\n");
		returnValue = EXCEPTION_CONTINUE_SEARCH;
	}

	CloseHandle(hFile);
	return returnValue;
}*/

/** Taken from https://randomascii.wordpress.com/2012/07/05/when-even-crashing-doesnt-work/ */
static void EnableCrashingOnCrashes()
{
	typedef BOOL(WINAPI *tGetPolicy)(LPDWORD lpFlags);
	typedef BOOL(WINAPI *tSetPolicy)(DWORD dwFlags);
	const DWORD EXCEPTION_SWALLOWING = 0x1;

	HMODULE kernel32 = LoadLibraryA("kernel32.dll");
	tGetPolicy pGetPolicy = (tGetPolicy)GetProcAddress(kernel32,
		"GetProcessUserModeExceptionPolicy");
	tSetPolicy pSetPolicy = (tSetPolicy)GetProcAddress(kernel32,
		"SetProcessUserModeExceptionPolicy");
	if (pGetPolicy && pSetPolicy)
	{
		DWORD dwFlags;
		if (pGetPolicy(&dwFlags))
		{
			// Turn off the filter
			pSetPolicy(dwFlags & ~EXCEPTION_SWALLOWING);
		}
	}
	BOOL insanity = FALSE;
	SetUserObjectInformationA(GetCurrentProcess(),
		UOI_TIMERPROC_EXCEPTION_SUPPRESSION,
		&insanity, sizeof(insanity));
}

/** Taken from http://blog.kalmbach-software.de/2013/05/23/improvedpreventsetunhandledexceptionfilter/ */
static BOOL PreventSetUnhandledExceptionFilter()
{
	HMODULE hKernel32 = LoadLibrary(_T("kernel32.dll"));
	if (hKernel32 == NULL) return FALSE;
	void *pOrgEntry = GetProcAddress(hKernel32, "SetUnhandledExceptionFilter");
	if (pOrgEntry == NULL) return FALSE;

#ifdef _M_IX86
	// Code for x86:
	// 33 C0                xor         eax,eax
	// C2 04 00             ret         4
	unsigned char szExecute[] = { 0x33, 0xC0, 0xC2, 0x04, 0x00 };
#elif _M_X64
	// 33 C0                xor         eax,eax
	// C3                   ret
	unsigned char szExecute[] = { 0x33, 0xC0, 0xC3 };
#else
#error "The following code only works for x86 and x64!"
#endif

	SIZE_T bytesWritten = 0;
	BOOL bRet = WriteProcessMemory(GetCurrentProcess(),
		pOrgEntry, szExecute, sizeof(szExecute), &bytesWritten);
	return bRet;
}

static void exceptionHandler(EXCEPTION_POINTERS* excpInfo)
{
	std::string message = "handling...";
	WriteFile(OUTPUT_FILE, message.c_str(), (DWORD)strlen(message.c_str()), NULL, NULL);
}

static std::atomic_bool exceptionHandlerRan(false);
static LONG WINAPI unhandledException(EXCEPTION_POINTERS* excpInfo = NULL)
{
	bool handlerAlreadyRan = exceptionHandlerRan.exchange(true);
	if (handlerAlreadyRan) {
		return 0;
	}

	if (!excpInfo == NULL)
	{
		__try // Generate exception to get proper context in dump
		{
			RaiseException(EXCEPTION_BREAKPOINT, 0, 0, NULL);
		}
		__except (exceptionHandler(GetExceptionInformation()), EXCEPTION_EXECUTE_HANDLER)
		{
		}
	}
	else
	{
		exceptionHandler(excpInfo);
	}

	return 0;
}

static void invalidParameter(const wchar_t* expr, const wchar_t* func,
	const wchar_t* file, unsigned int line, uintptr_t reserved)
{
	unhandledException();
}

static void pureVirtualCall()
{
	unhandledException();
}

static void sigAbortHandler(int sig)
{
	// this is required, otherwise if there is another thread
	// simultaneously tries to abort process will be terminated
	signal(SIGABRT, sigAbortHandler);
	unhandledException();
}

static void terminateHandler() {
	unhandledException();
}

/** Taken from https://stackoverflow.com/a/48817594/1396068 */
void Stacktrace::printStacktraceOnAbort()
{
	/*OUTPUT_FILE = CreateFile(OUTPUT_FILE_PATH.c_str(), GENERIC_WRITE, FILE_SHARE_WRITE, NULL, CREATE_ALWAYS,
		FILE_ATTRIBUTE_NORMAL, NULL);

	SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX);
	SetUnhandledExceptionFilter(unhandledException);
	_set_invalid_parameter_handler(invalidParameter);
	_set_purecall_handler(pureVirtualCall);
	signal(SIGABRT, sigAbortHandler);
	_set_abort_behavior(0, 0);
	std::set_terminate(terminateHandler);
	std::set_unexpected(terminateHandler);
	EnableCrashingOnCrashes();
	PreventSetUnhandledExceptionFilter();*/
}