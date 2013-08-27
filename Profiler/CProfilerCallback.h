 /*
 * @ConQAT.Rating YELLOW Hash: 65079FDB2DAFB2A2145E9C6E948D7DB0
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

// TODO [NG]: I think we should avoid macros wherever possible and use a
//            'normal' constant here and make it part of the class.
#define NAME_BUFFER_SIZE 2048

// TODO [NG]: I think we should avoid macros wherever possible and implement
//            this as a function. I suspect the overhead for the additional
//            function calls can be tolerated.
#define ARRAY_SIZE(s) (sizeof(s) / sizeof(s[0]))

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

	// TODO [NG]: Why is this a pointer and not just a normal variable?
	// Maps from assemblyIDs to assemblyNumbers (determined by assemblyCounter).
	map<int, int>* assemblyMap;

	// Info object that keeps track of jitted methods.
	// TODO [NG]: Why is this a pointer and not just a normal variable?
	vector<FunctionInfo>* jittedMethods;

	// Info object that keeps track of inlined methods
	// TODO [NG]: Why do we need two collections?
	// TODO [NG]: Why are these pointers and not just a normal variables?
	set<FunctionID>* inlinedMethods;
	vector<FunctionInfo>* inlinedMethodsList;

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
