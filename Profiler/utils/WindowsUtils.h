#pragma once

#include <string>
#include <vector>

class WindowsUtils {
public:

	/**
	* Returns the message for the last WinAPI error (retrieved via GetLastError).
	* Adapted from https://stackoverflow.com/a/17387176/1396068
	*/
	static std::string getLastErrorAsString();

	/** Return the value for the environment variable COR_PROFILER_<suffix> or the empty string if it is not set. */
	static std::string getConfigValueFromEnvironment(std::string suffix);

	/** Returns a list of all environment variables (in the format VAR=VALUE). Returns an empty list in case of errors. */
	static std::vector<std::string> listEnvironmentVariables();

	/** Returns the path to the profiled process. */
	static std::string getPathOfThisProcess();

	/** Returns the PID of the profiled process. */
	static unsigned long getPidOfThisProcess();

	/** Returns true only if the given pathe exists and is a file. */
	static bool isFile(std::string path);

	/** Returns true if the directory and parent directories exist or could be created. False otherwise. */
	static bool ensureDirectoryExists(std::string directory);
};