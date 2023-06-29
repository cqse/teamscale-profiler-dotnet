#pragma once
#include <string>
#include <atlbase.h>

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
	HANDLE logFile = INVALID_HANDLE_VALUE;

	/**
	 * Create the log file. Must be the first method called on this object.
	 * This method is not thread-safe or reentrant.
	 */
	void createLogFile(std::string directory, std::string name, bool overwriteIfExists);

	/** Writes the given string to the log file. */
	int writeToFile(const char* string);

	/** Writes the given name-value pair to the log file. */
	void writeTupleToFile(const char* key, const char* value);

	/** Fills the given buffer with a string representing the current time. */
	std::string getFormattedCurrentTime();
};
