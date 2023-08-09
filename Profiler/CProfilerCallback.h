#pragma once
#include "CProfilerCallbackBase.h"
#include "FunctionInfo.h"
#include "log/TraceLog.h"
#include "log/AttachLog.h"
#include "config/Config.h"
#include "utils/WindowsUtils.h"
#include <atlbase.h>
#include <string>
#include <vector>
#include <map>
#include <utils/UIntSet/UIntSet.h>
#include "CProfilerWorker.h"
#include "UploadDaemon.h"
#include "utils/Ipc.h"
#include "instrumentation/CanonicalNames.h"
#include <corhlpr.h>

/**
 * Coverage profiler class. Implements JIT event hooks to record method
 * coverage.
 */
class CProfilerCallback : public CProfilerCallbackBase {
public:

	/** Shuts down the profiler from the DllMain function on Dll detach if it is still running. */
	static void ShutdownFromDllMainDetach();

	/** Constructor. */
	CProfilerCallback();

	/** Destructor. */
	virtual ~CProfilerCallback();

	/** Initializer. Called at profiler startup. */
	STDMETHOD(Initialize)(IUnknown* pICorProfilerInfoUnk);

	/** Write coverage information to log file at shutdown. */
	STDMETHOD(Shutdown)();

	bool checkAlreadyInstrumented(BYTE* code, UINT64 function);

	STDMETHOD(JITCompilationStarted)(FunctionID functionId, BOOL fIsSafeToBlock);

	const HRESULT instrumentation(FunctionID functionId);

	/** Write loaded assembly to log file. */
	STDMETHOD(AssemblyLoadFinished)(AssemblyID assemblyID, HRESULT hrStatus);

	/**
	 * Implements the actual shutdown procedure. Must only be called once.
	 * If clrIsAvailable is true, also tries to force a GC.
	 * Note that forcing a GC after the CLR has shut down can result in deadlocks so this
	 * should be set only when calling from a CLR callback.
	 */
	void CProfilerCallback::ShutdownOnce(bool clrIsAvailable);

private:
	/** Synchronizes profiling callbacks. */
	CRITICAL_SECTION callbackSynchronization;

	CRITICAL_SECTION methodSetSynchronization;

	/** Default size for arrays. */
	static const int BUFFER_SIZE = 2048;

	/** Counts the number of assemblies loaded. */
	int assemblyCounter = 1;

	bool isTestCaseRecording = false;
	CProfilerCallback* callbackInstance = NULL;

	Config config = Config(WindowsUtils::getConfigValueFromEnvironment);

	/**
	 * Maps from assembly IDs to assemblyNumbers (determined by assemblyCounter).
	 * It is used to identify the declaring assembly for functions.
	 */
	std::map<AssemblyID, int> assemblyMap;

	/** Smart pointer to the .NET framework profiler info. */
	CComQIPtr<ICorProfilerInfo8> profilerInfo;

	/** The log to write all results and messages to. */
	TraceLog traceLog;

	/** The log to write attach and detatch events to */
	AttachLog attachLog;

	/** Inter-process connection for TIA communication. null if not in TIA mode. */
	Ipc* ipc = NULL;

	/** Callback that is being called when a testcase starts. */
	void onTestStart(std::string testName);

	/** Callback that is being called when a testcase ends. */
	void onTestEnd(std::string result = "", std::string message = "");

	/**
	 * Keeps track of called methods.
	 * We use the set to efficiently determine if we already noticed an called method.
	 */
	UIntSet calledMethodIds;

	CProfilerWorker* worker = NULL;

	/**
	 * Keeps track of called methods.
	 * We use the vector to uniquely store the information about called methods.
	 */
	std::vector<FunctionInfo> calledMethods;
private:

	/**
	* Returns the event mask which tells the CLR which callbacks the profiler wants to subscribe
	* to. We enable JIT compilation and assembly loads for coverage profiling. In
	* addition if light mode is disabled, EnterLeave hooks are enabled to force re-jitting of pre-jitted
	* code, in order to make coverage information independent of pre-jitted code.
	*/
	void adjustEventMask();

	/** Dumps all environment variables to the log file. */
	void dumpEnvironment();

	void initializeConfig();

	/** Returns a proxy for the upload daemon process */
	UploadDaemon createDaemon();

	/**  Store assembly counter for id. */
	int registerAssembly(AssemblyID assemblyId);

	/** Stores the assmebly name, path and metadata in the passed variables.*/
	void getAssemblyInfo(AssemblyID assemblyId, WCHAR* assemblyName, WCHAR* assemblyPath, ASSEMBLYMETADATA* moduleId);


	/** Write all information about the recorded functions to the log and clears the log. */
	void writeFunctionInfosToLog();

	/** Writes the fileVersionInfo into the provided buffer. */
	int writeFileVersionInfo(LPCWSTR moduleFileName, char* buffer, size_t bufferSize);

	HRESULT AssemblyLoadFinishedImplementation(AssemblyID assemblyID, HRESULT hrStatus);
	HRESULT InitializeImplementation(IUnknown* pICorProfilerInfoUnk);

	/**
	* Fix SEH header offsets. These need to be adjusted because we add additional bytes to the method which also
	* moves around the exception hanlding sections.
	*/
	bool fixSehHeaders(COR_ILMETHOD_FAT* newFatImage, int extraSize);

	/**
	* Add the code we need to record coveage to the start of a method.
	*/
	void addCustomCode(BYTE*& newCode, const UINT64 functionId, const ModuleID& moduleId);

	/** Logs a stack trace. May rethrow the caught exception. */
	void handleException(std::string context);
};
