#include <windows.h>
#include <stdio.h>
#include "ProfilerCallback.h"
#include <winuser.h>

#pragma intrinsic(strcmp,labs,strcpy,_rotl,memcmp,strlen,_rotr,memcpy,_lrotl,_strset,memset,_lrotr,abs,strcat)

// Constants used for report generation
#ifdef _WIN64
	#define HEADER "Coverage profiler version 0.9.1.1 (x64)"
#endif
#ifndef _WIN64
	#define HEADER "Coverage profiler version 0.9.1.1 (x86)"
#endif

#define INFO "Info"
#define ASSEMBLY "Assembly"
#define PROCESS "Process"
#define INLINED "Inlined"
#define JITTED "Jitted"
#define STARTED "Started"
#define STOPPED "Stopped"

CHAR g_szBuffer[4096];


/**
 * Constructor
 */
CProfilerCallback::CProfilerCallback() :
    m_dwEventMask(0),
	_resultFile(INVALID_HANDLE_VALUE) {

	// make a critical section for synchronization
	InitializeCriticalSection(&m_prf_crit_sec);
}

/**
 * Destructor
 */
CProfilerCallback::~CProfilerCallback()
{
	// clean up the critical section
	DeleteCriticalSection(&m_prf_crit_sec);
}


/**
 * Initializer. Called at profiler startup.
 */
HRESULT CProfilerCallback::Initialize(IUnknown * pICorProfilerInfoUnk )
{
	//#ifdef _WIN64
	//printf("Profiler (x64) Initializing.");
	//#endif
	//#ifndef _WIN64
	//printf("Profiler (x86) Initializing.");
	//#endif

	
	// read target directory from environment variable 
	char targetDir[1000];
	if ( !GetEnvironmentVariable( "COR_PROFILER_TARGETDIR", targetDir,
                                    sizeof(targetDir) ) ) {
        sprintf(targetDir, "c:/profiler/");
    }
	SYSTEMTIME time;
	GetSystemTime (&time);


	// create target file
	char targetFilename[1000];
	sprintf (targetFilename, "%s/coverage_%04d%02d%02d_%02d%02d%02d%04d.txt", targetDir, time.wYear, time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond, time.wMilliseconds);
	_tcscpy(m_pszResultFile, targetFilename);

	EnterCriticalSection(&m_prf_crit_sec);
	_resultFile = CreateFile(m_pszResultFile,GENERIC_WRITE,FILE_SHARE_READ,NULL,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,NULL);
	WriteTupleToFile(INFO, HEADER);
	
	char timeStamp[NAME_BUFFER_SIZE];
	sprintf(timeStamp, "%04d%02d%02d_%02d%02d%02d%04d", time.wYear, time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond, time.wMilliseconds);
	WriteTupleToFile(STARTED, timeStamp);
	LeaveCriticalSection(&m_prf_crit_sec);


	// intitialize data structures
	_assemblyCounter = 1;
	_assemblyMap = new map<int, int>;
	_jittedMethods = new vector<MethodInfo>;
	_inlinedMethods = new set<FunctionID>;
	_inlinedMethodsList = new vector<MethodInfo>;

	// Get reference to the ICorProfilerInfo interface 
    HRESULT hr =
        pICorProfilerInfoUnk->QueryInterface( IID_ICorProfilerInfo,
                                            (LPVOID *)&m_pICorProfilerInfo );
	if ( FAILED(hr) ) {
        return E_INVALIDARG;
	}

    hr = pICorProfilerInfoUnk->QueryInterface( IID_ICorProfilerInfo2,
                                            (LPVOID *)&m_pICorProfilerInfo2 );
    
	if ( FAILED(hr) ) {
		// we still want to work if this call fails, might be an older .NET version than VS2005
        OutputDebugString("Pre-VS2005 version detected");
		m_pICorProfilerInfo2.p = NULL;
	}


	// Indicate which events we're interested in.
	m_dwEventMask = GetEventMask();

	// set the event mask
	if(m_pICorProfilerInfo2.p == NULL)
	{
		// Pre VS2005
		m_pICorProfilerInfo->SetEventMask( m_dwEventMask );
		// Enable function mapping
		m_pICorProfilerInfo->SetFunctionIDMapper(FunctionMapper);
	}
	else
	{
		//VS 2005
		m_pICorProfilerInfo2->SetEventMask( m_dwEventMask );

		// Enable function mapping
		m_pICorProfilerInfo2->SetFunctionIDMapper(FunctionMapper);
	}


	// Get the name of the executing process and write to log
	m_szAppPath[0]=0x00;
	m_szAppName[0]=0x00;
    if (0 == GetModuleFileNameW (NULL, m_szAppPath, MAX_PATH))
	    _wsplitpath_s (m_szAppPath, NULL,0, NULL,0, m_szAppName,_MAX_FNAME, NULL, 0);

	if(m_szAppPath[0]==0x00)
	{
		wcscpy_s(m_szAppPath,MAX_PATH,L"No Application Path Found");
		wcscpy_s(m_szAppName,_MAX_FNAME,L"No Application Name Found");
	}

	char process[NAME_BUFFER_SIZE];
	sprintf(process, "%S", m_szAppPath);
	WriteTupleToFile(PROCESS, process );


    return S_OK;
}

/**
 * Write coverage information to log file at shutdown.
 */
HRESULT CProfilerCallback::Shutdown()
{
	// get timestamp
	SYSTEMTIME time;
	GetSystemTime (&time);

	// write inlined methods
	WriteToFile("//%i methods inlined\r\n", _inlinedMethodsList->size());
	WriteToLog(INLINED, _inlinedMethodsList);

	// write jitted methods
	WriteToFile("//%i methods jitted\r\n", _jittedMethods->size());
	WriteToLog(JITTED, _jittedMethods);

	// write timestamp
	char timeStamp[NAME_BUFFER_SIZE];
	sprintf(timeStamp, "%04d%02d%02d_%02d%02d%02d%04d", time.wYear, time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond, time.wMilliseconds);
	WriteTupleToFile(STOPPED, timeStamp);

	WriteTupleToFile(INFO, "Shutting down coverage profiler" );
	
	// cleanup
	m_pICorProfilerInfo2->ForceGC();

	// close the log file
	EnterCriticalSection(&m_prf_crit_sec);
	if(_resultFile != INVALID_HANDLE_VALUE)
		CloseHandle(_resultFile);
	LeaveCriticalSection(&m_prf_crit_sec);
	
	delete _assemblyMap;
	delete _jittedMethods;
	delete _inlinedMethods;
	delete _inlinedMethodsList;

    return S_OK;
}


/**
 * The event mask tells the CLR which callbacks the profiler wants to subscribe to.
 * We enable JIT compilation and assembly loads for coverage profiling. 
 * In addition, EnterLeave hooks are enabled to force re-jitting of pre-jitted code, 
 * in order to make coverage information independent of pre-jitted code.
 */
DWORD CProfilerCallback::GetEventMask()
{
	m_dwEventMask = 0;
	m_dwEventMask |= COR_PRF_MONITOR_JIT_COMPILATION;
	m_dwEventMask |= COR_PRF_MONITOR_ASSEMBLY_LOADS;
	m_dwEventMask |= COR_PRF_MONITOR_ENTERLEAVE; 

	return m_dwEventMask;
}


/**
 * We do not register a single function callback in order to not affect performance.
 * In effect, we disable this feature here. It has been tested via a performance benchmark,
 * that this implementation does not impact call performance.
 * 
 * (For coverage profiling, we do not need this callback. However, the event is enabled
 * in the event mask in order to force JIT-events for each first call to a function, 
 * independent of whether a prejitted version exists.)
 */
UINT_PTR CProfilerCallback::FunctionMapper(FunctionID functionId,
											BOOL *pbHookFunction)
{
	// disable hooking of functions
	*pbHookFunction = false;

	// Always return original function id
	return functionId;
}


/**
 * Store information about jitted method.
 */
HRESULT CProfilerCallback::JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
	// notify monitor that method has been jitted
	MethodInfo info;
	GetFunctionIdentifier(functionId, &info);
	_jittedMethods->push_back(info);

	// Always return OK
	return S_OK;
}


/**
 * Write loaded assembly to log file.
 */
HRESULT CProfilerCallback::AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus) {

	// store assembly counter for id
	int assemblyNumber = _assemblyCounter++;
	(*_assemblyMap)[assemblyId] = assemblyNumber;

	// log assembly load
	WCHAR assemblyName[NAME_BUFFER_SIZE];

	ULONG assemblyNameSize = 0;
	AppDomainID appDomainId = 0;
	ModuleID moduleId = 0;
	m_pICorProfilerInfo->GetAssemblyInfo(assemblyId, NAME_BUFFER_SIZE, &assemblyNameSize, assemblyName, &appDomainId, &moduleId);


	IMetaDataAssemblyImport * pMetaDataAssemblyImport = NULL;
	HRESULT hr = S_OK;
	hr = m_pICorProfilerInfo->GetModuleMetaData(moduleId, ofRead, IID_IMetaDataAssemblyImport, (IUnknown** ) &pMetaDataAssemblyImport);
	
	if(SUCCEEDED(hr)){
		WriteTupleToFile("GetModuleMetaData", "Success");
	}else{
		WriteTupleToFile("GetModuleMetaData", "Fail");
	}
	
	mdAssembly ptkAssembly = NULL;
	hr = pMetaDataAssemblyImport->GetAssemblyFromScope(&ptkAssembly);
	if(SUCCEEDED(hr)){
		WriteTupleToFile("GetAssemblyFromScope", "Success");
	}else{
		WriteTupleToFile("GetAssemblyFromScope", "Fail");
	}

	if(ptkAssembly == NULL){
		WriteTupleToFile("ptkAssembly", "is null");
	}
	
	ULONG pcbPublicKey = 0;
	ULONG pulHashAlgId = 0;
	wchar_t buff[1024];
	ASSEMBLYMETADATA metadata;
	ULONG nameLength = 0;
//	pMetaDataAssemblyImport->GetAssemblyProps(ptkAssembly, NULL, NULL, NULL, buff, 1024, &nameLength, &metadata, NULL);
/*	        hr = pMetaDataAssemblyImport->GetAssemblyProps(
                ptkAssembly,
                NULL, NULL,
                NULL,
                NULL, 0, NULL,
                &metadata,
                NULL);
      // alloc mem for AssemblyMetaData arrays
        if (metadata.cbLocale)
                metadata.szLocale = (WCHAR*)malloc(metadata.cbLocale * sizeof(WCHAR));
        if (metadata.ulProcessor)
                metadata.rProcessor = (DWORD*)malloc(metadata.ulProcessor * sizeof(DWORD));
        if (metadata.ulOS)
                metadata.rOS = (OSINFO*)malloc(metadata.ulOS * sizeof(OSINFO));
 */         

    metadata.szLocale = (WCHAR*)malloc(1024 * sizeof(WCHAR));
    metadata.rProcessor = (DWORD*)malloc(1024 * sizeof(DWORD));
    metadata.rOS = (OSINFO*)malloc(1024 * sizeof(OSINFO));
	hr = pMetaDataAssemblyImport->GetAssemblyProps(
                ptkAssembly,
                NULL, NULL,
                NULL,
                NULL, 0, NULL,
                &metadata,
                NULL);
	// recall GetAssemblyProps	
/*	byte *pbyPublicKey;  
	DWORD dwcKey, dwHashAlg, dwFlags;   
	WCHAR wcName[1024];
    hr = pMetaDataAssemblyImport->GetAssemblyProps(
                ptkAssembly,
                (const void**)&pcbPublicKey, &dwcKey,
                &dwHashAlg,
                wcName, 1024, NULL,
                &metadata,
                &dwFlags);
*/
	char target[NAME_BUFFER_SIZE];
	sprintf(target, "%S:%i Version:%i.%i.%i.%i", assemblyName, assemblyNumber, metadata.usMajorVersion, metadata.usMinorVersion, metadata.usBuildNumber, metadata.usRevisionNumber);
//	sprintf(target, "%S:%i Version", assemblyName, assemblyNumber);
	WriteTupleToFile(ASSEMBLY, target);

	

	// Always return OK
	return S_OK;
}


/**
 * Record inlining of method, but generally allow it.
 */
HRESULT CProfilerCallback::JITInlining(FunctionID callerID, FunctionID calleeId, BOOL *pfShouldInline) {
	// notify monitor that method has been inlined
	if (_inlinedMethods->insert(calleeId).second == true ) {
		MethodInfo info;
		GetFunctionIdentifier(calleeId, &info);
		_inlinedMethodsList->push_back(info);
	}

	// always allow inlining
	*pfShouldInline = true;

	// Always return OK
	return S_OK;
}



/**
 * Create method info object for a function id.
 */
HRESULT CProfilerCallback::GetFunctionIdentifier( FunctionID functionID, MethodInfo* info)
{
	HRESULT hr = E_FAIL; // assume success
            

    mdToken funcToken = mdTypeDefNil;
    IMetaDataImport *pMDImport = NULL;      
    WCHAR funName[NAME_BUFFER_SIZE] = L"UNKNOWN";
            
    
    //
    // Get the MetadataImport interface and the metadata token 
    //
    hr = m_pICorProfilerInfo->GetTokenAndMetaDataFromFunction( functionID, 
                                                           IID_IMetaDataImport, 
                                                           (IUnknown **)&pMDImport,
                                                           &funcToken );
    if ( SUCCEEDED( hr ) )
    {
        mdTypeDef classToken = mdTypeDefNil;
        DWORD methodAttr = 0;
        PCCOR_SIGNATURE sigBlob = NULL;
		ULONG sigSize = 0;
		ModuleID moduleId = 0;
        hr = pMDImport->GetMethodProps( funcToken,
                                        &classToken,
                                        funName,
                                        NAME_BUFFER_SIZE,
                                        0,
                                        &methodAttr,
                                        &sigBlob,
                                        &sigSize,
                                        NULL, 
                                        NULL );
        if ( SUCCEEDED( hr ) )
        {
            WCHAR className[NAME_BUFFER_SIZE] = L"UNKNOWN";
            ClassID classId =0;


            if (m_pICorProfilerInfo2 != NULL)
            {
				ULONG32 values = 0;
                hr = m_pICorProfilerInfo2->GetFunctionInfo2(functionID,
                                                        0,
                                                        &classId,
                                                        &moduleId,
														&funcToken,
                                                        0,
                                                        &values,
                                                        NULL);
				if (!SUCCEEDED(hr)) {
                    classId = 0;
				}
            }


			int assemblyNumber = -1;
			if (SUCCEEDED(hr) && moduleId != 0)
            {
				// get assembly name
				AssemblyID assemblyId;
				hr = m_pICorProfilerInfo->GetModuleInfo(moduleId, NULL, NULL, NULL, NULL, &assemblyId);
				if (SUCCEEDED (hr)) {
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
} // PrfInfo::GetFunctionProperties


/**
 * Write name-value pair to log file.
 */
void CProfilerCallback::WriteTupleToFile(const char* label, const char* value) {
	WriteToFile(label);
	WriteToFile("=");
	WriteToFile(value);
	WriteToFile("\r\n");
}

/**
 * Write to log file.
 */
int CProfilerCallback::WriteToFile(const char *pszFmtString, ...)
{
	EnterCriticalSection(&m_prf_crit_sec);
    int retVal = 0;
	DWORD dwWritten = 0;
	memset(g_szBuffer,0,4096);

    va_list args;
    va_start( args, pszFmtString );
    retVal = wvsprintf(g_szBuffer, pszFmtString, args );
    va_end( args );

	// write out to the file if the file is open
	if(_resultFile != INVALID_HANDLE_VALUE)
	{
		if(TRUE == WriteFile(_resultFile ,g_szBuffer,(DWORD)strlen(g_szBuffer),&dwWritten,NULL))
		{
			retVal = dwWritten;
		}
		else
			retVal = 0;
	}
	LeaveCriticalSection(&m_prf_crit_sec);

	// also write out to the debug window
    return retVal;
}

/**
 * Write a list of method info values to the log.
 */
void CProfilerCallback::WriteToLog(const char* label, vector<MethodInfo>* list) {
	for (vector<MethodInfo>::iterator i = list->begin (); i != list->end (); i++) {
		MethodInfo info = *i;
		char signature[NAME_BUFFER_SIZE];
		signature[0] = '\0';
		sprintf(signature, "%i:%i:%i", info.assemblyNumber, info.classToken, info.funcToken);
		WriteTupleToFile(label, signature);
	}
}
