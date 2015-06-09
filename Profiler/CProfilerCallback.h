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

using namespace std;

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

// Overwritten Profiling methods

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

// End of overwritten profiling methods

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
	static const int bufferSize = 2048;

	/** Counts the number of assemblies loaded. */
	int assemblyCounter;

	/** Whether to run in light mode or force re-jitting of pre-jitted methods. */
	bool isLightMode;

	// Maps from assemblyIDs to assemblyNumbers (determined by assemblyCounter).
	// It is used to identify the declaring assembly for functions.
	map<AssemblyID, int> assemblyMap;

	// Info object that keeps track of jitted methods.
	// We use a pointer because this collection may become large.
	vector<FunctionInfo> jittedMethods;

	// Collecions that keep track of inlined methods.
	// We use the set to efficiently determine if we already noticed an inlined method and 
	// the vector to uniquely store the information about inlined methods. Using one collection, 
	// e.g. a hash_map would force us to implement additional functions to write the infos to the output file.
	// We use pointers because these collections may become large.
	set<FunctionID> inlinedMethodIds;
	vector<FunctionInfo> inlinedMethods;

	// Function to set up our event mask.
	DWORD GetEventMask();

	// File into which results are written.
	HANDLE resultFile;
	
	// Smart pointer container for ICorProfilerInfo reference.
	CComQIPtr<ICorProfilerInfo> pICorProfilerInfo;	
	
	// Smart pointer container for ICorProfilerInfo2 reference.
	CComQIPtr<ICorProfilerInfo2> pICorProfilerInfo2;	

	// Name of the result file.
	TCHAR pszResultFile[_MAX_PATH];

	// Path for the process we are in.
	wchar_t szAppPath[_MAX_PATH]; 
   
	// Name of the file for the process we are in.
	wchar_t szAppName[_MAX_FNAME]; 
	
	// Synchronization primitive
	CRITICAL_SECTION criticalSection; 
	
	// Writes info about the process to the output file.
	void WriteProcessInfoToOutputFile(); 
	
	// Creates the output file.
	void CreateOutputFile(); 
	
	// Writes information about the called functions to the output file.
	void WriteToLog(const char* key, vector<FunctionInfo>* functions); 

	// Return the current time.
	void GetFormattedTime(char *result, size_t size);
};
#endif
