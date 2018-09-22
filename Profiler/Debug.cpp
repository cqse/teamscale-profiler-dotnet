#include "Debug.h"

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
	if (logFile == INVALID_HANDLE_VALUE) {
		logFile = CreateFile("C:\\Users\\Public\\profiler_debug.log", GENERIC_WRITE, FILE_SHARE_READ,
			NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	}
	WriteFile(logFile, message.c_str(), (DWORD)strlen(message.c_str()), NULL, NULL);
}