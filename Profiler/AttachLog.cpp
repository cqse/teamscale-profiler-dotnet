#include "AttachLog.h"


namespace {
	/** The key to log information about inlined methods. */
	const char* LOG_KEY_ATTACH = "Attach";
}


void AttachLog::createLogFile(std::string path) {
	FileLogBase::createLogFile(path, "attach.log");
}


void AttachLog::logAttach(std::string processID, std::string executablePath)
{
	std::string message = "Attached to \"" + executablePath + "\" with PID " + processID;
	FileLogBase::writeTupleToFile(LOG_KEY_ATTACH, message.c_str());
}


void AttachLog::logDetach(std::string processID, std::string executablePath)
{
	std::string message = "Detached to \"" + executablePath + "\" with PID " + processID;
	FileLogBase::writeTupleToFile(LOG_KEY_ATTACH, message.c_str());
}
