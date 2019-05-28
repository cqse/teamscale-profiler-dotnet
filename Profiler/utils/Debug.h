#pragma once
#include <atlbase.h>
#include <string>

/**
 * Helper for debugging. Logs messages to C:\Users\Public\profiler_debug.log.
 * The log will be overwritten with every run of the profiler.
 * This class implements the singleton pattern so we are able to log from every class of the profiler.
 */
class Debug
{
public:

	/** Logs the given message to the debug log. */
	void log(std::string message);

	/** Logs the given context and the current stacktrace to the debug log. */
	void logErrorWithStracktrace(std::string context);

	static Debug& getInstance();

private:
	Debug();
	virtual ~Debug();

	HANDLE logFile = INVALID_HANDLE_VALUE;
	CRITICAL_SECTION loggingSynchronization;
};
