#include "TraceLog.h"
#include "version.h"
#include <vector>
#include <fstream>
#include <algorithm>
#include <winuser.h>
#include "utils/WindowsUtils.h"
#include <string>


void TraceLog::writeJittedFunctionInfosToLog(std::vector<FunctionInfo>* functions)
{
	writeFunctionInfosToLog(LOG_KEY_JITTED, functions);
}

void TraceLog::writeInlinedFunctionInfosToLog(std::vector<FunctionInfo>* functions)
{
	writeFunctionInfosToLog(LOG_KEY_INLINED, functions);
}

void TraceLog::createLogFile(Config& config) {
	std::string targetDir = config.getTargetDir();

	char timeStamp[BUFFER_SIZE];
	getFormattedCurrentTime(timeStamp, sizeof(timeStamp));

	std::string fileName = "";
	fileName = fileName + "coverage_" + timeStamp + ".txt";

	FileLogBase::createLogFile(targetDir, fileName, true);

	writeTupleToFile(LOG_KEY_INFO, VERSION_DESCRIPTION);
	writeTupleToFile(LOG_KEY_STARTED, timeStamp);
}

void TraceLog::writeFunctionInfosToLog(const char* key, std::vector<FunctionInfo>* functions) {
	for (std::vector<FunctionInfo>::iterator i = functions->begin(); i != functions->end(); i++) {
		writeSingleFunctionInfoToLog(key, *i);
	}
}

void TraceLog::writeSingleFunctionInfoToLog(const char* key, FunctionInfo& info) {
	char signature[BUFFER_SIZE];
	signature[0] = '\0';
	sprintf_s(signature, "%i:%i", info.assemblyNumber,
		info.functionToken);
	writeTupleToFile(key, signature);
}

void TraceLog::shutdown() {
	char timeStamp[BUFFER_SIZE];
	getFormattedCurrentTime(timeStamp, sizeof(timeStamp));
	writeTupleToFile(LOG_KEY_STOPPED, timeStamp);

	writeTupleToFile(LOG_KEY_INFO, "Shutting down coverage profiler");

	FileLogBase::shutdown();
}