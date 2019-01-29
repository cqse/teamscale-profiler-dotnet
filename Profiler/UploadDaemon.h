#pragma once
#include "Log.h"
#include <string>

/**
 * Launches the upload daemon executable that runs in the background.
 */
class UploadDaemon {
public:

	/** Constructor. */
	UploadDaemon(std::string profilerPath, Log* log);

	/** Destructor. */
	virtual ~UploadDaemon() noexcept;

	/** Starts the upload daemon in a new background process. */
	void launch();

private:

	/** The log for error reporting. */
	Log * log;

	/** Path to the executable of the upload daemon. */
	std::string pathToExe;
};
