 /*
 * @ConQAT.Rating YELLOW Hash: 5BC43D3204326C64782428D6C56505E9
 */

#include <windows.h>
#include <stdio.h>
#include "CProfilerCallback.h"
#include <winuser.h>

#pragma intrinsic(strcmp,labs,strcpy,_rotl,memcmp,strlen,_rotr,memcpy,_lrotl,_strset,memset,_lrotr,abs,strcat)

// Constants used for report generation
// TODO [NG]: Is this really needed? If yes, use a normal constant?
#ifdef _WIN64
#define HEADER "Coverage profiler version 0.9.1.2 (x64)"
#endif
#ifndef _WIN64
#define HEADER "Coverage profiler version 0.9.1.2 (x86)"
#endif

// TODO [NG]: Use "normal" constants for the following macros?
#define INFO "Info"
#define ASSEMBLY "Assembly"
#define PROCESS "Process"
#define INLINED "Inlined"
#define JITTED "Jitted"
#define STARTED "Started"
#define STOPPED "Stopped"

// TODO [NG]: Use better variable name.
// TODO [NG]: Why is this declared here? It is used only in the method
//            WriteToFile.
CHAR g_szBuffer[4096];

/** Constructor. */
CProfilerCallback::CProfilerCallback() : m_dwEventMask(0), _resultFile(INVALID_HANDLE_VALUE) {
		// Make a critical section for synchronization.
		InitializeCriticalSection(&m_prf_crit_sec);
}

/** Destructor. */
CProfilerCallback::~CProfilerCallback() {
	// Clean up the critical section.
	DeleteCriticalSection(&m_prf_crit_sec);
}

/** Initializer. Called at profiler startup. */
// TODO [NG]: Chose better name for the parameter?
HRESULT CProfilerCallback::Initialize(IUnknown * pICorProfilerInfoUnk ) {
	CreateOutputFile();

	// Initialize data structures.
	_assemblyCounter = 1;
	_assemblyMap = new map<int, int>;
	_jittedMethods = new vector<FunctionInfo>;
	_inlinedMethods = new set<FunctionID>;
	_inlinedMethodsList = new vector<FunctionInfo>;

	// Get reference to the ICorProfilerInfo interface 
	HRESULT hr =
		pICorProfilerInfoUnk->QueryInterface( IID_ICorProfilerInfo, (LPVOID *)&m_pICorProfilerInfo );
	if ( FAILED(hr) ) {
		return E_INVALIDARG;
	}

	hr = pICorProfilerInfoUnk->QueryInterface( IID_ICorProfilerInfo2, (LPVOID *)&m_pICorProfilerInfo2 );

	if ( FAILED(hr) ) {
		// We still want to work if this call fails, might be an older .NET
		// version than VS2005.
		OutputDebugString("Pre .NET 2 version detected");
		m_pICorProfilerInfo2.p = NULL;
	}

	// Indicate which events we're interested in.
	m_dwEventMask = GetEventMask();

	// Set the event mask for the interfaces of .NET 1 and .NET 2. We currently
	// do not need the features of the profiling interface beyond .NET 2.
	if (m_pICorProfilerInfo2.p == NULL) {
		// Pre .NET 2.
		m_pICorProfilerInfo->SetEventMask(m_dwEventMask);
		// Enable function mapping.
		m_pICorProfilerInfo->SetFunctionIDMapper(FunctionMapper);
	} else {
		// .NET 2 and beyond.
		m_pICorProfilerInfo2->SetEventMask(m_dwEventMask);
		// Enable function mapping.
		m_pICorProfilerInfo2->SetFunctionIDMapper(FunctionMapper);
	}
	WriteProcessInfoToOutputFile();
	return S_OK;
}

/**
 * Writes information about the profiled process (path to executable) to the
 * output file.
 */
void CProfilerCallback::WriteProcessInfoToOutputFile(){
	// Get the name of the executing process and write to log.
	// TODO [NG]: Why '0x00' and not simply '0'?
	m_szAppPath[0] = 0x00;
	m_szAppName[0] = 0x00;
	if (0 == GetModuleFileNameW(NULL, m_szAppPath, MAX_PATH)) {
		_wsplitpath_s(m_szAppPath, NULL, 0, NULL, 0, m_szAppName, _MAX_FNAME, NULL, 0);
	}
	if (m_szAppPath[0] == 0x00) { // TODO [NG]: Why '0x00' and not simply '0'?
		wcscpy_s(m_szAppPath, MAX_PATH, L"No Application Path Found");
		wcscpy_s(m_szAppName, _MAX_FNAME, L"No Application Name Found");
	}

	// TODO [NG]: What are the following two lines for?
	char process[NAME_BUFFER_SIZE];
	sprintf_s(process, "%S", m_szAppPath);
	WriteTupleToFile(PROCESS, process);
}

/** Create the output file and add general information. */
void CProfilerCallback::CreateOutputFile() {
	// Read target directory from environment variable.
	char targetDir[1000];
	if (!GetEnvironmentVariable("COR_PROFILER_TARGETDIR", targetDir,
			sizeof(targetDir))) {
		sprintf_s(targetDir, "c:/profiler/");
	}
	// TODO [NG]: Getting the system time and formatting it should be put in its
	//            own method.
	SYSTEMTIME time;
	GetSystemTime(&time);

	// Create target file.
	// TODO [NG]: Why not use NAME_BUFFER_SIZE here?
	char targetFilename[1000];
	sprintf_s(targetFilename, "%s/coverage_%04d%02d%02d_%02d%02d%02d%04d.txt",
			targetDir, time.wYear, time.wMonth, time.wDay, time.wHour,
			time.wMinute, time.wSecond, time.wMilliseconds);
	_tcscpy_s(m_pszResultFile, targetFilename);

	EnterCriticalSection(&m_prf_crit_sec);
	_resultFile = CreateFile(m_pszResultFile, GENERIC_WRITE, FILE_SHARE_READ,
			NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	WriteTupleToFile(INFO, HEADER);

	char timeStamp[NAME_BUFFER_SIZE];
	sprintf_s(timeStamp, "%04d%02d%02d_%02d%02d%02d%04d", time.wYear,
			time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond,
			time.wMilliseconds);
	WriteTupleToFile(STARTED, timeStamp);
	LeaveCriticalSection(&m_prf_crit_sec);
}

/** Write coverage information to log file at shutdown. */
HRESULT CProfilerCallback::Shutdown() {
	// TODO [NG]: Getting the system time and formatting it should be put in its
	//            own method.
	// Get timestamp.
	SYSTEMTIME time;
	GetSystemTime (&time);

	// Write inlined methods.
	WriteToFile("//%i methods inlined\r\n", _inlinedMethodsList->size());
	WriteToLog(INLINED, _inlinedMethodsList);

	// Write jitted methods.
	WriteToFile("//%i methods jitted\r\n", _jittedMethods->size());
	WriteToLog(JITTED, _jittedMethods);

	// Write timestamp.
	char timeStamp[NAME_BUFFER_SIZE];
	sprintf_s(timeStamp, "%04d%02d%02d_%02d%02d%02d%04d", time.wYear, time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond, time.wMilliseconds);
	WriteTupleToFile(STOPPED, timeStamp);

	WriteTupleToFile(INFO, "Shutting down coverage profiler" );

	// Cleanup.
	m_pICorProfilerInfo2->ForceGC();

	// Close the log file.
	EnterCriticalSection(&m_prf_crit_sec);
	if(_resultFile != INVALID_HANDLE_VALUE) {
		CloseHandle(_resultFile);
	}
	LeaveCriticalSection(&m_prf_crit_sec);

	delete _assemblyMap;
	delete _jittedMethods;
	delete _inlinedMethods;
	delete _inlinedMethodsList;

	return S_OK;
}

/**
 * The event mask tells the CLR which callbacks the profiler wants to subscribe
 * to. We enable JIT compilation and assembly loads for coverage profiling. In
 * addition, EnterLeave hooks are enabled to force re-jitting of pre-jitted
 * code, in order to make coverage information independent of pre-jitted code.
 */
DWORD CProfilerCallback::GetEventMask() {
	m_dwEventMask = 0;
	m_dwEventMask |= COR_PRF_MONITOR_JIT_COMPILATION;
	m_dwEventMask |= COR_PRF_MONITOR_ASSEMBLY_LOADS;
	m_dwEventMask |= COR_PRF_MONITOR_ENTERLEAVE;

	return m_dwEventMask;
}

/**
 * We do not register a single function callback in order to not affect
 * performance. In effect, we disable this feature here. It has been tested via
 * a performance benchmark, that this implementation does not impact call
 * performance.
 *
 * For coverage profiling, we do not need this callback. However, the event is
 * enabled in the event mask in order to force JIT-events for each first call to
 * a function, independent of whether a pre-jitted version exists.)
 */
UINT_PTR CProfilerCallback::FunctionMapper(FunctionID functionId,
		BOOL *pbHookFunction) {
	// Disable hooking of functions.
	*pbHookFunction = false;

	// Always return original function id.
	return functionId;
}

/** Store information about jitted method. */
HRESULT CProfilerCallback::JITCompilationFinished(FunctionID functionId,
		HRESULT hrStatus, BOOL fIsSafeToBlock) {
	// Notify monitor that method has been jitted.
	FunctionInfo info;
	GetFunctionIdentifier(functionId, &info);
	_jittedMethods->push_back(info);

	// Always return OK
	return S_OK;
}

/** Write loaded assembly to log file. */
HRESULT CProfilerCallback::AssemblyLoadFinished(AssemblyID assemblyId,
		HRESULT hrStatus) {
	// Store assembly counter for id.
	int assemblyNumber = _assemblyCounter++;
	(*_assemblyMap)[assemblyId] = assemblyNumber;

	// Log assembly load.
	WCHAR assemblyName[NAME_BUFFER_SIZE];
	ULONG assemblyNameSize = 0;
	AppDomainID appDomainId = 0;
	ModuleID moduleId = 0;
	m_pICorProfilerInfo->GetAssemblyInfo(assemblyId, NAME_BUFFER_SIZE,
			&assemblyNameSize, assemblyName, &appDomainId, &moduleId);

	// Call GetModuleMetaData to get a MetaDataAssemblyImport object.
	IMetaDataAssemblyImport *pMetaDataAssemblyImport = NULL;
	m_pICorProfilerInfo->GetModuleMetaData(moduleId, ofRead,
			IID_IMetaDataAssemblyImport, (IUnknown**) &pMetaDataAssemblyImport);

	// Get the assembly token.
	mdAssembly ptkAssembly = NULL;
	pMetaDataAssemblyImport->GetAssemblyFromScope(&ptkAssembly);

	// Call GetAssemblyProps:
	// Allocate memory for the pointers, as GetAssemblyProps seems to
	// dereference the pointers (it crashed otherwise). We allocate the minimum
	// amount of memory as we do not know how much is needed to store the array.
	// This information would be available in the fields metadata.cbLocale,
	// metadata.ulProcessor and metadata.ulOS after the call to
	// GetAssemblyProps. However, we do not need the additional data and save
	// the second call to GetAssemblyProps with the correct amount of memory.
	ASSEMBLYMETADATA metadata;
	// TODO [NG]: What is the '1 *' for?
	// TODO [NG]: Why malloc/free instead of new/delete?
	metadata.szLocale = (WCHAR*) malloc(1 * sizeof(WCHAR));
	metadata.rProcessor = (DWORD*) malloc(1 * sizeof(DWORD));
	metadata.rOS = (OSINFO*) malloc(1 * sizeof(OSINFO));
	pMetaDataAssemblyImport->GetAssemblyProps(ptkAssembly, NULL, NULL, NULL,
			NULL, 0, NULL, &metadata, NULL);
	free(metadata.szLocale);
	free(metadata.rProcessor);
	free(metadata.rOS);

	char target[NAME_BUFFER_SIZE];
	sprintf_s(target, "%S:%i Version:%i.%i.%i.%i", assemblyName, assemblyNumber,
			metadata.usMajorVersion, metadata.usMinorVersion,
			metadata.usBuildNumber, metadata.usRevisionNumber);
	WriteTupleToFile(ASSEMBLY, target);

	// Always return OK
	return S_OK;
}

/** Record inlining of method, but generally allow it. */
HRESULT CProfilerCallback::JITInlining(FunctionID callerID, FunctionID calleeId,
		BOOL *pfShouldInline) {
	// Notify monitor that method has been inlined.
	if (_inlinedMethods->insert(calleeId).second == true) {
		FunctionInfo info;
		GetFunctionIdentifier(calleeId, &info);
		_inlinedMethodsList->push_back(info);
	}

	// Always allow inlining.
	*pfShouldInline = true;

	// Always return OK
	return S_OK;
}

/** Create method info object for a function id. */
HRESULT CProfilerCallback::GetFunctionIdentifier(FunctionID functionID,
		FunctionInfo* info) {
	HRESULT hr = E_FAIL; // Assume fail.
	mdToken funcToken = mdTypeDefNil;
	IMetaDataImport *pMDImport = NULL;
	WCHAR funName[NAME_BUFFER_SIZE] = L"UNKNOWN";

	// Get the MetadataImport interface and the metadata token.
	hr = m_pICorProfilerInfo->GetTokenAndMetaDataFromFunction(functionID,
			IID_IMetaDataImport, (IUnknown **) &pMDImport, &funcToken);
	if (SUCCEEDED(hr)) {
		mdTypeDef classToken = mdTypeDefNil;
		DWORD methodAttr = 0;
		PCCOR_SIGNATURE sigBlob = NULL;
		ULONG sigSize = 0;
		ModuleID moduleId = 0;
		hr = pMDImport->GetMethodProps(funcToken, &classToken, funName,
				NAME_BUFFER_SIZE, 0, &methodAttr, &sigBlob, &sigSize, NULL,
				NULL);
		if (SUCCEEDED(hr)) {
			WCHAR className[NAME_BUFFER_SIZE] = L"UNKNOWN";
			ClassID classId = 0;

			if (m_pICorProfilerInfo2 != NULL) {
				ULONG32 values = 0;
				hr = m_pICorProfilerInfo2->GetFunctionInfo2(functionID, 0,
						&classId, &moduleId, &funcToken, 0, &values, NULL);
				if (!SUCCEEDED(hr)) {
					classId = 0;
				}
			}

			int assemblyNumber = -1;
			if (SUCCEEDED(hr) && moduleId != 0) {
				// Get assembly name.
				AssemblyID assemblyId;
				hr = m_pICorProfilerInfo->GetModuleInfo(moduleId, NULL, NULL,
						NULL, NULL, &assemblyId);
				if (SUCCEEDED(hr)) {
					assemblyNumber = (*_assemblyMap)[assemblyId];
				}
			}

			info->assemblyNumber = assemblyNumber;
			info->classToken = classToken;
			info->funcToken = funcToken;
		}

		pMDImport->Release();
	}
	return hr;
}

/** Write name-value pair to log file. */
// TODO [NG]: Rename 'label' to 'key'?
void CProfilerCallback::WriteTupleToFile(const char* label, const char* value) {
	WriteToFile(label);
	WriteToFile("=");
	WriteToFile(value);
	WriteToFile("\r\n");
}

/** Write to log file. */
int CProfilerCallback::WriteToFile(const char *pszFmtString, ...) {
	// TODO [NG]: Why do we need to enter the critical section here? I think it
	//            can be moved inside the if-block.
	EnterCriticalSection(&m_prf_crit_sec);
	int retVal = 0;
	DWORD dwWritten = 0;
	memset(g_szBuffer, 0, 4096);

	va_list args;
	va_start(args, pszFmtString);
	retVal = wvsprintf(g_szBuffer, pszFmtString, args);
	va_end(args);

	// Write out to the file if the file is open.
	if (_resultFile != INVALID_HANDLE_VALUE) {
		if (TRUE == WriteFile(_resultFile, g_szBuffer,
						(DWORD) strlen(g_szBuffer), &dwWritten, NULL)) {
			retVal = dwWritten;
		} else {
			retVal = 0;
		}
	}
	LeaveCriticalSection(&m_prf_crit_sec);

	// TODO [NG]: What does this comment mean?
	// Also write out to the debug window.
	return retVal;
}

/** Write a list of method info values to the log. */
// TODO [NG]: Rename 'label' to 'key'?
void CProfilerCallback::WriteToLog(const char* label,
		vector<FunctionInfo>* list) {
	for (vector<FunctionInfo>::iterator i = list->begin(); i != list->end();
			i++) {
		FunctionInfo info = *i;
		char signature[NAME_BUFFER_SIZE];
		signature[0] = '\0';
		sprintf_s(signature, "%i:%i:%i", info.assemblyNumber, info.classToken,
				info.funcToken);
		WriteTupleToFile(label, signature);
	}
}
