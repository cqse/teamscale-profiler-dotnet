 /*
 * @ConQAT.Rating GREEN Hash: 59D81771404CD2E5405557F5588FE237
 */

#ifndef _ProfilerCallback_H_
#define _ProfilerCallback_H_

#include <cor.h>
#include <corprof.h>
#include <atlbase.h>
#include <string>
#include <vector>
#include <map>
#include <set>
#include "CProfilerCallbackBase.h"
#include "FunctionInfo.h"

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
	static UINT_PTR _stdcall FunctionMapper(FunctionID functionId,
						BOOL *pbHookFunction);

	/** Writes the given string to the log file. */
	int WriteToFile(const char* string); 

	/** Writes the given name-value pair to the log file. */
	void WriteTupleToFile(const char* key, const char* value);

	/** Create method info object for a function id. */
	HRESULT GetFunctionInfo( FunctionID functionID, FunctionInfo* info);

private:
	/** Default size for arrays. */
	static const int BUFFER_SIZE = 2048;

	/** Counts the number of assemblies loaded. */
	int assemblyCounter;

	/** Whether to run in light mode or force re-jitting of pre-jitted methods. */
	bool isLightMode;

	/**
	 * Maps from assembly IDs to assemblyNumbers (determined by assemblyCounter).
	 * It is used to identify the declaring assembly for functions.
	 */
	map<AssemblyID, int> assemblyMap;

	/**
	 * Info object that keeps track of jitted methods.
	 */
	vector<FunctionInfo> jittedMethods;

	/**
	 * Keeps track of inlined methods.
	 * We use the set to efficiently determine if we already noticed an inlined method.
	 */
	set<FunctionID> inlinedMethodIds;

	/**
	 * Collecions that keep track of inlined methods.
	 * We use the vector to uniquely store the information about inlined methods.
	 */
	vector<FunctionInfo> inlinedMethods;

	/**
	 * Returns the event mask which tells the CLR which callbacks the profiler wants to subscribe
	 * to. We enable JIT compilation and assembly loads for coverage profiling. In
	 * addition if light mode is disabled, EnterLeave hooks are enabled to force re-jitting of pre-jitted
	 * code, in order to make coverage information independent of pre-jitted code.
	 */
	DWORD GetEventMask();

	/** File into which results are written. INVALID_HANDLE if the file has not been opened yet. */
	HANDLE resultFile;
	
	/** Smart pointer container for ICorProfilerInfo2 reference. */
	CComQIPtr<ICorProfilerInfo2> pICorProfilerInfo2;	

	/** Path of the result file. */
	TCHAR pszResultFile[_MAX_PATH];

	/** Path of the process we are in. */
	wchar_t szAppPath[_MAX_PATH]; 
   
	/** Name of the profiled application. */
	wchar_t szAppName[_MAX_FNAME]; 
	
	/** Synchronizes access to the result file. */
	CRITICAL_SECTION criticalSection; 

	/**
	 * Writes information about the profiled process to the
	 * output file.
	 */
	void WriteProcessInfoToOutputFile(); 

	/** Create the output file and add general information. */
	void CreateOutputFile();

	/** Write a information about the given functions to the log. */
	void WriteToLog(const char* key, vector<FunctionInfo>* functions);

	/** Fills the given function info for the function represented by the given IDs and tokens. */
	void FillFunctionInfo(FunctionInfo* info, FunctionID functionId, mdToken functionToken, ModuleID moduleId, mdTypeDef classToken);

	/** Fills the given buffer with a string representing the current time. */
	void GetFormattedTime(char *result, size_t size);
};
#endif
