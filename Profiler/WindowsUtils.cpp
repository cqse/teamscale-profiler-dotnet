#include "WindowsUtils.h"
#include <Windows.h>
#include <vector>
#include  <algorithm>
#include "Debug.h"

extern char** _environ;

/** Maximum size of an enironment variable value according to http://msdn.microsoft.com/en-us/library/ms683188.aspx */
static const size_t MAX_ENVIRONMENT_VARIABLE_VALUE_SIZE = 32767 * sizeof(char);

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

std::string WindowsUtils::getConfigValueFromEnvironment(std::string suffix) {
	std::transform(suffix.begin(), suffix.end(), suffix.begin(), ::toupper);

	char value[MAX_ENVIRONMENT_VARIABLE_VALUE_SIZE];
	std::string name = "COR_PROFILER_" + suffix;
	if (!GetEnvironmentVariable(name.c_str(), value, MAX_ENVIRONMENT_VARIABLE_VALUE_SIZE)) {
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

std::string WindowsUtils::getPathOfThisProcess() {
	char appPath[_MAX_PATH];
	appPath[0] = 0;

	if (GetModuleFileName(NULL, appPath, MAX_PATH)) {
		return std::string(appPath);
	}
	return "Failed to read application path";
}