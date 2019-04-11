#include "AttachLog.h"
#include "utils/WindowsUtils.h"

AttachLog::AttachLog() {
}

AttachLog::~AttachLog() {
}

void AttachLog::createLogFile(std::string path) {
	FileLogBase::createLogFile(path, "attach.log", false);
}

void AttachLog::logAttach() {
	char timeStamp[BUFFER_SIZE];
	getFormattedCurrentTime(timeStamp, sizeof(timeStamp));
	std::string timeStampString(timeStamp);

	std::string message = timeStampString + " Attached to \"" + WindowsUtils::getPathOfThisProcess() + 
		"\" with PID " + std::to_string(WindowsUtils::getPidOfThisProcess());
	FileLogBase::writeTupleToFile(LOG_KEY_ATTACH, message.c_str());
}

void AttachLog::logDetach() {
	char timeStamp[BUFFER_SIZE];
	getFormattedCurrentTime(timeStamp, sizeof(timeStamp));
	std::string timeStampString(timeStamp);

	std::string message = timeStampString + " Detached from \"" + WindowsUtils::getPathOfThisProcess() + 
		"\" with PID " + std::to_string(WindowsUtils::getPidOfThisProcess());
	FileLogBase::writeTupleToFile(LOG_KEY_DETACH, message.c_str());
}