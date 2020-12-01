#pragma once
#include "FunctionInfo.h"
#include "FileLogBase.h"
#include <atlbase.h>
#include <string>
#include <vector>
#include <map>
#include <set>

/**
 * Manages a log file on the file system to which both diagnostic messages and trace information is written.
 * Unless mentioned otherwise, all methods in this class are thread-safe and perform their own synchronization.
 */
class TraceLog : public FileLogBase
{
public:
	virtual ~TraceLog() noexcept;

	/** Write all information about the given jitted functions to the log. */
	void writeJittedFunctionInfosToLog(std::vector<FunctionInfo>* functions);

	/** Write all information about the given inlined functions to the log. */
	void writeInlinedFunctionInfosToLog(std::vector<FunctionInfo>* functions);

#ifdef TIA
	/** Write all information about the given called functions to the log. */
	void writeCalledFunctionInfosToLog(std::vector<FunctionInfo>* functions);
#endif

	/**
	 * Create the log file and add general information.
	 * Can be called as an alternative for createLogFile method of the base class as first method called on the object.
	 * This method is not thread-safe or reentrant.
	 */
	void createLogFile(std::string targetDir);

	/** Writes a closing log entry to the file and closes the log file. Further calls to logging methods will be ignored. */
	void shutdown();

	/** Writes an info message. */
	void info(std::string message);

	/** Writes a warning message. */
	void warn(std::string message);

	/** Writes an error message. */
	void error(std::string message);

	/** Log a single environment variable definition. */
	void logEnvironmentVariable(std::string variable);

	/** Writes the profiled process into the log. Should only be called once. */
	void logProcess(std::string process);

	/** Writes info about a profiled assembly into the log. Should only be called once. */
	void logAssembly(std::string assembly);

	void logTestCase(std::string testName);

protected:
	/** The key to log information about the profiler startup. */
	const char* LOG_KEY_STARTED = "Started";

	/** The key to log information about the profiler shutdown. */
	const char* LOG_KEY_STOPPED = "Stopped";

	/** The key to log information about inlined methods. */
	const char* LOG_KEY_INLINED = "Inlined";

	/** The key to log information about jitted methods. */
	const char* LOG_KEY_JITTED = "Jitted";

#ifdef TIA
	/** The key to log information about called methods. */
	const char* LOG_KEY_CALLED = "Called";
#endif

	/** The key to log information about test cases. */
	const char* LOG_KEY_TESTCASE = "Test";

	/** The key to log information useful when interpreting the traces. */
	const char* LOG_KEY_INFO = "Info";

	/** The key to log information about non-critical problems. */
	const char* LOG_KEY_WARN = "Warn";

	/** The key to log information about errors that should be addressed but don't prevent the profiler from tracing method calls. */
	const char* LOG_KEY_ERROR = "Error";

	/** The key to log information about a single assembly. */
	const char* LOG_KEY_ASSEMBLY = "Assembly";

	/** The key to log information about the profiled process. */
	const char* LOG_KEY_PROCESS = "Process";

	/** The key to log information about the environment variables the profiled process sees. */
	const char* LOG_KEY_ENVIRONMENT = "Environment";

private:
	/** Write all information about the given functions to the log. */
	void writeFunctionInfosToLog(const char* key, std::vector<FunctionInfo>* functions);

	/** Write all information about the given function to the log. */
	void writeSingleFunctionInfoToLog(const char* key, FunctionInfo& info);
};
