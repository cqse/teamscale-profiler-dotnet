#include "FileLogBase.h"
#include "utils/WindowsUtils.h"
#include "utils/Debug.h"
#include <winuser.h>
#include "version.h"


namespace Profiler {
	FileLogBase::FileLogBase()
	{
		InitializeCriticalSection(&criticalSection);
	}


	FileLogBase::~FileLogBase()
	{
		DeleteCriticalSection(&criticalSection);
	}

	void FileLogBase::createLogFile(std::string directory, std::string name) {
		const std::string fallbackDirectory = "c:\\users\\public\\";
		if (directory.empty()) {
			// c:\users\public is usually writable for everyone
			// we must use backslashes here or the WinAPI path manipulation functions will fail
			// to split the path correctly
			directory = fallbackDirectory;
		}

		if (!WindowsUtils::ensureDirectoryExists(directory)) {
			Debug::getInstance().log("Cannot create directory '" + directory + "', falling back to: " + fallbackDirectory);
			directory = fallbackDirectory;
		}
		if (!WindowsUtils::isDirectoryWritable(directory)) {
			Debug::getInstance().log("Cannot write to directory '" + directory + "', falling back to: " + fallbackDirectory);
			directory = fallbackDirectory;
		}

		std::string logFilePath = directory + "\\" + name;

		logFile = std::wofstream(logFilePath);
	}

	void FileLogBase::shutdown()
	{
		EnterCriticalSection(&criticalSection);
		if (logFile.is_open()) {
			logFile.close();
		}
		LeaveCriticalSection(&criticalSection);
	}

	void FileLogBase::writeWideToFile(const std::wstring& string) {
		if (logFile.is_open()) {
			EnterCriticalSection(&criticalSection);
			logFile << string;
			LeaveCriticalSection(&criticalSection);
		}
	}

	void FileLogBase::writeWideTupleToFile(const std::wstring& key, const std::wstring& value) {
		const std::wstring entry = key + L"=" + value + L"\n";
		writeWideToFile(entry);
	}


	void FileLogBase::writeToFile(const std::string& string) {
		std::wstring wide = converter.from_bytes(string);
		writeWideToFile(wide);
	}

	void FileLogBase::writeTupleToFile(const std::string& key, const std::string& value) {
		const std::string entry = key + "=" + value + "\n";
		writeToFile(entry);
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
}


