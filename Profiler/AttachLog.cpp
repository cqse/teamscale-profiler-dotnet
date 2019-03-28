#include "utils/WindowsUtils.h"
#include "AttachLog.h"


namespace {
	/** The key to log information about processes to which the profiler is attached to. */
	const char* LOG_KEY_ATTACH = "Attach";
}


void AttachLog::createLogFile(std::string path) {
	FileLogBase::createLogFile(path, "attach.log");
}


void AttachLog::logAttach()
{
	std::string message = "Attached to \"" + WindowsUtils::getPathOfThisProcess() + 
		"\" with PID " + std::to_string(WindowsUtils::getPidOfThisProcess());
	FileLogBase::writeTupleToFile(LOG_KEY_ATTACH, message.c_str());
}


void AttachLog::logDetach()
{
	std::string message = "Detached from \"" + WindowsUtils::getPathOfThisProcess() + 
		"\" with PID " + std::to_string(WindowsUtils::getPidOfThisProcess());
	FileLogBase::writeTupleToFile(LOG_KEY_ATTACH, message.c_str());
}
