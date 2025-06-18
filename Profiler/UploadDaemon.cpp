#include "UploadDaemon.h"
#include <windows.h>
#include <shellapi.h>
#include "utils/WindowsUtils.h"

namespace Profiler {

	UploadDaemon::UploadDaemon(std::string profilerPath)
	{
		this->pathToExe = profilerPath + "\\UploadDaemon\\UploadDaemon.exe";
	}

	UploadDaemon::~UploadDaemon()
	{
		// nothing to do
	}

	void UploadDaemon::launch(TraceLog& traceLog)
	{
		bool successful = execute();
		if (!successful)
		{
			traceLog.error("Failed to launch upload daemon " + pathToExe + ": " + WindowsUtils::getLastErrorAsString());
		}
	}

	void UploadDaemon::notifyShutdown()
	{
		// Cannot log unsuccessful execution, because log is already closed at this
		// point (otherwise it could not be uploaded).
		execute();
	}

	bool UploadDaemon::execute()
	{
		if (!WindowsUtils::isFile(this->pathToExe)) {
			WindowsUtils::reportError("UploadDaemon could not be started", "Failed to launch upload daemon to upload coverages into Teamscale as UploadDaemon.exe was not found.");
			return false;
		}
		// We need to unset COR_ENABLE_PROFILING so the upload daemon process is not
		// profiled as well. See https://docs.microsoft.com/en-us/windows/desktop/procthread/changing-environment-variables
		SetEnvironmentVariable("COR_ENABLE_PROFILING", "0");

		SHELLEXECUTEINFO shExecInfo;

		shExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);

		shExecInfo.fMask = 0L;
		shExecInfo.hwnd = nullptr;
		shExecInfo.lpVerb = nullptr;
		shExecInfo.lpFile = pathToExe.c_str();
		shExecInfo.lpParameters = nullptr;
		shExecInfo.lpDirectory = nullptr;
		shExecInfo.nShow = SW_NORMAL;
		shExecInfo.hInstApp = nullptr;

		return ShellExecuteEx(&shExecInfo);

		// We reset the environment of this process. This does not affect the launched child process
		SetEnvironmentVariable("COR_ENABLE_PROFILING", "1");
	}
}


