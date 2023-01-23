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

	/** Closes the log. Further calls to logging methods will be ignored. */
	void shutdown();

protected:
	/** Synchronizes access to the log file. */
	CRITICAL_SECTION criticalSection;

	/** File into which results are written. INVALID_HANDLE if the file has not been opened yet. */
	HANDLE assemblyLogFile = INVALID_HANDLE_VALUE;

	/** File into which results are written. INVALID_HANDLE if the file has not been opened yet. */
	HANDLE testLogFile = INVALID_HANDLE_VALUE;

	/**
	 * Create the log file. Must be the first method called on this object.
	 * This method is not thread-safe or reentrant.
	 */
	void createLogFile(std::string directory, std::string name, bool overwriteIfExists, bool testCoverage);

	int writeToAssemblyFile(const char* string);

	int writeToTestFile(const char* string);

	/** Writes the given string to the log file. */
	int writeToFile(const char* string, HANDLE logFile);

	/** Writes the given name-value pair to the log file. */
	void writeTupleToFile(const char* key, const char* value, bool writeToCurrentTestFile);

	/** Fills the given buffer with a string representing the current time. */
	std::string getFormattedCurrentTime();
};
