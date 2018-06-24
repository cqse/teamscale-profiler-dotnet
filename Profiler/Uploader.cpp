#include "Uploader.h"
#include "windows.h"
#include "shellapi.h"
#include "WindowsUtils.h"

Uploader::Uploader(std::string uploaderPath, std::string traceDirectory, Log* log)
{
	this->pathToExe = uploaderPath + "\\uploader.exe";
	this->traceDirectory = traceDirectory;
	this->log = log;
}

void Uploader::launch()
{
	SHELLEXECUTEINFO shExecInfo;

	shExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);

	shExecInfo.fMask = NULL;
	shExecInfo.hwnd = NULL;
	shExecInfo.lpVerb = NULL;
	shExecInfo.lpFile = this->pathToExe.c_str();
	shExecInfo.lpParameters = this->traceDirectory.c_str();
	shExecInfo.lpDirectory = NULL;
	shExecInfo.nShow = SW_NORMAL;
	shExecInfo.hInstApp = NULL;

	bool successful = ShellExecuteEx(&shExecInfo);
	if (!successful) {
		log->error("Failed to launch uploader " + this->pathToExe + ": " + GetLastErrorAsString());
	}
}
