 /*
 * @ConQAT.Rating YELLOW Hash: A77344601990103C6B9F2C80BB947596
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

// Default size for arrays containing names. 
const int nameBufferSize = 2048;

/**
 * Coverage profiler class. Implements JIT event hooks to record method
 * coverage.
 */
class CProfilerCallback : public CProfilerCallbackBase {
public:
	// Constructor.
	CProfilerCallback();
	// Destructor.
	virtual ~CProfilerCallback();

// Overwritten Profiling methods
	// Startup/shutdown events
    STDMETHOD(Initialize)(IUnknown *pICorProfilerInfoUnk);
    STDMETHOD(Shutdown)();

	// JIT method to capture JIT events
	STDMETHOD(JITCompilationFinished)(FunctionID functionID, HRESULT hrStatus, BOOL fIsSafeToBlock);

	// Assembly load hook to capture assembly load events
	STDMETHOD(AssemblyLoadFinished)(AssemblyID assemblyID, HRESULT hrStatus);

	// Inlining hook to keep track of inlined methods
	STDMETHOD(JITInlining)(FunctionID callerID, FunctionID calleeID, BOOL *pfShouldInline);
// End of overwritten profiling methods


// FunctionIDMapper implementation
	// Defined if a given function should be registered to receice a callback every time it is executed.
	static UINT_PTR _stdcall FunctionMapper(FunctionID functionId,
						BOOL *pbHookFunction);
// End of FunctionIDMapper implementation

	// Writes the given string to the output file.
    int WriteToFile(const char* pszFmtString, ... ); // write an entry to the beacon file
    
	// Writes the given key and value to the output file.
	void WriteTupleToFile(const char* key, const char* value);
	
	// Retrieves the FunctionInfo for the function with the given ID.
	HRESULT GetFunctionInfo( FunctionID functionID, FunctionInfo* info);

private:
	// Count the assemblies loaded.
	int assemblyCounter;

	// Maps from assemblyIDs to assemblyNumbers (determined by assemblyCounter).
	// It is used to identify the declaring assembly for functions.
	map<int, int> assemblyMap;

	// Info object that keeps track of jitted methods.
	// We use a pointer because this collection may become large.
	vector<FunctionInfo> *jittedMethods;

	// Collecions that keep track of inlined methods.
	// We use the set to efficiently determine if we already noticed an inlined method and 
	// the vector to uniquely store the information about inlined methods. Using one collection, 
	// e.g. a hash_map would force us to implement additional functions to write the infos to the output file.
	// We use pointers because these collections may become large.
	set<FunctionID>* inlinedMethodIds;
	vector<FunctionInfo> *inlinedMethods;

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
	SYSTEMTIME CProfilerCallback::GetTime();
};
#endif
