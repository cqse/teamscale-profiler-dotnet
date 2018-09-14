#include "WindowsUtils.h"
#include <Windows.h>
#include <vector>

extern char** _environ;

/**
* Returns the message for the last WinAPI error (retrieved via GetLastError).
* Adapted from https://stackoverflow.com/a/17387176/1396068
*/
std::string WindowsUtils::getLastErrorAsString()
{
	DWORD errorMessageID = ::GetLastError();
	if (errorMessageID == 0) {
		// No error message has been recorded
		return std::string();
	}

	LPSTR messageBuffer = nullptr;
	size_t size = FormatMessageA(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
		NULL, errorMessageID, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPSTR)&messageBuffer, 0, NULL);

	std::string message(messageBuffer, size);
	LocalFree(messageBuffer);
	return message;
}

/** Return the value for the environment variable COR_PROFILER_<suffix> or the empty string if it is not set. */
std::string WindowsUtils::getConfigValueFromEnvironment(std::string suffix) {
	char value[32767 * sizeof(char)]; // maximum size according to http://msdn.microsoft.com/en-us/library/ms683188.aspx
	std::string name = "COR_PROFILER_" + suffix;
	if (GetEnvironmentVariable(name.c_str(), value, sizeof(value)) == 0) {
		return "";
	}
	return value;
}

std::vector<std::string> WindowsUtils::listEnvironmentVariables() {
	std::vector<std::string> variables;

	if (_environ == NULL) {
		return variables;
	}

	char** nextVariable = _environ;
	while (*nextVariable) {
		variables.push_back(*nextVariable);
		nextVariable++;
	}

	return variables;
}