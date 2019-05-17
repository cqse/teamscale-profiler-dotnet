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
	Debug();

	/** Logs the given message to the debug log. */
	void log(std::string message);

	/** Logs the given stack trace to the debug log. */
	void logStacktrace(std::string context);

	virtual ~Debug();
private:
	HANDLE logFile = INVALID_HANDLE_VALUE;
	CRITICAL_SECTION loggingSynchronization;
};
