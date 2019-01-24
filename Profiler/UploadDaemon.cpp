#include "UploadDaemon.h"
#include <windows.h>
#include <shellapi.h>
#include "utils/WindowsUtils.h"

UploadDaemon::UploadDaemon(std::string profilerPath)
{
	this->pathToExe = profilerPath + "\\UploadDaemon\\UploadDaemon.exe";
}

UploadDaemon::~UploadDaemon()
{
	// nothing to do
}

void UploadDaemon::launch(Log* log)
{
	bool successful = execute();
	if (!successful)
	{
		log->error("Failed to launch upload daemon " + pathToExe + ": " + WindowsUtils::getLastErrorAsString());
	}
}

void UploadDaemon::notifyShutdown()
{
	bool successful = execute();
	if (!successful)
	{
		// Cannot log unsuccessful shutdown, because log is already closed at this point.
	}
}

bool UploadDaemon::execute()
{
	// We need to unset COR_ENABLE_PROFILING so the upload daemon process is not
	// profiled as well. See https://docs.microsoft.com/en-us/windows/desktop/procthread/changing-environment-variables
	SetEnvironmentVariable("COR_ENABLE_PROFILING", "0");

	SHELLEXECUTEINFO shExecInfo;

	shExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);

	shExecInfo.fMask = NULL;
	shExecInfo.hwnd = NULL;
	shExecInfo.lpVerb = NULL;
	shExecInfo.lpFile = pathToExe.c_str();
	shExecInfo.lpParameters = NULL;
	shExecInfo.lpDirectory = NULL;
	shExecInfo.nShow = SW_NORMAL;
	shExecInfo.hInstApp = NULL;

	return ShellExecuteEx(&shExecInfo);

	// We reset the environment of this process. This does not affect the launched child process
	SetEnvironmentVariable("COR_ENABLE_PROFILING", "1");
}