/*
 * @ConQAT.Rating GREEN Hash: 50065B80A9CB7AECB61678F6A0834456
 */

#include <windows.h>
#include <stdio.h>
#include <fstream>
#include <string>
#include <algorithm>
#include "CProfilerCallback.h"
#include <winuser.h>

#pragma intrinsic(strcmp,labs,strcpy,_rotl,memcmp,strlen,_rotr,memcpy,_lrotl,_strset,memset,_lrotr,abs,strcat)

namespace {

	/** The key to log information useful when interpreting the traces. */
	const char* LOG_KEY_INFO = "Info";

	/** The key to log information about non-critical error conditions. */
	const char* LOG_KEY_WARN= "Warn";

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

CProfilerCallback::CProfilerCallback() : logFile(INVALID_HANDLE_VALUE), isLightMode(false), assemblyCounter(1) {
	InitializeCriticalSection(&criticalSection);
}

CProfilerCallback::~CProfilerCallback() {
	DeleteCriticalSection(&criticalSection);
}

HRESULT CProfilerCallback::Initialize(IUnknown* pICorProfilerInfoUnkown) {
	createLogFile();
	readConfig();

	if (getOption("LIGHT_MODE") == "1") {
		isLightMode = true;
		writeTupleToFile(LOG_KEY_INFO, "Mode: light");
	} else {
		writeTupleToFile(LOG_KEY_INFO, "Mode: force re-jitting");
	}

	HRESULT hr = pICorProfilerInfoUnkown->QueryInterface( IID_ICorProfilerInfo2, (LPVOID*) &profilerInfo);
	if (FAILED(hr) || profilerInfo.p == NULL) {
		return E_INVALIDARG;
	}

	DWORD dwEventMask = getEventMask();
	profilerInfo->SetEventMask(dwEventMask);
	profilerInfo->SetFunctionIDMapper(functionMapper);
	writeProcessInfoToLogFile();
	return S_OK;
}

std::string CProfilerCallback::getEnvironmentVariable(std::string suffix) {
	char value[BUFFER_SIZE];
	std::string name = "COR_PROFILER_" + suffix;
	if (GetEnvironmentVariable(name.c_str(), value, sizeof(value)) == 0) {
		return "";
	}
	return value;
}

void CProfilerCallback::readConfig() {
	std::string configFile = getEnvironmentVariable("CONFIG");
	if (configFile.empty()) {
		configFile = getEnvironmentVariable("PATH") + ".config";
	}
	writeTupleToFile(LOG_KEY_INFO, ("looking for configuration options in: " + configFile).c_str());

	std::ifstream inputStream(configFile);
	this->configOptions = std::map<std::string, std::string>();
	for (std::string line; getline(inputStream, line);) {
		size_t delimiterPosition = line.find("=");
		if (delimiterPosition == std::string::npos) {
			writeTupleToFile(LOG_KEY_WARN, ("invalid line in config file: " + line).c_str());
			continue;
		}

		std::string optionName = line.substr(0, delimiterPosition);
		std::string optionValue = line.substr(delimiterPosition + 1);
		std::transform(optionName.begin(), optionName.end(), optionName.begin(), toupper);
		this->configOptions[optionName] = optionValue;
	}
}

std::string CProfilerCallback::getOption(std::string optionName) {
	std::string value = getEnvironmentVariable(optionName);
	if (!value.empty()) {
		return value;
	}
	return this->configOptions[optionName];
}

void CProfilerCallback::writeProcessInfoToLogFile(){
	appPath[0] = 0;
	appName[0] = 0;
	if (0 == GetModuleFileNameW(NULL, appPath, MAX_PATH)) {
		_wsplitpath_s(appPath, NULL, 0, NULL, 0, appName, _MAX_FNAME, NULL, 0);
	}
	if (appPath[0] == 0) {
		wcscpy_s(appPath, MAX_PATH, L"No Application Path Found");
		wcscpy_s(appName, _MAX_FNAME, L"No Application Name Found");
	}

	char process[BUFFER_SIZE];
	// turn application path from wide to normal character string
	sprintf_s(process, "%S", appPath);
	writeTupleToFile(LOG_KEY_PROCESS, process);
}

void CProfilerCallback::createLogFile() {
	char targetDir[BUFFER_SIZE];
	if (!GetEnvironmentVariable("COR_PROFILER_TARGETDIR", targetDir,
			sizeof(targetDir))) {
		// c:/users/public is usually writable for everyone
		strcpy_s(targetDir, "c:/users/public/");
	}

	char logFileName[BUFFER_SIZE];
	char timeStamp[BUFFER_SIZE];
	getFormattedCurrentTime(timeStamp, sizeof(timeStamp));

	sprintf_s(logFileName, "%s/coverage_%s.txt", targetDir, timeStamp);
	_tcscpy_s(logFilePath, logFileName);

	EnterCriticalSection(&criticalSection);
	logFile = CreateFile(logFilePath, GENERIC_WRITE, FILE_SHARE_READ,
			NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	
	writeTupleToFile(LOG_KEY_INFO, PROFILER_VERSION_INFO);

	writeTupleToFile(LOG_KEY_STARTED, timeStamp);
	LeaveCriticalSection(&criticalSection);
}

void CProfilerCallback::getFormattedCurrentTime(char *result, size_t size) {
	SYSTEMTIME time;
	GetSystemTime (&time);
	// Four digits for milliseconds means we always have a leading 0 there.
	// We consider this legacy and keep it here for compatibility reasons.
	sprintf_s(result, size, "%04d%02d%02d_%02d%02d%02d%04d", time.wYear,
			time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond,
			time.wMilliseconds);
}

HRESULT CProfilerCallback::Shutdown() {
	char buffer[BUFFER_SIZE];

	EnterCriticalSection(&criticalSection);

	// Write inlined methods.
	sprintf_s(buffer, "//%zu methods inlined\r\n", inlinedMethods.size());
	writeToFile(buffer);
	writeFunctionInfosToLog(LOG_KEY_INLINED, &inlinedMethods);

	// Write jitted methods.
	sprintf_s(buffer, "//%zu methods jitted\r\n", jittedMethods.size());
	writeToFile(buffer);
	writeFunctionInfosToLog(LOG_KEY_JITTED, &jittedMethods);

	// Write timestamp.
	char timeStamp[BUFFER_SIZE];

	getFormattedCurrentTime(timeStamp, sizeof(timeStamp));
	writeTupleToFile(LOG_KEY_STOPPED, timeStamp);

	writeTupleToFile(LOG_KEY_INFO, "Shutting down coverage profiler" );

	LeaveCriticalSection(&criticalSection);

	profilerInfo->ForceGC();

	// Close the log file.
	EnterCriticalSection(&criticalSection);
	if(logFile != INVALID_HANDLE_VALUE) {
		CloseHandle(logFile);
	}
	LeaveCriticalSection(&criticalSection);

	return S_OK;
}

DWORD CProfilerCallback::getEventMask() {
	DWORD dwEventMask = 0;
	dwEventMask |= COR_PRF_MONITOR_JIT_COMPILATION;
	dwEventMask |= COR_PRF_MONITOR_ASSEMBLY_LOADS;

	// disable force re-jitting for the light variant
	if (!isLightMode) {
		dwEventMask |= COR_PRF_MONITOR_ENTERLEAVE;
	}

	return dwEventMask;
}

UINT_PTR CProfilerCallback::functionMapper(FunctionID functionId,
		BOOL* pbHookFunction) {
	// Disable hooking of functions.
	*pbHookFunction = false;

	// Always return original function id.
	return functionId;
}

HRESULT CProfilerCallback::JITCompilationFinished(FunctionID functionId,
		HRESULT hrStatus, BOOL fIsSafeToBlock) {
	// Notify monitor that method has been jitted.
	FunctionInfo info;
	getFunctionInfo(functionId, &info);
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
	profilerInfo->GetAssemblyInfo(assemblyId, sizeof(assemblyName),
			&assemblyNameSize, assemblyName, &appDomainId, &moduleId);

	// We need the module info to get the path of the assembly
	LPCBYTE baseLoadAddress;
	WCHAR moduleFileName[BUFFER_SIZE];
	ULONG moduleFileNameSize = 0;
	AssemblyID parentAssembly;
	profilerInfo->GetModuleInfo(moduleId, &baseLoadAddress, sizeof(moduleFileName),
		&moduleFileNameSize, moduleFileName, &parentAssembly);

	// Call GetModuleMetaData to get a MetaDataAssemblyImport object.
	IMetaDataAssemblyImport* pMetaDataAssemblyImport = NULL;
	profilerInfo->GetModuleMetaData(moduleId, ofRead,
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

	char assemblyInfo[BUFFER_SIZE];
	if (getOption("ASSEMBLY_PATHS") == "1") {
		sprintf_s(assemblyInfo, "%S:%i Version:%i.%i.%i.%i Path:%S", assemblyName, assemblyNumber,
			metadata.usMajorVersion, metadata.usMinorVersion,
			metadata.usBuildNumber, metadata.usRevisionNumber, moduleFileName);
	} else {
		sprintf_s(assemblyInfo, "%S:%i Version:%i.%i.%i.%i", assemblyName, assemblyNumber,
			metadata.usMajorVersion, metadata.usMinorVersion,
			metadata.usBuildNumber, metadata.usRevisionNumber);
	}
	writeTupleToFile(LOG_KEY_ASSEMBLY, assemblyInfo);

	// Always return OK
	return S_OK;
}

HRESULT CProfilerCallback::JITInlining(FunctionID callerID, FunctionID calleeId,
		BOOL* pfShouldInline) {
	// Save information about inlined method.
	if (inlinedMethodIds.insert(calleeId).second == true) {
		FunctionInfo info;
		getFunctionInfo(calleeId, &info);
		inlinedMethods.push_back(info);
	}
	// Always allow inlining.
	*pfShouldInline = true;

	// Always return OK
	return S_OK;
}

HRESULT CProfilerCallback::getFunctionInfo(FunctionID functionId,
		FunctionInfo* info) {
	mdToken functionToken = mdTypeDefNil;
	IMetaDataImport* pMDImport = NULL;
	WCHAR functionName[BUFFER_SIZE] = L"UNKNOWN";

	HRESULT hr = profilerInfo->GetTokenAndMetaDataFromFunction(functionId,
			IID_IMetaDataImport, (IUnknown**) &pMDImport, &functionToken);
	if (!SUCCEEDED(hr)) {
		return hr;
	}

	mdTypeDef classToken = mdTypeDefNil;
	DWORD methodAttr = 0;
	PCCOR_SIGNATURE sigBlob = NULL;
	ULONG sigSize = 0;
	ModuleID moduleId = 0;
	hr = pMDImport->GetMethodProps(functionToken, &classToken, functionName,
			sizeof(functionName), 0, &methodAttr, &sigBlob, &sigSize, NULL,
			NULL);
	if (SUCCEEDED(hr)) {
		fillFunctionInfo(info, functionId, functionToken, moduleId);
	}
	
	pMDImport->Release();
	
	return hr;
}

void CProfilerCallback::fillFunctionInfo(FunctionInfo* info, FunctionID functionId, mdToken functionToken, ModuleID moduleId) {
	ClassID classId = 0;
	ULONG32 values = 0;
	HRESULT hr = profilerInfo->GetFunctionInfo2(functionId, 0,
		&classId, &moduleId, &functionToken, 0, &values, NULL);
	if (!SUCCEEDED(hr)) {
		classId = 0;
	}

	int assemblyNumber = -1;
	if (SUCCEEDED(hr) && moduleId != 0) {
		AssemblyID assemblyId;
		hr = profilerInfo->GetModuleInfo(moduleId, NULL, NULL,
			NULL, NULL, &assemblyId);
		if (SUCCEEDED(hr)) {
			assemblyNumber = assemblyMap[assemblyId];
		}
	}

	info->assemblyNumber = assemblyNumber;
	info->functionToken = functionToken;
}

void CProfilerCallback::writeTupleToFile(const char* key, const char* value) {
	char buffer[BUFFER_SIZE];
	sprintf_s(buffer, "%s=%s\r\n", key, value);
	writeToFile(buffer);
}

int CProfilerCallback::writeToFile(const char* string) {
	int retVal = 0;
	DWORD dwWritten = 0;

	if (logFile != INVALID_HANDLE_VALUE) {
		EnterCriticalSection(&criticalSection);
		if (TRUE == WriteFile(logFile, string,
			(DWORD)strlen(string), &dwWritten, NULL)) {
			retVal = dwWritten;
		} else {
			retVal = 0;
		}
		LeaveCriticalSection(&criticalSection);
	}
	
	return retVal;
}

void CProfilerCallback::writeFunctionInfosToLog(const char* key,
		vector<FunctionInfo>* functions) {
	for (vector<FunctionInfo>::iterator i = functions->begin(); i != functions->end();
			i++) {
		FunctionInfo info = *i;
		char signature[BUFFER_SIZE];
		signature[0] = '\0';
		sprintf_s(signature, "%i:%i", info.assemblyNumber,
				info.functionToken);
		writeTupleToFile(key, signature);
	}
}
