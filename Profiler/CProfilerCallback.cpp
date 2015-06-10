/*
 * @ConQAT.Rating YELLOW Hash: 319C145D01BEA068904C5C8AE957347B
 */

#include <windows.h>
#include <stdio.h>
#include "CProfilerCallback.h"
#include <winuser.h>

#pragma intrinsic(strcmp,labs,strcpy,_rotl,memcmp,strlen,_rotr,memcpy,_lrotl,_strset,memset,_lrotr,abs,strcat)

namespace {

	/** The key to log information useful when interpreting the traces. */
	const char* LOG_KEY_INFO = "Info";

	/** The key to log information about a single assembly. */
	const char* LOG_KEY_ASSEMBLY = "Assembly";

	/** The key to log information about the profiled process. */
	const char* LOG_KEY_PROCESS = "Process";

	/** The key to log information about inlined methods. */
	const char* LOG_KEY_INLINED = "Inlined";

	/** The key to log information about jitted methods. */
	const char* LOG_KEY_JITTED = "Jitted";

	/** The key to log information about the profiler startup. */
	const char* LOG_KEY_STARTED = "Started";

	/** The key to log information about the profiler shutdown. */
	const char* LOG_KEY_STOPPED = "Stopped";

	/** The version of the profiler */
	const char* PROFILER_VERSION_INFO =
#ifdef _WIN64
		"Coverage profiler version 0.9.2.5 (x64)"
#else
		"Coverage profiler version 0.9.2.5 (x86)"
#endif
		;
}

CProfilerCallback::CProfilerCallback() : resultFile(INVALID_HANDLE_VALUE), isLightMode(false), assemblyCounter(1) {
	InitializeCriticalSection(&criticalSection);
}

CProfilerCallback::~CProfilerCallback() {
	DeleteCriticalSection(&criticalSection);
}

HRESULT CProfilerCallback::Initialize(IUnknown * pICorProfilerInfoUnkown) {
	char lightMode[BUFFER_SIZE];
	if (GetEnvironmentVariable("COR_PROFILER_LIGHT_MODE", lightMode,
		sizeof(lightMode)) && strcmp(lightMode, "1") == 0) {
		isLightMode = true;
		WriteTupleToFile(LOG_KEY_INFO, "Mode: light");
	} else {
		WriteTupleToFile(LOG_KEY_INFO, "Mode: force re-jitting");
	}

	CreateOutputFile();

	HRESULT hr = pICorProfilerInfoUnkown->QueryInterface( IID_ICorProfilerInfo2, (LPVOID *) &pICorProfilerInfo2);
	if (FAILED(hr) || pICorProfilerInfo2.p == NULL) {
		return E_INVALIDARG;
	}

	DWORD dwEventMask = GetEventMask();
	pICorProfilerInfo2->SetEventMask(dwEventMask);
	pICorProfilerInfo2->SetFunctionIDMapper(FunctionMapper);
	WriteProcessInfoToOutputFile();
	return S_OK;
}

void CProfilerCallback::WriteProcessInfoToOutputFile(){
	szAppPath[0] = 0;
	szAppName[0] = 0;
	if (0 == GetModuleFileNameW(NULL, szAppPath, MAX_PATH)) {
		_wsplitpath_s(szAppPath, NULL, 0, NULL, 0, szAppName, _MAX_FNAME, NULL, 0);
	}
	if (szAppPath[0] == 0) {
		wcscpy_s(szAppPath, MAX_PATH, L"No Application Path Found");
		wcscpy_s(szAppName, _MAX_FNAME, L"No Application Name Found");
	}

	// turn szAppPath from wchar_t to char
	char process[BUFFER_SIZE];
	sprintf_s(process, "%S", szAppPath);
	WriteTupleToFile(LOG_KEY_PROCESS, process);
}

void CProfilerCallback::CreateOutputFile() {
	// Read target directory from environment variable.
	char targetDir[BUFFER_SIZE];
	if (!GetEnvironmentVariable("COR_PROFILER_TARGETDIR", targetDir,
			sizeof(targetDir))) {
		sprintf_s(targetDir, BUFFER_SIZE, "c:/profiler/");
	}

	// Create target file.
	char targetFilename[BUFFER_SIZE];
	char timeStamp[BUFFER_SIZE];
	GetFormattedTime(timeStamp, BUFFER_SIZE);

	sprintf_s(targetFilename, "%s/coverage_%s.txt", targetDir, timeStamp);
	_tcscpy_s(pszResultFile, targetFilename);

	EnterCriticalSection(&criticalSection);
	resultFile = CreateFile(pszResultFile, GENERIC_WRITE, FILE_SHARE_READ,
			NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	
	WriteTupleToFile(LOG_KEY_INFO, PROFILER_VERSION_INFO);

	WriteTupleToFile(LOG_KEY_STARTED, timeStamp);
	LeaveCriticalSection(&criticalSection);
}

void CProfilerCallback::GetFormattedTime(char *result, size_t size) {
	SYSTEMTIME time;
	GetSystemTime (&time);
	// TODO (AG) size always equals BUFFER_SIZE. Remove method parameter and use BUFFER_SIZE directly.
	// TODO (FS) I'd rather not. If someone changes the buffer size later on (e.g. because it turns out we need a bigger buffer), this can cause really ugly memory issues here. Hard to debug.
	sprintf_s(result, size, "%04d%02d%02d_%02d%02d%02d%04d", time.wYear,
			time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond,
			time.wMilliseconds);
}

HRESULT CProfilerCallback::Shutdown() {
	char buffer[BUFFER_SIZE];

	EnterCriticalSection(&criticalSection);

	// Write inlined methods.
	sprintf_s(buffer, "//%i methods inlined\r\n", inlinedMethods.size());
	WriteToFile(buffer);
	WriteToLog(LOG_KEY_INLINED, &inlinedMethods);

	// TODO (AG) Is it safe to reuse the same buffer here? What if the second string is shorter than the first?
	// TODO (FS) it's safe. sprintf_s will always write a \0 character. see here: https://msdn.microsoft.com/en-us/library/ce3zzk1k.aspx
	// Write jitted methods.
	sprintf_s(buffer, "//%i methods jitted\r\n", jittedMethods.size());
	WriteToFile(buffer);
	WriteToLog(LOG_KEY_JITTED, &jittedMethods);

	// Write timestamp.
	char timeStamp[BUFFER_SIZE];

	GetFormattedTime(timeStamp, sizeof(timeStamp));
	WriteTupleToFile(LOG_KEY_STOPPED, timeStamp);

	WriteTupleToFile(LOG_KEY_INFO, "Shutting down coverage profiler" );

	LeaveCriticalSection(&criticalSection);

	pICorProfilerInfo2->ForceGC();

	// Close the log file.
	EnterCriticalSection(&criticalSection);
	if(resultFile != INVALID_HANDLE_VALUE) {
		CloseHandle(resultFile);
	}
	LeaveCriticalSection(&criticalSection);

	return S_OK;
}

DWORD CProfilerCallback::GetEventMask() {
	DWORD dwEventMask = 0;
	dwEventMask |= COR_PRF_MONITOR_JIT_COMPILATION;
	dwEventMask |= COR_PRF_MONITOR_ASSEMBLY_LOADS;

	// disable force re-jitting for the light variant
	if (!isLightMode) {
		WriteTupleToFile("Mode", "Light");
		dwEventMask |= COR_PRF_MONITOR_ENTERLEAVE;
	}

	return dwEventMask;
}

UINT_PTR CProfilerCallback::FunctionMapper(FunctionID functionId,
		BOOL *pbHookFunction) {
	// Disable hooking of functions.
	*pbHookFunction = false;

	// Always return original function id.
	return functionId;
}

HRESULT CProfilerCallback::JITCompilationFinished(FunctionID functionId,
		HRESULT hrStatus, BOOL fIsSafeToBlock) {
	// Notify monitor that method has been jitted.
	FunctionInfo info;
	GetFunctionInfo(functionId, &info);
	jittedMethods.push_back(info);

	// Always return OK
	return S_OK;
}

HRESULT CProfilerCallback::AssemblyLoadFinished(AssemblyID assemblyId,
		HRESULT hrStatus) {
	// Store assembly counter for id.
	int assemblyNumber = assemblyCounter++;
	assemblyMap[assemblyId] = assemblyNumber;

	// Log assembly load.
	WCHAR assemblyName[BUFFER_SIZE];
	ULONG assemblyNameSize = 0;
	AppDomainID appDomainId = 0;
	ModuleID moduleId = 0;
	pICorProfilerInfo2->GetAssemblyInfo(assemblyId, BUFFER_SIZE,
			&assemblyNameSize, assemblyName, &appDomainId, &moduleId);

	// Call GetModuleMetaData to get a MetaDataAssemblyImport object.
	IMetaDataAssemblyImport *pMetaDataAssemblyImport = NULL;
	pICorProfilerInfo2->GetModuleMetaData(moduleId, ofRead,
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

    // We have to explicitly set these to NULL, otherwise the .NET framework will try
    // to access these pointers at a later time and crash, because they are not
    // valid. This happened when we started an application multiple times on
    // the same machine in rapid succession.
	metadata.szLocale = NULL;
	metadata.rProcessor = NULL;
	metadata.rOS = NULL;

	pMetaDataAssemblyImport->GetAssemblyProps(ptkAssembly, NULL, NULL, NULL,
			NULL, 0, NULL, &metadata, NULL);

	char target[BUFFER_SIZE];
	sprintf_s(target, "%S:%i Version:%i.%i.%i.%i", assemblyName, assemblyNumber,
			metadata.usMajorVersion, metadata.usMinorVersion,
			metadata.usBuildNumber, metadata.usRevisionNumber);
	WriteTupleToFile(LOG_KEY_ASSEMBLY, target);

	// Always return OK
	return S_OK;
}

HRESULT CProfilerCallback::JITInlining(FunctionID callerID, FunctionID calleeId,
		BOOL *pfShouldInline) {
	// Save information about inlined method.
	if (inlinedMethodIds.insert(calleeId).second == true) {
		FunctionInfo info;
		GetFunctionInfo(calleeId, &info);
		inlinedMethods.push_back(info);
	}
	// Always allow inlining.
	*pfShouldInline = true;

	// Always return OK
	return S_OK;
}

HRESULT CProfilerCallback::GetFunctionInfo(FunctionID functionId,
		FunctionInfo* info) {
	mdToken functionToken = mdTypeDefNil;
	IMetaDataImport *pMDImport = NULL;
	WCHAR funName[BUFFER_SIZE] = L"UNKNOWN";

	HRESULT hr = pICorProfilerInfo2->GetTokenAndMetaDataFromFunction(functionId,
			IID_IMetaDataImport, (IUnknown **) &pMDImport, &functionToken);
	if (!SUCCEEDED(hr)) {
		return hr;
	}

	mdTypeDef classToken = mdTypeDefNil;
	DWORD methodAttr = 0;
	PCCOR_SIGNATURE sigBlob = NULL;
	ULONG sigSize = 0;
	ModuleID moduleId = 0;
	hr = pMDImport->GetMethodProps(functionToken, &classToken, funName,
			BUFFER_SIZE, 0, &methodAttr, &sigBlob, &sigSize, NULL,
			NULL);
	if (SUCCEEDED(hr)) {
		FillFunctionInfo(info, functionId, functionToken, moduleId, classToken);
	}
	
	pMDImport->Release();
	
	return hr;
}

void CProfilerCallback::FillFunctionInfo(FunctionInfo* info, FunctionID functionId, mdToken functionToken, ModuleID moduleId, mdTypeDef classToken) {
	ClassID classId = 0;
	ULONG32 values = 0;
	HRESULT hr = pICorProfilerInfo2->GetFunctionInfo2(functionId, 0,
		&classId, &moduleId, &functionToken, 0, &values, NULL);
	if (!SUCCEEDED(hr)) {
		classId = 0;
	}

	int assemblyNumber = -1;
	if (SUCCEEDED(hr) && moduleId != 0) {
		AssemblyID assemblyId;
		hr = pICorProfilerInfo2->GetModuleInfo(moduleId, NULL, NULL,
			NULL, NULL, &assemblyId);
		if (SUCCEEDED(hr)) {
			assemblyNumber = assemblyMap[assemblyId];
		}
	}

	info->assemblyNumber = assemblyNumber;
	info->classToken = classToken;
	info->functionToken = functionToken;
}

void CProfilerCallback::WriteTupleToFile(const char* key, const char* value) {
	char buffer[BUFFER_SIZE];
	sprintf_s(buffer, "%s=%s\r\n", key, value);
	WriteToFile(buffer);
}

int CProfilerCallback::WriteToFile(const char *string) {
	int retVal = 0;
	DWORD dwWritten = 0;

	if (resultFile != INVALID_HANDLE_VALUE) {
		EnterCriticalSection(&criticalSection);
		if (TRUE == WriteFile(resultFile, string,
			(DWORD)strlen(string), &dwWritten, NULL)) {
			retVal = dwWritten;
		} else {
			retVal = 0;
		}
		LeaveCriticalSection(&criticalSection);
	}
	
	return retVal;
}

void CProfilerCallback::WriteToLog(const char* key,
		vector<FunctionInfo>* functions) {
	for (vector<FunctionInfo>::iterator i = functions->begin(); i != functions->end();
			i++) {
		FunctionInfo info = *i;
		char signature[BUFFER_SIZE];
		signature[0] = '\0';
		sprintf_s(signature, "%i:%i:%i", info.assemblyNumber, info.classToken,
				info.functionToken);
		WriteTupleToFile(key, signature);
	}
}
