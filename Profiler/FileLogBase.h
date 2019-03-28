#pragma once
#include <string>
#include <atlbase.h>

static const int BUFFER_SIZE = 2048;

/**
 * Manages a log file on the file system.
 * Unless mentioned otherwise, all methods in this class are thread-safe and perform their own synchronization.
 */
class FileLogBase
{
public:
	FileLogBase();
	virtual ~FileLogBase() noexcept;

	void createLogFile(std::string directory, std::string name);

	/** Closes the log. Further calls to logging methods will be ignored. */
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

protected:
	/** Synchronizes access to the log file. */
	CRITICAL_SECTION criticalSection;

	/** File into which results are written. INVALID_HANDLE if the file has not been opened yet. */
	HANDLE logFile = INVALID_HANDLE_VALUE;

	/** Writes the given string to the log file. */
	int writeToFile(const char* string);

	/** Writes the given name-value pair to the log file. */
	void writeTupleToFile(const char* key, const char* value);

	/** Fills the given buffer with a string representing the current time. */
	void getFormattedCurrentTime(char *result, size_t size);
};
