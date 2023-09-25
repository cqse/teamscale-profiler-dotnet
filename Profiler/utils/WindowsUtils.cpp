#include "WindowsUtils.h"
#include "Debug.h"
#include <vector>
#include <algorithm>
#include <stdlib.h>
#include <chrono>
#include <thread>

#include <fstream>

/** Maximum size of an enironment variable value according to http://msdn.microsoft.com/en-us/library/ms683188.aspx */
static const size_t MAX_ENVIRONMENT_VARIABLE_VALUE_SIZE = 32767 * sizeof(char);

/** Closes error window after 10 seconds, in case the profiled application is running in an automated environment where user interaction is not possible. */
void CloseErrorWindow(LPCTSTR errorTitle) {
	std::this_thread::sleep_for(std::chrono::milliseconds(10000));
	HWND hWnd = FindWindow(NULL, errorTitle);
	if (hWnd) {
		PostMessage(hWnd, WM_CLOSE, 0, 0);
	}
}

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

std::string WindowsUtils::getPathConfigValueFromEnvironment() {
	std::string value;
	value = getConfigValueFromEnvironment("PATH");
	if (value != "") {
		return value;
	}
	value = getConfigValueFromEnvironment("PATH_32");
	if (value != "") {
		return value;
	}
	value = getConfigValueFromEnvironment("PATH_64");
	return value;
}

std::string WindowsUtils::getConfigValueFromEnvironment(std::string suffix) {
	std::transform(suffix.begin(), suffix.end(), suffix.begin(), ::toupper);

	char value[MAX_ENVIRONMENT_VARIABLE_VALUE_SIZE];
	std::string nameForFramework = "COR_PROFILER_" + suffix;
	std::string nameForCore = "CORECLR_PROFILER_" + suffix;
	if (!GetEnvironmentVariable(nameForFramework.c_str(), value, MAX_ENVIRONMENT_VARIABLE_VALUE_SIZE)
		&& !GetEnvironmentVariable(nameForCore.c_str(), value, MAX_ENVIRONMENT_VARIABLE_VALUE_SIZE)) {
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

std::string WindowsUtils::getPathOfProfiler() {
	char profilerPath[_MAX_PATH];

	HMODULE hm = NULL;
	if (GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS |
		GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
		(LPCSTR)&getPathOfProfiler, &hm) != 0)
	{
		size_t length = GetModuleFileName(hm, profilerPath, _MAX_PATH);
		if (length != 0)
		{
			return std::string(profilerPath, length);
		}
	}

	// Failed to retrieve module path, try to retrieve it from the environment variable instead
	return getPathConfigValueFromEnvironment();
}

std::string WindowsUtils::getPathOfThisProcess() {
	char appPath[_MAX_PATH];
	appPath[0] = 0;

	size_t length = GetModuleFileName(NULL, appPath, MAX_PATH);
	if (length == 0) {
		return "Failed to read application path";
	}
	return std::string(appPath, length);
}

unsigned long WindowsUtils::getPidOfThisProcess() {
	return GetCurrentProcessId();
}

bool WindowsUtils::isFile(std::string path)
{
	DWORD dwAttrib = GetFileAttributes(path.c_str());
	// check if it's a valid path and not a directory
	return (dwAttrib != INVALID_FILE_ATTRIBUTES && !(dwAttrib & FILE_ATTRIBUTE_DIRECTORY));
}

bool WindowsUtils::ensureDirectoryExists(std::string directory) {
	static const std::string separators("\\/");

	DWORD dwAttrib = GetFileAttributes(directory.c_str());

	if (isFile(directory)) {
		return false;
	}

	if (dwAttrib != INVALID_FILE_ATTRIBUTES && (dwAttrib & FILE_ATTRIBUTE_DIRECTORY || dwAttrib & FILE_ATTRIBUTE_REPARSE_POINT)) {
		// directory or junction exists
		return true;
	}

	std::size_t indexOfSeparator = directory.find_last_of(separators);
	if (indexOfSeparator != std::wstring::npos) {
		if (!ensureDirectoryExists(directory.substr(0, indexOfSeparator))) {
			return false;
		}
	}

	Debug::getInstance().log("Create: " + directory);
	return CreateDirectory(directory.c_str(), NULL) == TRUE;
}

bool WindowsUtils::isDirectoryWritable(std::string directory)
{
	try {
		std::string tempFileName = directory + "/" + std::to_string(std::time(nullptr)) + ".tmp";

		// Attempt to create and immediately delete a temporary file in the directory.
		std::ofstream fs(tempFileName, std::ios::binary | std::ios::trunc);
		if (fs.is_open()) {
			fs.close();
			std::remove(tempFileName.c_str());
			return true;
		}
		return false;
	}
	catch (...) {
		return false;
	}
}
}

void WindowsUtils::reportError(LPCTSTR errorTitle, LPCTSTR errorMessage) {
	HANDLE h_event_log = RegisterEventSource(0, ".NET Runtime");
	ReportEvent(h_event_log, EVENTLOG_ERROR_TYPE, 0, 0x3E8, 0, 1, 0, &errorMessage, 0);

	char appPool[BUFFER_SIZE];
	bool isRunningInAppPool = GetEnvironmentVariable("APP_POOL_ID", appPool, sizeof(appPool));
	if (!isRunningInAppPool) {
		std::thread error_window_closing_thread(CloseErrorWindow, errorTitle);
		error_window_closing_thread.detach();
		MessageBox(NULL, errorMessage, errorTitle, MB_OK | MB_ICONERROR);
	}
}
