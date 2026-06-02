#pragma once
#include "FunctionInfo.h"
#include "FileLogBase.h"
#include <atlbase.h>
#include <string>
#include <vector>
#include <map>
#include <set>


namespace Profiler {
	/**
	 * Manages a log file on the file system to which both diagnostic messages and trace information is written.
	 * Unless mentioned otherwise, all methods in this class are thread-safe and perform their own synchronization.
	 */
	class TraceLog : public FileLogBase
	{
	public:
		~TraceLog() override = default;

		/** Write all information about the given jitted functions to the log. */
		void writeJittedFunctionInfosToLog(const std::vector<FunctionInfo>& functions);

		/** Write all information about the given inlined functions to the log. */
		void writeInlinedFunctionInfosToLog(const std::vector<FunctionInfo>& functions);

		/** Write all information about the given called functions to the log. */
		void writeCalledFunctionInfosToLog(const std::vector<FunctionInfo>& functions);

		/**
		 * Create the log file and add general information.
		 * Can be called as an alternative for createLogFile method of the base class as first method called on the object.
		 * This method is not thread-safe or reentrant.
		 */
		void createLogFile(const std::string& targetDir);

		/** Writes a closing log entry to the file and closes the log file. Further calls to logging methods will be ignored. */
		void shutdown();

		/** Writes an info message. */
		void info(const std::string& message);

		/** Writes a warning message. */
		void warn(const std::string& message);

		/** Writes an error message. */
		void error(const std::string& message);

		/** Log a single environment variable definition. */
		void logEnvironmentVariable(const std::string& variable);

		/** Writes the profiled process into the log. Should only be called once. */
		void logProcess(const std::string& process);

		/** Writes info about a profiled assembly into the log. Should only be called once. */
		void logAssembly(const std::wstring& assembly);

		void startTestCase(const std::string& testName);

		void endTestCase(const std::string& result = "", const std::string& duration = "");

	protected:
		/** The key to log information about the profiler startup. */
		const std::string LOG_KEY_STARTED = "Started";

		/** The key to log information about the profiler shutdown. */
		const std::string LOG_KEY_STOPPED = "Stopped";

		/** The key to log information about inlined methods. */
		const std::string LOG_KEY_INLINED = "Inlined";

		/** The key to log information about jitted methods. */
		const std::string LOG_KEY_JITTED = "Jitted";

		/** The key to log information about called methods. */
		const std::string LOG_KEY_CALLED = "Called";

		/** The key to log information about test cases. */
		const std::string LOG_KEY_TESTCASE = "Test";

		/** The key to log information useful when interpreting the traces. */
		const std::string LOG_KEY_INFO = "Info";

		/** The key to log information about non-critical problems. */
		const std::string LOG_KEY_WARN = "Warn";

		/** The key to log information about errors that should be addressed but don't prevent the profiler from tracing method calls. */
		const std::string LOG_KEY_ERROR = "Error";

		/** The key to log information about a single assembly. */
		const std::wstring LOG_KEY_ASSEMBLY = L"Assembly";

		/** The key to log information about the profiled process. */
		const std::string LOG_KEY_PROCESS = "Process";

		/** The key to log information about the environment variables the profiled process sees. */
		const std::string LOG_KEY_ENVIRONMENT = "Environment";

	private:
		/** Write all information about the given functions to the log. */
		void writeFunctionInfosToLog(const std::string& key, const std::vector<FunctionInfo>& functions);
	};
}
