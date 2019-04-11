#pragma once
#include "FileLogBase.h"

class AttachLog: public FileLogBase
{
public:
	void createLogFile(std::string path);

	void logAttach();

	void logDetach();

protected :
	/** The key to log information about processes to which the profiler is attached to. */
	const char* LOG_KEY_ATTACH = "Attach";
};

