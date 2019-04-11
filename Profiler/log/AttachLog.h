#pragma once
#include "FileLogBase.h"

/**
 * Manages a log file on the file system to which profiler attach events (time, executable name and process ID) are written.
 * Unless mentioned otherwise, all methods in this class are thread-safe and perform their own synchronization.
 */
class AttachLog: public FileLogBase
{
public:

	/**
	 * Create the log file.
	 * Can be called as an alternative for createLogFile method of the base class as first method called on the object.
	 * This method is not thread-safe or reentrant.
	 */
	void createLogFile(std::string path);

	/**
	 * Log an attach event with time, executable name and process ID
	 */
	void logAttach();

	/**
	 * Log an detach event with time, executable name and process ID
	 */
	void logDetach();

protected :
	/** The key to log information about processes to which the profiler is attached to. */
	const char* LOG_KEY_ATTACH = "Attach";

	/** The key to log information about processes to which the profiler was attached to and is currently detatching from. */
	const char* LOG_KEY_DETACH = "Detach";
};

