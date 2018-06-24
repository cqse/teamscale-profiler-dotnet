#include "Uploader.h"
#include "windows.h"
#include "shellapi.h"

Uploader::Uploader(std::string uploaderPath, std::string traceDirectory)
{
	this->pathToExe = uploaderPath + "\\uploader.exe";
	this->traceDirectory = traceDirectory;
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

	ShellExecuteEx(&shExecInfo);
	// ToDO handle errors
}
