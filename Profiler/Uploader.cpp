#include "Uploader.h"
#include <windows.h>
#include <shellapi.h>
#include "WindowsUtils.cpp"

Uploader::Uploader(std::string uploaderPath, std::string traceDirectory, Log* log)
{
	this->pathToExe = uploaderPath + "\\uploader.exe";
	this->traceDirectory = traceDirectory;
	this->log = log;
}

Uploader::~Uploader()
{
	// nothing to do
}

void Uploader::launch()
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
		log->error("Failed to launch uploader " + pathToExe + ": " + WindowsUtils::getLastErrorAsString());
	}
}