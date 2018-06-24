#pragma once
#include <string>

/**
 * Launches the uploader executable that runs in the background.
 */
class Uploader {
public:

	/** Constructor. */
	Uploader(std::string uploaderPath, std::string traceDirectory);

	/** Starts the uploader in a new process. */
	void launch();

private:

	/** Path to the executable of the uploader. */
	std::string pathToExe;
	
	/** Path to the directory that contains the trace files. */
	std::string traceDirectory;

};
