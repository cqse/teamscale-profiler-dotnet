#pragma once
#include "FunctionInfo.h"
#include "FileLogBase.h"
#include <atlbase.h>
#include <string>
#include <vector>
#include <map>
#include <set>
#include "config/Config.h"

/**
 * Manages a log file on the file system to which both diagnostic messages and trace information is written.
 * Unless mentioned otherwise, all methods in this class are thread-safe and perform their own synchronization.
 */
class TraceLog: public FileLogBase
{
public:
	/** Write all information about the given jitted functions to the log. */
	void writeJittedFunctionInfosToLog(std::vector<FunctionInfo>* functions);

	/** Write all information about the given inlined functions to the log. */
	void writeInlinedFunctionInfosToLog(std::vector<FunctionInfo>* functions);

	/**
	 * Create the log file and add general information. Must be the first method called on this object.
	 * This method is not thread-safe or reentrant.
	 */
	void createLogFile(Config& config);

	void shutdown();

protected:
	/** The key to log information about the profiler startup. */
	const char* LOG_KEY_STARTED = "Started";

	/** The key to log information about the profiler shutdown. */
	const char* LOG_KEY_STOPPED = "Stopped";

	/** The key to log information about inlined methods. */
	const char* LOG_KEY_INLINED = "Inlined";

	/** The key to log information about jitted methods. */
	const char* LOG_KEY_JITTED = "Jitted";

private:
	/** Write all information about the given functions to the log. */
	void writeFunctionInfosToLog(const char* key, std::vector<FunctionInfo>* functions);

	/** Write all information about the given function to the log. */
	void writeSingleFunctionInfoToLog(const char* key, FunctionInfo& info);
};
