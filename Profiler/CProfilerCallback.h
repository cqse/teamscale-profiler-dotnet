 /*
 * @ConQAT.Rating YELLOW Hash: F639FF2BDEB7841E508F10DE8286F37C
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
#include "MethodInfo.h"

using namespace std;

#define NAME_BUFFER_SIZE 2048
#define ARRAY_SIZE(s) (sizeof(s) / sizeof(s[0]))

/**
 * Coverage profiler class. Implements JIT event hooks to record method coverage.
 */
class CProfilerCallback : public CProfilerCallbackBase {
public:
	CProfilerCallback();
	virtual ~CProfilerCallback();

// Overwritten Profiling methods
	// STARTUP/SHUTDOWN EVENTS
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
	static UINT_PTR _stdcall FunctionMapper(FunctionID functionId,
						BOOL *pbHookFunction);
// End of FunctionIDMapper implementation

    int WriteToFile(const char* pszFmtString, ... ); // write an entry to the beacon file
	void WriteTupleToFile(const char* label, const char* value);
	
	HRESULT GetFunctionIdentifier( FunctionID functionID, MethodInfo* info);

private:
	// maps from assemblyIDs to assemblyNumbers
	int _assemblyCounter;
	map<int, int>* _assemblyMap;

	// Info object that keeps track of jitted methods
	vector<MethodInfo>* _jittedMethods;

	// Info object that keeps track of inlined methods
	set<FunctionID>* _inlinedMethods;
	vector<MethodInfo>* _inlinedMethodsList;

	// function to set up our event mask
	DWORD GetEventMask();

	// file into which results are written
	HANDLE _resultFile;

    DWORD m_dwEventMask; // the event mask used for this profiler
	CComQIPtr<ICorProfilerInfo> m_pICorProfilerInfo;	// smart pointer container for ICorProfilerInfo reference
    CComQIPtr<ICorProfilerInfo2> m_pICorProfilerInfo2;	// smart pointer container for ICorProfilerInfo2 reference
	CComQIPtr<ICorProfilerInfo3> m_pICorProfilerInfo3;

	TCHAR m_pszResultFile[_MAX_PATH]; // name of the result file

    wchar_t m_szAppPath[_MAX_PATH]; // path for the process we are in
    wchar_t m_szAppName[_MAX_FNAME]; // name of the file for the process we are in

	CRITICAL_SECTION m_prf_crit_sec; // SYNCHRONIZATION PRIMITIVE

	void CProfilerCallback::WriteProcessInfoToOutputFile();
	void CProfilerCallback::CreateOutputFile();
	void CProfilerCallback::WriteToLog(const char*, vector<MethodInfo>* list);

};
#endif