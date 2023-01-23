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

void FileLogBase::createLogFile(std::string directory, std::string name, bool overwriteIfExists, bool testCoverage) {
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
	if (testCoverage) {
		testLogFile = CreateFile(logFilePath.c_str(), FILE_APPEND_DATA, FILE_SHARE_READ,
			NULL, creationPolicy, FILE_ATTRIBUTE_NORMAL, NULL);
	}
	else {
		assemblyLogFile = CreateFile(logFilePath.c_str(), FILE_APPEND_DATA, FILE_SHARE_READ,
			NULL, creationPolicy, FILE_ATTRIBUTE_NORMAL, NULL);
	}
	
}

void FileLogBase::shutdown()
{
	EnterCriticalSection(&criticalSection);
	if (assemblyLogFile != INVALID_HANDLE_VALUE) {
		CloseHandle(assemblyLogFile);
	}
	if (testLogFile != INVALID_HANDLE_VALUE) {
		CloseHandle(testLogFile);
	}
	LeaveCriticalSection(&criticalSection);
}

int FileLogBase::writeToAssemblyFile(const char* string) {
	return writeToFile(string, assemblyLogFile);
}

int FileLogBase::writeToTestFile(const char* string) {
	return writeToFile(string, testLogFile);
}


int FileLogBase::writeToFile(const char* string, HANDLE logFile) {
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

void FileLogBase::writeTupleToFile(const char* key, const char* value, bool writeToCurrentTestFile) {
	char buffer[BUFFER_SIZE];
	sprintf_s(buffer, "%s=%s\r\n", key, value);
	if (writeToCurrentTestFile) {
		writeToTestFile(buffer);
	}
	else {
		writeToAssemblyFile(buffer);
	}
	
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
