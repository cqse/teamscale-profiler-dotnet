#pragma once
#include "FunctionInfo.h"
#include <atlbase.h>
#include <string>
#include <vector>
#include <map>
#include <set>

class Log
{
public:
	Log();
	~Log();

	/** Create the log file and add general information. Must be the first method called on this object otherwise other calls will be ignored. */
	void createLogFile();

	/** Closes the log. Further calls to logging methods will be ignored. */
	void shutdown();

	/** Writes an info message. */
	void info(std::string message);

	/** Writes a warning message. */
	void warn(std::string message);

	/** Writes the profiled process into the log. Should only be called once. */
	void logProcess(std::string process);

	/** Writes info about a profiled assembly into the log. Should only be called once. */
	void logAssembly(std::string assembly);

	/** Returns the path to the log file. */
	std::string getLogFilePath();

	/** Write all information about the given jitted functions to the log. */
	void writeJittedFunctionInfosToLog(std::vector<FunctionInfo>* functions);

	/** Write all information about the given inlined functions to the log. */
	void writeInlinedFunctionInfosToLog(std::vector<FunctionInfo>* functions);

private:

	/** Synchronizes access to the log file. */
	CRITICAL_SECTION criticalSection;

	/** File into which results are written. INVALID_HANDLE if the file has not been opened yet. */
	HANDLE logFile = INVALID_HANDLE_VALUE;

	/** Path of the result file. */
	TCHAR logFilePath[_MAX_PATH];

	/** Writes the given string to the log file. */
	int writeToFile(const char* string);

	/** Writes the given name-value pair to the log file. */
	void writeTupleToFile(const char* key, const char* value);

	/** Write all information about the given functions to the log. */
	void writeFunctionInfosToLog(const char* key, std::vector<FunctionInfo>* functions);

	/** Write all information about the given function to the log. */
	void writeSingleFunctionInfoToLog(const char* key, FunctionInfo& info);

	/** Fills the given buffer with a string representing the current time. */
	void getFormattedCurrentTime(char *result, size_t size);
};

