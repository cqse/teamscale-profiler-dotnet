#include "TraceLog.h"
#include "version.h"
#include <vector>
#include <fstream>
#include <algorithm>
#include <winuser.h>
#include "utils/WindowsUtils.h"
#include <string>
#include <regex>
#include <sstream>

namespace Profiler {
	void TraceLog::writeJittedFunctionInfosToLog(const std::vector<FunctionInfo>& functions)
	{
		writeFunctionInfosToLog(LOG_KEY_JITTED, functions);
	}

	void TraceLog::writeInlinedFunctionInfosToLog(const std::vector<FunctionInfo>& functions)
	{
		writeFunctionInfosToLog(LOG_KEY_INLINED, functions);
	}

	void TraceLog::writeCalledFunctionInfosToLog(const std::vector<FunctionInfo>& functions)
	{
		writeFunctionInfosToLog(LOG_KEY_CALLED, functions);
	}

	void TraceLog::createLogFile(const std::string& targetDir) {
		std::string timeStamp = getFormattedCurrentTime();

		std::string fileName = "";
		fileName = fileName + "coverage_" + timeStamp + ".txt";

		FileLogBase::createLogFile(targetDir, fileName);

		writeTupleToFile(LOG_KEY_INFO, VERSION_DESCRIPTION);
		writeTupleToFile(LOG_KEY_STARTED, timeStamp);
	}

	void TraceLog::writeFunctionInfosToLog(const std::string& key, const std::vector<FunctionInfo>& functions) {
		std::stringstream stream;
		const std::string endLine = "\r\n";
		for (const FunctionInfo& function : functions) {
			stream << key << '=' << function.assemblyNumber << ':' << function.functionToken << endLine;
		}
		writeToFile(stream.str());
	}

	void TraceLog::info(const std::string& message) {
		writeTupleToFile(LOG_KEY_INFO, message);
	}

	void TraceLog::warn(const std::string& message)
	{
		writeTupleToFile(LOG_KEY_WARN, message);
	}

	void TraceLog::error(const std::string& message)
	{
		writeTupleToFile(LOG_KEY_ERROR, message);
	}

	void TraceLog::logEnvironmentVariable(const std::string& variable)
	{
		writeTupleToFile(LOG_KEY_ENVIRONMENT, variable);
	}

	void TraceLog::logProcess(const std::string& process)
	{
		writeTupleToFile(LOG_KEY_PROCESS, process);
	}

	void TraceLog::logAssembly(const std::wstring& assembly)
	{
		writeWideTupleToFile(LOG_KEY_ASSEMBLY, assembly);
	}

	void TraceLog::startTestCase(const std::string& testName)
	{
		// Line will look like this:
		// Test=Start:{Start Date}:{Testname}
		std::string testStartLine = "Start:" + getFormattedCurrentTime() + ":" + testName;
		writeTupleToFile(LOG_KEY_TESTCASE, testStartLine);
	}

	void TraceLog::endTestCase(const std::string& result, const std::string& duration)
	{
		// Line will look like this:
		// Test=End:{End Date}:{Result}:{Duration}
		std::string testEndLine = "End:" + getFormattedCurrentTime();
		if (!result.empty()) {
			testEndLine += ":" + result;
			testEndLine += ":" + duration;
		}

		writeTupleToFile(LOG_KEY_TESTCASE, testEndLine);
	}

	void TraceLog::shutdown() {
		std::string timeStamp = getFormattedCurrentTime();
		writeTupleToFile(LOG_KEY_STOPPED, timeStamp);

		writeTupleToFile(LOG_KEY_INFO, "Shutting down coverage profiler");

		FileLogBase::shutdown();
	}

}

