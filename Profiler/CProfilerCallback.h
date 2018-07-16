#ifndef _ProfilerCallback_H_
#define _ProfilerCallback_H_

#include "FunctionInfo.h"
#include "CProfilerCallbackBase.h"
#include <atlbase.h>
#include <string>
#include <vector>
#include <map>
#include <set>

/**
 * Coverage profiler class. Implements JIT event hooks to record method
 * coverage.
 */
class CProfilerCallback : public CProfilerCallbackBase {
public:

	/** Constructor. */
	CProfilerCallback();

	/** Destructor. */
	virtual ~CProfilerCallback();

	/** Initializer. Called at profiler startup. */
	STDMETHOD(Initialize)(IUnknown *pICorProfilerInfoUnk);

	/** Return the value for the environment variable COR_PROFILER_<suffix> or the empty string if it is not set. */
	std::string getConfigValueFromEnvironment(std::string suffix);

	/** Reads all options from the config file into memory. */
	void readConfig();

	/**
	* Reads all the config option from 1) the environment variable COR_PROFILER_<optionName> and then 2) the config file if the corresponding environment variable is not set.
	* If the option is declared in neither location, returns the empty string.
	*/
	std::string getOption(std::string optionName);

	/** Write coverage information to log file at shutdown. */
	STDMETHOD(Shutdown)();

	/** Store information about jitted method. */
	STDMETHOD(JITCompilationFinished)(FunctionID functionID, HRESULT hrStatus, BOOL fIsSafeToBlock);

	/** Write loaded assembly to log file. */
	STDMETHOD(AssemblyLoadFinished)(AssemblyID assemblyID, HRESULT hrStatus);

	/** Record inlining of method, but generally allow it. */
	STDMETHOD(JITInlining)(FunctionID callerID, FunctionID calleeID, BOOL *pfShouldInline);

	/**
	 * Defines whether the given function should trigger a callback everytime it is executed.
	 * 
	 * We do not register a single function callback in order to not affect
	 * performance. In effect, we disable this feature here. It has been tested via
	 * a performance benchmark, that this implementation does not impact call
	 * performance.
	 *
	 * For coverage profiling, we do not need this callback. However, the event is
	 * enabled in the event mask in order to force JIT-events for each first call to
	 * a function, independent of whether a pre-jitted version exists.)
	 */
	static UINT_PTR _stdcall functionMapper(FunctionID functionId,
						BOOL *pbHookFunction);

	/** Create method info object for a function id. */
	HRESULT getFunctionInfo( FunctionID functionID, FunctionInfo* info);

private:
	/** Default size for arrays. */
	static const int BUFFER_SIZE = 2048;

	/** Counts the number of assemblies loaded. */
	int assemblyCounter = 1;

	/** Whether to run in light mode or force re-jitting of pre-jitted methods. */
	bool isLightMode = false;

	/** 
	 * Whether to run in eager mode and write a batch of recorded invocations to the trace 
	 * file instead of waiting until shutdown. If 0, everything is written on shutdown,
	 * otherwise the sepcified amount of method calls is recorded and written thereafter.
	 */
	size_t eagerness = 0;

	/** Whether the current process should be profiled. */
	bool isProfilingEnabled = false;

	/**
	 * Maps from assembly IDs to assemblyNumbers (determined by assemblyCounter).
	 * It is used to identify the declaring assembly for functions.
	 */
	std::map<AssemblyID, int> assemblyMap;

	/**
	 * Info object that keeps track of jitted methods.
	 */
	std::vector<FunctionInfo> jittedMethods;

	/**
	 * Keeps track of inlined methods.
	 * We use the set to efficiently determine if we already noticed an inlined method.
	 */
	std::set<FunctionID> inlinedMethodIds;

	/**
	 * Collecions that keep track of inlined methods.
	 * We use the vector to uniquely store the information about inlined methods.
	 */
	std::vector<FunctionInfo> inlinedMethods;

	/**
	* Stores all declared options from the config file.
	*/
	std::map<std::string, std::string> configOptions;

	/** File into which results are written. INVALID_HANDLE if the file has not been opened yet. */
	HANDLE logFile = INVALID_HANDLE_VALUE;
	
	/** Smart pointer to the .NET framework profiler info. */
	CComQIPtr<ICorProfilerInfo2> profilerInfo;	

	/** Path of the result file. */
	TCHAR logFilePath[_MAX_PATH];

	/** Path of the process we are in. */
	wchar_t appPath[_MAX_PATH]; 
   
	/** Name of the profiled application. */
	wchar_t appName[_MAX_FNAME]; 
	
	/** Synchronizes access to the result file. */
	CRITICAL_SECTION criticalSection;

	/**
	* Returns the event mask which tells the CLR which callbacks the profiler wants to subscribe
	* to. We enable JIT compilation and assembly loads for coverage profiling. In
	* addition if light mode is disabled, EnterLeave hooks are enabled to force re-jitting of pre-jitted
	* code, in order to make coverage information independent of pre-jitted code.
	*/
	DWORD getEventMask();

	/** Returns information about the profiled process. */
	std::string getProcessInfo(); 

	/** Create the log file and add general information. */
	void createLogFile();

	/**  Store assembly counter for id. */
	int registerAssembly(AssemblyID assemblyId);

	/** Stores the assmebly name, path and metadata in the passed variables.*/
	void getAssemblyInfo(AssemblyID assemblyId, WCHAR* assemblyName, WCHAR *assemblyPath, ASSEMBLYMETADATA* moduleId);

	/** Triggers eagerly writing of function infos to log. */
	void recordFunctionInfo(std::vector<FunctionInfo>* list, const char* key, FunctionID calleeId);

	/** Returns whether eager mode is enabled and amount of recorded method calls reached eagerness threshold. */
	bool shouldWriteEagerly();

	/** Writes the given string to the log file. */
	int writeToFile(const char* string);

	/** Writes the given name-value pair to the log file. */
	void writeTupleToFile(const char* key, std::string value);

	/** Writes the given name-value pair to the log file. */
	void writeTupleToFile(const char* key, const char* value);

	/** Writes the fileVersionInfo into the provided buffer. */
	int writeFileVersionInfo(LPCWSTR moduleFileName, char* buffer, size_t bufferSize);

	/** Write all information about the recorded functions to the log and clears the log. */
	void writeFunctionInfosToLog();

	/** Write all information about the given functions to the log and clears the log. */
	void writeFunctionInfosToLog(const char* key, std::vector<FunctionInfo>* functions);

	/** Write all information about the given function to the log. */
	void writeSingleFunctionInfoToLog(const char* key, FunctionInfo& info);

	/** Fills the given function info for the function represented by the given IDs and tokens. */
	void fillFunctionInfo(FunctionInfo* info, FunctionID functionId, mdToken functionToken, ModuleID moduleId);

	/** Fills the given buffer with a string representing the current time. */
	void getFormattedCurrentTime(char *result, size_t size);

};

#endif
