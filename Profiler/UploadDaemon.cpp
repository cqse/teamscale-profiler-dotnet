#include "UploadDaemon.h"
#include <windows.h>
#include <shellapi.h>
#include "WindowsUtils.h"

UploadDaemon::UploadDaemon(std::string profilerPath, std::string traceDirectory, Log* log)
{
	this->pathToExe = profilerPath + "\\UploadDaemon\\UploadDaemon.exe";
	this->traceDirectory = traceDirectory;
	this->log = log;
}

UploadDaemon::~UploadDaemon()
{
	// nothing to do
}

void UploadDaemon::launch()
{
	std::string arguments = "\"" + traceDirectory + "\"";

	SHELLEXECUTEINFO shExecInfo;

	shExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);

	shExecInfo.fMask = NULL;
	shExecInfo.hwnd = NULL;
	shExecInfo.lpVerb = NULL;
	shExecInfo.lpFile = pathToExe.c_str();
	shExecInfo.lpParameters = arguments.c_str();
	shExecInfo.lpDirectory = NULL;
	shExecInfo.nShow = SW_NORMAL;
	shExecInfo.hInstApp = NULL;

	bool successful = ShellExecuteEx(&shExecInfo);
	if (!successful) {
		log->error("Failed to launch upload daemon " + pathToExe + ": " + WindowsUtils::getLastErrorAsString());
	}
}