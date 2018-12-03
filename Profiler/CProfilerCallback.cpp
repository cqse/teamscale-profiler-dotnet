#include "CProfilerCallback.h"
#include "version.h"
#include "UploadDaemon.h"
#include "StringUtils.h"
#include "WindowsUtils.h"
#include <fstream>
#include <algorithm>
#include <winuser.h>
#include "Debug.h"

#pragma comment(lib, "version.lib")
#pragma intrinsic(strcmp,labs,strcpy,_rotl,memcmp,strlen,_rotr,memcpy,_lrotl,_strset,memset,_lrotr,abs,strcat)

CProfilerCallback::CProfilerCallback() {
	try {
		InitializeCriticalSection(&callbackSynchronization);
	}
	catch (...) {
		handleException("Constructor");
	}
}

CProfilerCallback::~CProfilerCallback() {
	try {
		DeleteCriticalSection(&callbackSynchronization);
	}
	catch (...) {
		handleException("Destructor");
	}
}

HRESULT CProfilerCallback::Initialize(IUnknown* pICorProfilerInfoUnkown) {
	try {
		return InitializeImplementation(pICorProfilerInfoUnkown);
	}
	catch (...) {
		handleException("Initialize");
		return S_OK;
	}
}

HRESULT CProfilerCallback::InitializeImplementation(IUnknown* pICorProfilerInfoUnkown) {
	initializeConfig();
	if (!config.isEnabled()) {
		return S_OK;
	}

	log.createLogFile(config);

	for (std::string problem : config.getProblems()) {
		log.error(problem);
	}

	if (config.shouldUseLightMode()) {
		log.info("Mode: light");
	}
	else {
		log.info("Mode: force re-jitting");
	}

	log.info("Eagerness: " + std::to_string(config.getEagerness()));

	if (config.shouldStartUploadDaemon()) {
		log.info("Starting upload deamon");
		startUploadDeamon();
	}

	char appPool[BUFFER_SIZE];
	if (GetEnvironmentVariable("APP_POOL_ID", appPool, sizeof(appPool))) {
		std::string message = "IIS AppPool: ";
		message += appPool;
		log.info(message);
	}

	std::string message = "Command Line: ";
	message += GetCommandLine();
	log.info(message);

	if (config.shouldDumpEnvironment()) {
		dumpEnvironment();
	}

	HRESULT hr = pICorProfilerInfoUnkown->QueryInterface(IID_ICorProfilerInfo2, (LPVOID*)&profilerInfo);
	if (FAILED(hr) || profilerInfo.p == NULL) {
		return E_INVALIDARG;
	}

	DWORD dwEventMask = getEventMask();
	profilerInfo->SetEventMask(dwEventMask);
	profilerInfo->SetFunctionIDMapper(functionMapper);

	log.logProcess(WindowsUtils::getPathOfThisProcess());

	return S_OK;
}

void CProfilerCallback::dumpEnvironment() {
	std::vector<std::string> environmentVariables = WindowsUtils::listEnvironmentVariables();
	if (environmentVariables.empty()) {
		log.error("Failed to list the environment variables");
		return;
	}

	for (size_t i = 0; i < environmentVariables.size(); i++)
	{
		log.logEnvironmentVariable(environmentVariables.at(i));
	}
}

void CProfilerCallback::startUploadDeamon() {
	std::string profilerPath = StringUtils::removeLastPartOfPath(WindowsUtils::getConfigValueFromEnvironment("PATH"));
	std::string traceDirectory = StringUtils::removeLastPartOfPath(log.getLogFilePath());

	UploadDaemon daemon(profilerPath, traceDirectory, &log);
	daemon.launch();
}

void CProfilerCallback::initializeConfig() {
	std::string configFile = WindowsUtils::getConfigValueFromEnvironment("CONFIG");
	if (configFile.empty()) {
		configFile = WindowsUtils::getConfigValueFromEnvironment("PATH") + ".yml";
	}
	log.info("looking for configuration options in: " + configFile);

	config.load(configFile, WindowsUtils::getPathOfThisProcess());
}

HRESULT CProfilerCallback::ShutdownImplementation() {
	if (!config.isEnabled()) {
		return S_OK;
	}

	EnterCriticalSection(&callbackSynchronization);
	writeFunctionInfosToLog();

	log.shutdown();
	profilerInfo->ForceGC();
	LeaveCriticalSection(&callbackSynchronization);

	return S_OK;
}

HRESULT CProfilerCallback::Shutdown() {
	try {
		return ShutdownImplementation();
	}
	catch (...) {
		handleException("Shutdown");
		return S_OK;
	}
}

DWORD CProfilerCallback::getEventMask() {
	DWORD dwEventMask = 0;
	dwEventMask |= COR_PRF_MONITOR_JIT_COMPILATION;
	dwEventMask |= COR_PRF_MONITOR_ASSEMBLY_LOADS;

	// disable force re-jitting for the light variant
	if (!config.shouldUseLightMode()) {
		dwEventMask |= COR_PRF_MONITOR_ENTERLEAVE;
	}

	return dwEventMask;
}

UINT_PTR CProfilerCallback::functionMapper(FunctionID functionId, BOOL* pbHookFunction) {
	try {
		// Disable hooking of functions.
		*pbHookFunction = false;

		// Always return original function id.
		return functionId;
	}
	catch (...) {
		Debug::logStacktrace("functionMapper");
		// since this function must be static, we have no way to access the config so we always terminate the program.
		throw;
	}
}

HRESULT CProfilerCallback::AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus) {
	try {
		return AssemblyLoadFinishedImplementation(assemblyId, hrStatus);
	}
	catch (...) {
		handleException("AssemblyLoadFinished");
		return S_OK;
	}
}

HRESULT CProfilerCallback::AssemblyLoadFinishedImplementation(AssemblyID assemblyId, HRESULT hrStatus) {
	if (!config.isEnabled()) {
		return S_OK;
	}

	EnterCriticalSection(&callbackSynchronization);

	int assemblyNumber = registerAssembly(assemblyId);

	char assemblyInfo[BUFFER_SIZE];
	int writtenChars = 0;

	WCHAR assemblyName[BUFFER_SIZE];
	WCHAR assemblyPath[BUFFER_SIZE];
	ASSEMBLYMETADATA metadata;
	getAssemblyInfo(assemblyId, assemblyName, assemblyPath, &metadata);

	LeaveCriticalSection(&callbackSynchronization);

	// Log assembly load.
	writtenChars += sprintf_s(assemblyInfo + writtenChars, BUFFER_SIZE - writtenChars, "%S:%i",
		assemblyName, assemblyNumber);

	writtenChars += sprintf_s(assemblyInfo + writtenChars, BUFFER_SIZE - writtenChars, " Version:%i.%i.%i.%i",
		metadata.usMajorVersion, metadata.usMinorVersion, metadata.usBuildNumber, metadata.usRevisionNumber);

	if (config.shouldLogAssemblyFileVersion()) {
		writtenChars += writeFileVersionInfo(assemblyPath, assemblyInfo + writtenChars, BUFFER_SIZE - writtenChars);
	}

	if (config.shouldLogAssemblyPaths()) {
		writtenChars += sprintf_s(assemblyInfo + writtenChars, BUFFER_SIZE - writtenChars, " Path:%S", assemblyPath);
	}
	log.logAssembly(assemblyInfo);

	// Always return OK
	return S_OK;
}

int CProfilerCallback::registerAssembly(AssemblyID assemblyId) {
	int assemblyNumber = assemblyCounter;
	assemblyCounter++;
	assemblyMap[assemblyId] = assemblyNumber;
	return assemblyNumber;
}

void CProfilerCallback::getAssemblyInfo(AssemblyID assemblyId, WCHAR *assemblyName, WCHAR *assemblyPath, ASSEMBLYMETADATA *metadata) {
	ULONG assemblyNameSize = 0;
	AppDomainID appDomainId = 0;
	ModuleID moduleId = 0;
	profilerInfo->GetAssemblyInfo(assemblyId, BUFFER_SIZE,
		&assemblyNameSize, assemblyName, &appDomainId, &moduleId);

	// We need the module info to get the path of the assembly
	LPCBYTE baseLoadAddress;
	ULONG assemblyPathSize = 0;
	AssemblyID parentAssembly;
	profilerInfo->GetModuleInfo(moduleId, &baseLoadAddress, BUFFER_SIZE,
		&assemblyPathSize, assemblyPath, &parentAssembly);

	// Call GetModuleMetaData to get a MetaDataAssemblyImport object.
	IMetaDataAssemblyImport* pMetaDataAssemblyImport = NULL;
	profilerInfo->GetModuleMetaData(moduleId, ofRead,
		IID_IMetaDataAssemblyImport, (IUnknown**)&pMetaDataAssemblyImport);

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
	// We have to explicitly set these to NULL, otherwise the .NET framework will try
	// to access these pointers at a later time and crash, because they are not
	// valid. This happened when we started an application multiple times on
	// the same machine in rapid succession.
	metadata->szLocale = NULL;
	metadata->rProcessor = NULL;
	metadata->rOS = NULL;

	pMetaDataAssemblyImport->GetAssemblyProps(ptkAssembly, NULL, NULL, NULL,
		NULL, 0, NULL, metadata, NULL);
}

HRESULT CProfilerCallback::JITCompilationFinished(FunctionID functionId,
	HRESULT hrStatus, BOOL fIsSafeToBlock) {
	try {
		return JITCompilationFinishedImplementation(functionId, hrStatus, fIsSafeToBlock);
	}
	catch (...) {
		handleException("JITCompilationFinished");
		return S_OK;
	}
}

void CProfilerCallback::handleException(std::string context) {
	Debug::logStacktrace(context);
	if (!config.shouldIgnoreExceptions()) {
		throw;
	}
}

HRESULT CProfilerCallback::JITCompilationFinishedImplementation(FunctionID functionId,
	HRESULT hrStatus, BOOL fIsSafeToBlock) {
	if (config.isEnabled()) {
		EnterCriticalSection(&callbackSynchronization);

		recordFunctionInfo(&jittedMethods, functionId);

		LeaveCriticalSection(&callbackSynchronization);
	}
	return S_OK;
}

HRESULT CProfilerCallback::JITInlining(FunctionID callerId, FunctionID calleeId,
	BOOL* pfShouldInline) {
	try {
		return JITInliningImplementation(callerId, calleeId, pfShouldInline);
	}
	catch (...) {
		handleException("JITInlining");
		return S_OK;
	}
}

HRESULT CProfilerCallback::JITInliningImplementation(FunctionID callerId, FunctionID calleeId,
	BOOL* pfShouldInline) {
	if (config.isEnabled()) {
		// Save information about inlined method (if not already seen)
		EnterCriticalSection(&callbackSynchronization);

		if (inlinedMethodIds.insert(calleeId).second == true) {
			recordFunctionInfo(&inlinedMethods, calleeId);
		}

		LeaveCriticalSection(&callbackSynchronization);
	}

	// Always allow inlining.
	*pfShouldInline = true;

	return S_OK;
}

void CProfilerCallback::recordFunctionInfo(std::vector<FunctionInfo>* recordedFunctionInfos, FunctionID calleeId) {
	// Must be called from synchronized context

	FunctionInfo info;
	getFunctionInfo(calleeId, &info);

	recordedFunctionInfos->push_back(info);

	if (shouldWriteEagerly()) {
		writeFunctionInfosToLog();
	}
}

inline bool CProfilerCallback::shouldWriteEagerly() {
	// Must be called from synchronized context
	return config.getEagerness() > 0 && static_cast<int>(inlinedMethods.size() + jittedMethods.size()) >= config.getEagerness();
}

void CProfilerCallback::writeFunctionInfosToLog() {
	// Must be called from synchronized context
	log.writeInlinedFunctionInfosToLog(&inlinedMethods);
	inlinedMethods.clear();

	log.writeJittedFunctionInfosToLog(&jittedMethods);
	jittedMethods.clear();
}

HRESULT CProfilerCallback::getFunctionInfo(FunctionID functionId, FunctionInfo* info) {
	ModuleID moduleId = 0;
	HRESULT hr = profilerInfo->GetFunctionInfo2(functionId, 0,
		NULL, &moduleId, &info->functionToken, 0, NULL, NULL);

	if (SUCCEEDED(hr) && moduleId != 0) {
		AssemblyID assemblyId;
		hr = profilerInfo->GetModuleInfo(moduleId, NULL, NULL,
			NULL, NULL, &assemblyId);
		if (SUCCEEDED(hr)) {
			info->assemblyNumber = assemblyMap[assemblyId];
		}
	}

	return hr;
}

int CProfilerCallback::writeFileVersionInfo(LPCWSTR assemblyPath, char* buffer, size_t bufferSize) {
	DWORD infoSize = GetFileVersionInfoSizeW(assemblyPath, NULL);
	if (!infoSize) {
		return 0;
	}

	BYTE* versionInfo = new BYTE[infoSize];
	if (!GetFileVersionInfoW(assemblyPath, NULL, infoSize, versionInfo)) {
		return 0;
	}

	VS_FIXEDFILEINFO* fileInfo = NULL;
	UINT fileInfoLength = 0;
	if (!VerQueryValueW(versionInfo, L"\\", (void**)&fileInfo, &fileInfoLength)) {
		return 0;
	}

	int	 version[4];
	version[0] = HIWORD(fileInfo->dwFileVersionMS);
	version[1] = LOWORD(fileInfo->dwFileVersionMS);
	version[2] = HIWORD(fileInfo->dwFileVersionLS);
	version[3] = LOWORD(fileInfo->dwFileVersionLS);

	int writtenChars = sprintf_s(buffer, bufferSize, " FileVersion:%i.%i.%i.%i",
		version[0], version[1], version[2], version[3]);

	version[0] = HIWORD(fileInfo->dwProductVersionMS);
	version[1] = LOWORD(fileInfo->dwProductVersionMS);
	version[2] = HIWORD(fileInfo->dwProductVersionLS);
	version[3] = LOWORD(fileInfo->dwProductVersionLS);

	writtenChars += sprintf_s(buffer + writtenChars, bufferSize - writtenChars, " ProductVersion:%i.%i.%i.%i",
		version[0], version[1], version[2], version[3]);

	delete versionInfo;
	return writtenChars;
}