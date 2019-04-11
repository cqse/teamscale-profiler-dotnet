#pragma once
#include "CProfilerCallbackBase.h"
#include "FunctionInfo.h"
#include "log\TraceLog.h"
#include "log\AttachLog.h"
#include "config/Config.h"
#include "utils/WindowsUtils.h"
#include <atlbase.h>
#include <string>
#include <vector>
#include <map>
#include <set>
#include <mutex>
#include "UploadDaemon.h"

/**
 * Coverage profiler class. Implements JIT event hooks to record method
 * coverage.
 */
class CProfilerCallback : public CProfilerCallbackBase {
public:

	/** Returns the profiler, unless it was already destroyed or not yet constructed. */
	static CProfilerCallback* getInstance();

	/** Constructor. */
	CProfilerCallback();

	/** Destructor. */
	virtual ~CProfilerCallback();

	/** Initializer. Called at profiler startup. */
	STDMETHOD(Initialize)(IUnknown *pICorProfilerInfoUnk);

	/** Write coverage information to log file at shutdown. */
	STDMETHOD(Shutdown)();

	/** Store information about jitted method. */
	STDMETHOD(JITCompilationFinished)(FunctionID functionID, HRESULT hrStatus, BOOL fIsSafeToBlock);

	/** Write loaded assembly to log file. */
	STDMETHOD(AssemblyLoadFinished)(AssemblyID assemblyID, HRESULT hrStatus);

	/** Record inlining of method, but generally allow it. */
	STDMETHOD(JITInlining)(FunctionID callerID, FunctionID calleeID, BOOL *pfShouldInline);

private:
	static CProfilerCallback* instance;

	std::once_flag shutdownCompletedFlag;

	/** Synchronizes profiling callbacks. */
	CRITICAL_SECTION callbackSynchronization;

	/** Default size for arrays. */
	static const int BUFFER_SIZE = 2048;

	/** Counts the number of assemblies loaded. */
	int assemblyCounter = 1;

	Config config = Config(WindowsUtils::getConfigValueFromEnvironment);

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
	 * Keeps track of inlined methods.
	 * We use the vector to uniquely store the information about inlined methods.
	 */
	std::vector<FunctionInfo> inlinedMethods;

	/** Smart pointer to the .NET framework profiler info. */
	CComQIPtr<ICorProfilerInfo2> profilerInfo;

	/** The log to write all results and messages to. */
	TraceLog traceLog;

	/** The log to write attach and detatch events to */
	AttachLog attachLog;

	/**
	* Returns the event mask which tells the CLR which callbacks the profiler wants to subscribe
	* to. We enable JIT compilation and assembly loads for coverage profiling. In
	* addition if light mode is disabled, EnterLeave hooks are enabled to force re-jitting of pre-jitted
	* code, in order to make coverage information independent of pre-jitted code.
	*/
	DWORD getEventMask();

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
	static UINT_PTR _stdcall functionMapper(FunctionID functionId, BOOL *pbHookFunction) throw(...);

	/** Dumps all environment variables to the log file. */
	void dumpEnvironment();

	void initializeConfig();

	/** Returns a proxy for the upload daemon process */
	UploadDaemon createDaemon();

	/** Create method info object for a function id. */
	HRESULT getFunctionInfo(FunctionID functionID, FunctionInfo* info);

	/**  Store assembly counter for id. */
	int registerAssembly(AssemblyID assemblyId);

	/** Stores the assmebly name, path and metadata in the passed variables.*/
	void getAssemblyInfo(AssemblyID assemblyId, WCHAR* assemblyName, WCHAR *assemblyPath, ASSEMBLYMETADATA* moduleId);

	/** Triggers eagerly writing of function infos to log. */
	void recordFunctionInfo(std::vector<FunctionInfo>* list, FunctionID calleeId);

	/** Returns whether eager mode is enabled and amount of recorded method calls reached eagerness threshold. */
	bool shouldWriteEagerly();

	/** Write all information about the recorded functions to the log and clears the log. */
	void writeFunctionInfosToLog();

	/** Writes the fileVersionInfo into the provided buffer. */
	int writeFileVersionInfo(LPCWSTR moduleFileName, char* buffer, size_t bufferSize);

	/** Implements the actual shutdown procedure. Must only be called once. */
	void CProfilerCallback::ShutdownOnce();

	HRESULT JITCompilationFinishedImplementation(FunctionID functionID, HRESULT hrStatus, BOOL fIsSafeToBlock);
	HRESULT AssemblyLoadFinishedImplementation(AssemblyID assemblyID, HRESULT hrStatus);
	HRESULT JITInliningImplementation(FunctionID callerID, FunctionID calleeID, BOOL *pfShouldInline);
	HRESULT InitializeImplementation(IUnknown *pICorProfilerInfoUnk);

	/** Logs a stack trace. May rethrow the caught exception. */
	void handleException(std::string context);
};
