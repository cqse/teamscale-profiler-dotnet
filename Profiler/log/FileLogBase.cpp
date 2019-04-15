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

void FileLogBase::createLogFile(std::string directory, std::string name, bool overwriteIfExists) {
	if (directory.empty()) {
		// c:\users\public is usually writable for everyone
		// we must use backslashes here or the WinAPI path manipulation functions will fail
		// to split the path correctly
		directory = "c:\\users\\public\\";
	}

	std::string logFilePath = directory + "\\" + name;

	DWORD creationPolicy = OPEN_ALWAYS;
	if (overwriteIfExists) {
		creationPolicy = CREATE_ALWAYS;
	}

	logFile = CreateFile(logFilePath.c_str(), FILE_APPEND_DATA, FILE_SHARE_READ,
		NULL, creationPolicy, FILE_ATTRIBUTE_NORMAL, NULL);
}

void FileLogBase::shutdown()
{
	EnterCriticalSection(&criticalSection);
	if (logFile != INVALID_HANDLE_VALUE) {
		CloseHandle(logFile);
	}
	LeaveCriticalSection(&criticalSection);
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

std::string FileLogBase::getFormattedCurrentTime() {
	char formattedTime[BUFFER_SIZE];
	SYSTEMTIME time;
	GetSystemTime(&time);
	// Four digits for milliseconds means we always have a leading 0 there.
	// We consider this legacy and keep it here for compatibility reasons.
	sprintf_s(formattedTime, sizeof(formattedTime), "%04d%02d%02d_%02d%02d%02d%04d", time.wYear,
		time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond,
		time.wMilliseconds);

	std::string result(formattedTime);
	return result;
}

void FileLogBase::writeTupleToFile(const char* key, const char* value) {
	char buffer[BUFFER_SIZE];
	sprintf_s(buffer, "%s=%s\r\n", key, value);
	writeToFile(buffer);
}

