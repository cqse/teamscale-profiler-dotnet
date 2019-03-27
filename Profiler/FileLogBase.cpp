#include "FileLogBase.h"
#include <winuser.h>
#include "version.h"

FileLogBase::FileLogBase()
{
	InitializeCriticalSection(&criticalSection);
}


FileLogBase::~FileLogBase()
{
	DeleteCriticalSection(&criticalSection);
}

namespace {
	/** The key to log information useful when interpreting the traces. */
	const char* LOG_KEY_INFO = "Info";

	/** The key to log information about non-critical problems. */
	const char* LOG_KEY_WARN = "Warn";

	/** The key to log information about errors that should be addressed but don't prevent the profiler from tracing method calls. */
	const char* LOG_KEY_ERROR = "Error";

	/** The key to log information about a single assembly. */
	const char* LOG_KEY_ASSEMBLY = "Assembly";

	/** The key to log information about the profiled process. */
	const char* LOG_KEY_PROCESS = "Process";

	/** The key to log information about the environment variables the profiled process sees. */
	const char* LOG_KEY_ENVIRONMENT = "Environment";

	/** The key to log information about the profiler startup. */
	const char* LOG_KEY_STARTED = "Started";

	/** The key to log information about the profiler shutdown. */
	const char* LOG_KEY_STOPPED = "Stopped";
}

void FileLogBase::createLogFile(std::string directory, std::string name) {
	if (directory.empty()) {
		// c:\users\public is usually writable for everyone
		// we must use backslashes here or the WinAPI path manipulation functions will fail
		// to split the path correctly
		directory = "c:\\users\\public\\";
	}

	char timeStamp[BUFFER_SIZE];
	getFormattedCurrentTime(timeStamp, sizeof(timeStamp));

	// we must use backslash here or the WinAPI path manipulation functions will fail
	// to split the path correctly
	logFilePath = directory + "\\" + name;

	logFile = CreateFile(logFilePath.c_str(), GENERIC_WRITE, FILE_SHARE_READ,
		NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

	writeTupleToFile(LOG_KEY_INFO, VERSION_DESCRIPTION);
	writeTupleToFile(LOG_KEY_STARTED, timeStamp);
}

void FileLogBase::shutdown()
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

std::string FileLogBase::getLogFilePath()
{
	return std::string(logFilePath);
}


int FileLogBase::writeToFile(const char* string) {
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

void FileLogBase::info(std::string message) {
	writeTupleToFile(LOG_KEY_INFO, message.c_str());
}

void FileLogBase::warn(std::string message)
{
	writeTupleToFile(LOG_KEY_WARN, message.c_str());
}

void FileLogBase::error(std::string message)
{
	writeTupleToFile(LOG_KEY_ERROR, message.c_str());
}

void FileLogBase::logEnvironmentVariable(std::string variable)
{
	writeTupleToFile(LOG_KEY_ENVIRONMENT, variable.c_str());
}

void FileLogBase::logProcess(std::string process)
{
	writeTupleToFile(LOG_KEY_PROCESS, process.c_str());
}

void FileLogBase::logAssembly(std::string assembly)
{
	writeTupleToFile(LOG_KEY_ASSEMBLY, assembly.c_str());
}

void FileLogBase::getFormattedCurrentTime(char *result, size_t size) {
	SYSTEMTIME time;
	GetSystemTime(&time);
	// Four digits for milliseconds means we always have a leading 0 there.
	// We consider this legacy and keep it here for compatibility reasons.
	sprintf_s(result, size, "%04d%02d%02d_%02d%02d%02d%04d", time.wYear,
		time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond,
		time.wMilliseconds);
}

void FileLogBase::writeTupleToFile(const char* key, const char* value) {
	char buffer[BUFFER_SIZE];
	sprintf_s(buffer, "%s=%s\r\n", key, value);
	writeToFile(buffer);
}

