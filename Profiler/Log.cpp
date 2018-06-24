#include "Log.h"
#include "version.h"
#include <vector>
#include <fstream>
#include <algorithm>
#include <winuser.h>

static const int BUFFER_SIZE = 2048;

namespace {

	/** The key to log information useful when interpreting the traces. */
	const char* LOG_KEY_INFO = "Info";

	/** The key to log information about non-critical error conditions. */
	const char* LOG_KEY_WARN = "Warn";

	/** The key to log information about a single assembly. */
	const char* LOG_KEY_ASSEMBLY = "Assembly";

	/** The key to log information about the profiled process. */
	const char* LOG_KEY_PROCESS = "Process";

	/** The key to log information about inlined methods. */
	const char* LOG_KEY_INLINED = "Inlined";

	/** The key to log information about jitted methods. */
	const char* LOG_KEY_JITTED = "Jitted";

	/** The key to log information about the profiler startup. */
	const char* LOG_KEY_STARTED = "Started";

	/** The key to log information about the profiler shutdown. */
	const char* LOG_KEY_STOPPED = "Stopped";

}

Log::Log()
{
	InitializeCriticalSection(&criticalSection);
}

Log::~Log()
{
	DeleteCriticalSection(&criticalSection);
}

void Log::info(std::string message) {
	writeTupleToFile(LOG_KEY_INFO, message.c_str());
}

void Log::warn(std::string message)
{
	writeTupleToFile(LOG_KEY_WARN, message.c_str());
}

void Log::logProcess(std::string process)
{
	writeTupleToFile(LOG_KEY_PROCESS, process.c_str());
}

void Log::logAssembly(std::string assembly)
{
	writeTupleToFile(LOG_KEY_ASSEMBLY, assembly.c_str());
}

void Log::shutdown()
{
	char timeStamp[BUFFER_SIZE];
	getFormattedCurrentTime(timeStamp, sizeof(timeStamp));
	writeTupleToFile(LOG_KEY_STOPPED, timeStamp);

	writeTupleToFile(LOG_KEY_INFO, "Shutting down coverage profiler");

	EnterCriticalSection(&criticalSection);
	if (logFile != INVALID_HANDLE_VALUE) {
		CloseHandle(logFile);
	}
	LeaveCriticalSection(&criticalSection);
}

std::string Log::getLogFilePath()
{
	return std::string(logFilePath);
}

void Log::writeJittedFunctionInfosToLog(std::vector<FunctionInfo>* functions)
{
	writeFunctionInfosToLog(LOG_KEY_JITTED, functions);
}

void Log::writeInlinedFunctionInfosToLog(std::vector<FunctionInfo>* functions)
{
	writeFunctionInfosToLog(LOG_KEY_INLINED, functions);
}

void Log::createLogFile() {
	char targetDir[BUFFER_SIZE];
	if (!GetEnvironmentVariable("COR_PROFILER_TARGETDIR", targetDir,
		sizeof(targetDir))) {
		// c:/users/public is usually writable for everyone
		strcpy_s(targetDir, "c:/users/public/");
	}

	char logFileName[BUFFER_SIZE];
	char timeStamp[BUFFER_SIZE];
	getFormattedCurrentTime(timeStamp, sizeof(timeStamp));

	sprintf_s(logFileName, "%s/coverage_%s.txt", targetDir, timeStamp);
	_tcscpy_s(logFilePath, logFileName);

	EnterCriticalSection(&criticalSection);
	logFile = CreateFile(logFilePath, GENERIC_WRITE, FILE_SHARE_READ,
		NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

	writeTupleToFile(LOG_KEY_INFO, VERSION_DESCRIPTION);

	writeTupleToFile(LOG_KEY_STARTED, timeStamp);
	LeaveCriticalSection(&criticalSection);
}

void Log::getFormattedCurrentTime(char *result, size_t size) {
	SYSTEMTIME time;
	GetSystemTime(&time);
	// Four digits for milliseconds means we always have a leading 0 there.
	// We consider this legacy and keep it here for compatibility reasons.
	sprintf_s(result, size, "%04d%02d%02d_%02d%02d%02d%04d", time.wYear,
		time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond,
		time.wMilliseconds);
}



void Log::writeTupleToFile(const char* key, const char* value) {
	char buffer[BUFFER_SIZE];
	sprintf_s(buffer, "%s=%s\r\n", key, value);
	writeToFile(buffer);
}

int Log::writeToFile(const char* string) {
	int retVal = 0;
	DWORD dwWritten = 0;

	if (logFile != INVALID_HANDLE_VALUE) {
		EnterCriticalSection(&criticalSection);
		if (TRUE == WriteFile(logFile, string,
			(DWORD)strlen(string), &dwWritten, NULL)) {
			retVal = dwWritten;
		}
		else {
			retVal = 0;
		}
		LeaveCriticalSection(&criticalSection);
	}

	return retVal;
}

void Log::writeFunctionInfosToLog(const char* key, std::vector<FunctionInfo>* functions) {
	for (std::vector<FunctionInfo>::iterator i = functions->begin(); i != functions->end(); i++) {
		writeSingleFunctionInfoToLog(key, *i);
	}
}

void Log::writeSingleFunctionInfoToLog(const char* key, FunctionInfo& info) {
	EnterCriticalSection(&criticalSection);
	char signature[BUFFER_SIZE];
	signature[0] = '\0';
	sprintf_s(signature, "%i:%i", info.assemblyNumber,
		info.functionToken);
	writeTupleToFile(key, signature);
	LeaveCriticalSection(&criticalSection);
}