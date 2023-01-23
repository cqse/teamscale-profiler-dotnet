#include "AttachLog.h"
#include "utils/WindowsUtils.h"


AttachLog::~AttachLog() {
	// Nothing to do here, destructing is handled in FileLogBase
}


void AttachLog::createLogFile(std::string path) {
	FileLogBase::createLogFile(path, "attach.log", false, false);
}


void AttachLog::logAttach() {
	std::string timeStamp = getFormattedCurrentTime();
	std::string message = timeStamp + " Attached to \"" + WindowsUtils::getPathOfThisProcess() + 
		"\" with PID " + std::to_string(WindowsUtils::getPidOfThisProcess());
	writeTupleToFile(LOG_KEY_ATTACH, message.c_str(), false);
}


void AttachLog::logDetach() {
	std::string timeStamp = getFormattedCurrentTime();
	std::string message = timeStamp + " Detached from \"" + WindowsUtils::getPathOfThisProcess() +
		"\" with PID " + std::to_string(WindowsUtils::getPidOfThisProcess());
	writeTupleToFile(LOG_KEY_DETACH, message.c_str(), false);
}
