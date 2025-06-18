#include "AttachLog.h"
#include "utils/WindowsUtils.h"

namespace Profiler {
	AttachLog::~AttachLog() {
		// Nothing to do here, destructing is handled in FileLogBase
	}


	void AttachLog::createLogFile(std::string path) {
		FileLogBase::createLogFile(path, "attach.log");
	}


	void AttachLog::logAttach() {
		std::string timeStamp = getFormattedCurrentTime();
		std::string message = timeStamp + " Attached to \"" + WindowsUtils::getPathOfThisProcess() +
			"\" with PID " + std::to_string(WindowsUtils::getPidOfThisProcess());
		writeTupleToFile(LOG_KEY_ATTACH, message);
	}


	void AttachLog::logDetach() {
		std::string timeStamp = getFormattedCurrentTime();
		std::string message = timeStamp + " Detached from \"" + WindowsUtils::getPathOfThisProcess() +
			"\" with PID " + std::to_string(WindowsUtils::getPidOfThisProcess());
		writeTupleToFile(LOG_KEY_DETACH, message);
	}
}

