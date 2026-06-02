#pragma once

#include <string>
#include <vector>
#include <Windows.h>

namespace Profiler {
	static const int BUFFER_SIZE = 2048;

	class WindowsUtils {
	public:

		/**
		* Returns the message for the last WinAPI error (retrieved via GetLastError).
		* Adapted from https://stackoverflow.com/a/17387176/1396068
		*/
		static std::string getLastErrorAsString();

		/** Return the value for the profiler path (extracting from _PATH, _PATH_32 or _PATH_64) or the empty string if it is not set. */
		static std::string getPathConfigValueFromEnvironment();

		/** Return the value for the environment variable COR_PROFILER_<suffix>, CORECLR_PROFILER_<suffix> or the empty string if neither of them are set. */
		static std::string getConfigValueFromEnvironment(std::string suffix);

		/** Returns a list of all environment variables (in the format VAR=VALUE). Returns an empty list in case of errors. */
		static std::vector<std::string> listEnvironmentVariables();

		/** Returns the path to the profiler. */
		static std::string getPathOfProfiler();

		/** Returns the path to the profiled process. */
		static std::string getPathOfThisProcess();

		/** Returns the PID of the profiled process. */
		static unsigned long getPidOfThisProcess();

		/** Returns true only if the given pathe exists and is a file. */
		static bool isFile(std::string path);

		/** Returns true if the directory and parent directories exist or could be created. False otherwise. */
		static bool ensureDirectoryExists(std::string directory);

		/** Writes an Event Log error about the errors. If the application is not running in an application pool, then also pops up a message box with the error. */
		static void reportError(LPCTSTR errorTitle, LPCTSTR errorMessage);

		/** Returns true if the directory is writable by the current user. False otherwise. */
		static bool isDirectoryWritable(std::string directory);

	};
}

