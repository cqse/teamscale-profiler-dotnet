/*
 * @ConQAT.Rating RED Hash: AA6F2F22E767A44C243F82B6A81A756F
 */

#include <windows.h>
#include <stdio.h>
#include "CProfilerCallback.h"
#include <winuser.h>

#pragma intrinsic(strcmp,labs,strcpy,_rotl,memcmp,strlen,_rotr,memcpy,_lrotl,_strset,memset,_lrotr,abs,strcat)

const char* logKeyInfo = "Info";
const char* logKeyAssembly = "Assembly";
const char* logKeyProcess = "Process";
const char* logKeyInlined = "Inlined";
const char* logKeyJitted = "Jitted";
const char* logKeyStarted = "Started";
const char* logKeyStopped = "Stopped";

/** The version of the profiler */
#ifdef _WIN64
const char* profilerVersionInfo = "Coverage profiler version 0.9.2.4 (x64)";
#else
const char* profilerVersionInfo = "Coverage profiler version 0.9.2.4 (x86)";
#endif

CProfilerCallback::CProfilerCallback() : resultFile(INVALID_HANDLE_VALUE), isLightMode(false) {
	// Make a critical section for synchronization.
	InitializeCriticalSection(&criticalSection);
}

CProfilerCallback::~CProfilerCallback() {
	// Clean up the critical section.
	DeleteCriticalSection(&criticalSection);
}

HRESULT CProfilerCallback::Initialize(IUnknown * pICorProfilerInfoUnkown) {
	char lightMode[bufferSize];
	if (GetEnvironmentVariable("COR_PROFILER_LIGHT_MODE", lightMode,
		sizeof(lightMode)) && strcmp(lightMode, "1") == 0) {
		isLightMode = true;
		WriteTupleToFile(logKeyInfo, "Mode: light");
	} else {
		WriteTupleToFile(logKeyInfo, "Mode: force re-jitting");
	}

	CreateOutputFile();

	// Initialize data structures.
	assemblyCounter = 1;

	// Get reference to the ICorProfilerInfo interface 
	HRESULT hr = pICorProfilerInfoUnkown->QueryInterface( IID_ICorProfilerInfo, (LPVOID *)&pICorProfilerInfo );
	if ( FAILED(hr) ) {
		return E_INVALIDARG;
	}

	hr = pICorProfilerInfoUnkown->QueryInterface( IID_ICorProfilerInfo2, (LPVOID *)&pICorProfilerInfo2 );

	if ( FAILED(hr) ) {
		// We still want to work if this call fails, might be an older .NET
		// version than VS2005.
		OutputDebugString("Pre .NET 2 version detected");
		pICorProfilerInfo2.p = NULL;
	}

	// Indicate which events we're interested in.
	DWORD dwEventMask = GetEventMask();

	// Set the event mask for the interfaces of .NET 1 and .NET 2. We currently
	// do not need the features of the profiling interface beyond .NET 2.
	// TODO (AG) It appears that the new profiler does *not at all* work with older .NET versions. So maybe this can be removed?
	// TODO (FS) to be honest, I have no idea whaat is happening here and why. I'd rather consult with MF before changing anything here.
	if (pICorProfilerInfo2.p == NULL) {
		// Pre .NET 2.
		pICorProfilerInfo->SetEventMask(dwEventMask);
		// Enable function mapping.
		pICorProfilerInfo->SetFunctionIDMapper(FunctionMapper);
	} else {
		// .NET 2 and beyond.
		pICorProfilerInfo2->SetEventMask(dwEventMask);
		// Enable function mapping.
		pICorProfilerInfo2->SetFunctionIDMapper(FunctionMapper);
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
	char process[bufferSize];
	sprintf_s(process, "%S", szAppPath);
	WriteTupleToFile(logKeyProcess, process);
}

/** Create the output file and add general information. */
void CProfilerCallback::CreateOutputFile() {
	// Read target directory from environment variable.
	char targetDir[bufferSize];
	if (!GetEnvironmentVariable("COR_PROFILER_TARGETDIR", targetDir,
			sizeof(targetDir))) {
		sprintf_s(targetDir, bufferSize, "c:/profiler/");
	}

	// Create target file.
	char targetFilename[bufferSize];
	char timeStamp[bufferSize];
	GetFormattedTime(timeStamp, bufferSize);

	sprintf_s(targetFilename, "%s/coverage_%s.txt", targetDir, timeStamp);
	_tcscpy_s(pszResultFile, targetFilename);

	EnterCriticalSection(&criticalSection);
	resultFile = CreateFile(pszResultFile, GENERIC_WRITE, FILE_SHARE_READ,
			NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	
	WriteTupleToFile(logKeyInfo, profilerVersionInfo);

	WriteTupleToFile(logKeyStarted, timeStamp);
	LeaveCriticalSection(&criticalSection);
}

/** Makes result point to a string representing the current time. */ 
void CProfilerCallback::GetFormattedTime(char *result, size_t size) {
	SYSTEMTIME time;
	GetSystemTime (&time);
	// TODO (AG) size always equals bufferSize. Remove method parameter and use bufferSize directly.
	// TODO (FS) I'd rather not. If someone changes the buffer size later on (e.g. because it turns out we need a bigger buffer), this can cause really ugly memory issues here. Hard to debug.
	sprintf_s(result, size, "%04d%02d%02d_%02d%02d%02d%04d", time.wYear,
			time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond,
			time.wMilliseconds);
}

HRESULT CProfilerCallback::Shutdown() {
	char buffer[bufferSize];

	EnterCriticalSection(&criticalSection);

	// Write inlined methods.
	sprintf_s(buffer, "//%i methods inlined\r\n", inlinedMethods.size());
	WriteToFile(buffer);
	WriteToLog(logKeyInlined, &inlinedMethods);

	// TODO (AG) Is it safe to reuse the same buffer here? What if the second string is shorter than the first?
	// TODO (FS) it's safe. sprintf_s will always write a \0 character. see here: https://msdn.microsoft.com/en-us/library/ce3zzk1k.aspx
	// Write jitted methods.
	sprintf_s(buffer, "//%i methods jitted\r\n", jittedMethods.size());
	WriteToFile(buffer);
	WriteToLog(logKeyJitted, &jittedMethods);

	// Write timestamp.
	char timeStamp[bufferSize];

	GetFormattedTime(timeStamp, sizeof(timeStamp));
	WriteTupleToFile(logKeyStopped, timeStamp);

	WriteTupleToFile(logKeyInfo, "Shutting down coverage profiler" );

	LeaveCriticalSection(&criticalSection);

	// Cleanup.
	pICorProfilerInfo2->ForceGC();

	// Close the log file.
	EnterCriticalSection(&criticalSection);
	if(resultFile != INVALID_HANDLE_VALUE) {
		CloseHandle(resultFile);
	}
	LeaveCriticalSection(&criticalSection);

	return S_OK;
}

/**
 * The event mask tells the CLR which callbacks the profiler wants to subscribe
 * to. We enable JIT compilation and assembly loads for coverage profiling. In
 * addition, EnterLeave hooks are enabled to force re-jitting of pre-jitted
 * code, in order to make coverage information independent of pre-jitted code.
 */
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
	WCHAR assemblyName[bufferSize];
	ULONG assemblyNameSize = 0;
	AppDomainID appDomainId = 0;
	ModuleID moduleId = 0;
	pICorProfilerInfo->GetAssemblyInfo(assemblyId, bufferSize,
			&assemblyNameSize, assemblyName, &appDomainId, &moduleId);

	// Call GetModuleMetaData to get a MetaDataAssemblyImport object.
	IMetaDataAssemblyImport *pMetaDataAssemblyImport = NULL;
	pICorProfilerInfo->GetModuleMetaData(moduleId, ofRead,
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
    // valid. This happened @MR when we started an application multiple times on
    // the same machine in rapid succession.
	metadata.szLocale = NULL;
	metadata.rProcessor = NULL;
	metadata.rOS = NULL;

	pMetaDataAssemblyImport->GetAssemblyProps(ptkAssembly, NULL, NULL, NULL,
			NULL, 0, NULL, &metadata, NULL);

	char target[bufferSize];
	sprintf_s(target, "%S:%i Version:%i.%i.%i.%i", assemblyName, assemblyNumber,
			metadata.usMajorVersion, metadata.usMinorVersion,
			metadata.usBuildNumber, metadata.usRevisionNumber);
	WriteTupleToFile(logKeyAssembly, target);

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

// TODO (AG) This is long and depply nested (see Teamscale findings). Maybe extract some of the inner ifs?
HRESULT CProfilerCallback::GetFunctionInfo(FunctionID functionID,
		FunctionInfo* info) {
	HRESULT hr = E_FAIL; // Assume fail.
	mdToken functionToken = mdTypeDefNil;
	IMetaDataImport *pMDImport = NULL;
	WCHAR funName[bufferSize] = L"UNKNOWN";

	// Get the MetadataImport interface and the metadata token.
	hr = pICorProfilerInfo->GetTokenAndMetaDataFromFunction(functionID,
			IID_IMetaDataImport, (IUnknown **) &pMDImport, &functionToken);
	if (SUCCEEDED(hr)) {
		mdTypeDef classToken = mdTypeDefNil;
		DWORD methodAttr = 0;
		PCCOR_SIGNATURE sigBlob = NULL;
		ULONG sigSize = 0;
		ModuleID moduleId = 0;
		hr = pMDImport->GetMethodProps(functionToken, &classToken, funName,
				bufferSize, 0, &methodAttr, &sigBlob, &sigSize, NULL,
				NULL);
		if (SUCCEEDED(hr)) {
			WCHAR className[bufferSize] = L"UNKNOWN";
			ClassID classId = 0;

			if (pICorProfilerInfo2 != NULL) {
				ULONG32 values = 0;
				hr = pICorProfilerInfo2->GetFunctionInfo2(functionID, 0,
						&classId, &moduleId, &functionToken, 0, &values, NULL);
				if (!SUCCEEDED(hr)) {
					classId = 0;
				}
			}

			int assemblyNumber = -1;
			if (SUCCEEDED(hr) && moduleId != 0) {
				// Get assembly name.
				AssemblyID assemblyId;
				hr = pICorProfilerInfo->GetModuleInfo(moduleId, NULL, NULL,
						NULL, NULL, &assemblyId);
				if (SUCCEEDED(hr)) {
					assemblyNumber = assemblyMap[assemblyId];
				}
			}

			info->assemblyNumber = assemblyNumber;
			info->classToken = classToken;
			info->functionToken = functionToken;
		}

		pMDImport->Release();
	}
	return hr;
}

void CProfilerCallback::WriteTupleToFile(const char* key, const char* value) {
	char buffer[bufferSize];
	sprintf_s(buffer, "%s=%s\r\n", key, value);
	WriteToFile(buffer);
}

int CProfilerCallback::WriteToFile(const char *string) {
	int retVal = 0;
	DWORD dwWritten = 0;

	// Write out to the file if the file is open.
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

/** Write a list of functionInfos values to the log. */
void CProfilerCallback::WriteToLog(const char* key,
		vector<FunctionInfo>* functions) {
	for (vector<FunctionInfo>::iterator i = functions->begin(); i != functions->end();
			i++) {
		FunctionInfo info = *i;
		char signature[bufferSize];
		signature[0] = '\0';
		sprintf_s(signature, "%i:%i:%i", info.assemblyNumber, info.classToken,
				info.functionToken);
		WriteTupleToFile(key, signature);
	}
}
