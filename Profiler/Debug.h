#pragma once
#include <atlbase.h>
#include <string>

/**
 * Helper for debugging. Logs messages to C:\Users\Public\profiler_debug.log.
 * The log will be overwritten with every run of the profiler.
 */
class Debug
{
public:
	/** Logs the given message to the debug log. */
	static void log(std::string message);

	/** Logs the current stack trace. */
	static void logStacktrace(std::string context);

	virtual ~Debug();

private:
	Debug();

	void logInternal(std::string message);

	/**
	 * Implements a thread-safe singleton pattern and ensures that the instance
	 * is properly destroyed and thus the file handle freed.
	 * See https://stackoverflow.com/a/1008289/1396068
	 */
	static Debug& getInstance()
	{
		// Guaranteed to be destroyed.
		// Instantiated on first use.
		static Debug instance;
		return instance;
	}

	HANDLE logFile = INVALID_HANDLE_VALUE;
	CRITICAL_SECTION loggingSynchronization;
};
