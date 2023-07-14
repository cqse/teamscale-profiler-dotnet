#pragma once
#include "log/TraceLog.h"
#include <string>

/**
 * Launches the upload daemon executable that runs in the background.
 */
class UploadDaemon {
public:

	/** Constructor. */
	UploadDaemon(std::string profilerPath);

	/** Destructor. */
	virtual ~UploadDaemon() noexcept;

	/** Starts the upload daemon in a new background process. */
	void launch(TraceLog& traceLog);

	/** Notifies the upload daemon that a profiler is shut down. */
	void notifyShutdown();

private:
	/** Path to the executable of the upload daemon. */
	std::string pathToExe;

	/** Invoke the upload daemon process */
	bool execute();
};
