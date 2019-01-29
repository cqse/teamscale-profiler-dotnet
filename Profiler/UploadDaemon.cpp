#include "UploadDaemon.h"
#include <windows.h>
#include <shellapi.h>
#include "utils/WindowsUtils.h"

UploadDaemon::UploadDaemon(std::string profilerPath, Log* log)
{
	this->pathToExe = profilerPath + "\\UploadDaemon\\UploadDaemon.exe";
	this->log = log;
}

UploadDaemon::~UploadDaemon()
{
	// nothing to do
}

void UploadDaemon::launch()
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

	bool successful = ShellExecuteEx(&shExecInfo);
	if (!successful) {
		log->error("Failed to launch upload daemon " + pathToExe + ": " + WindowsUtils::getLastErrorAsString());
	}

	// We reset the environment of this process. This does not affect the launched child process
	SetEnvironmentVariable("COR_ENABLE_PROFILING", "1");
}