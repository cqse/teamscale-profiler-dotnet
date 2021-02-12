#include "CProfilerCallback.h"
#include "version.h"
#include "UploadDaemon.h"
#include "utils/StringUtils.h"
#include "utils/WindowsUtils.h"
#include "utils/Debug.h"
#include <fstream>
#include <algorithm>
#include <winuser.h>

#pragma comment(lib, "version.lib")
#pragma intrinsic(strcmp,labs,strcpy,_rotl,memcmp,strlen,_rotr,memcpy,_lrotl,_strset,memset,_lrotr,abs,strcat)

/**
 * Serializes access to the singleton profiler instance when trying to shut it down.
 * This prevents race conditions that can result in deadlocks that freeze the profiled process.
 */
class ShutdownGuard {
public:
	ShutdownGuard() {
		// we never delete this critical section. This is not needed as it's cleaned up on process
		// death like any other memory
		InitializeCriticalSection(&section);
	}

	virtual ~ShutdownGuard() {}

	void setInstance(CProfilerCallback* callback) {
		instance = callback;
	}

	/**
	 * Shuts down the instance. If clrIsAvailable is true, also tries to force a GC.
	 * Note that forcing a GC after the CLR has shut down can result in deadlocks so this
	 * should be set only when calling from a CLR callback.
	 */
	void shutdownInstance(bool clrIsAvailable) {
		EnterCriticalSection(&section);
		if (instance != NULL) {
			instance->ShutdownOnce(clrIsAvailable);
			instance = NULL;
		}
		LeaveCriticalSection(&section);
	}

private:
	CRITICAL_SECTION section;
	CProfilerCallback* instance = NULL;
};

static ShutdownGuard& getShutdownGuard() {
	// C++ 11 guarantees that this initialization will only happen once and in a thread-safe manner
	static ShutdownGuard instance;
	return instance;
}

void CProfilerCallback::ShutdownFromDllMainDetach() {
	getShutdownGuard().shutdownInstance(false);
}

#ifdef TIA
// TODO move to instance vars.
CProfilerCallback* callbackInstance = NULL;
bool enableFunctionHooks = false;
#endif

CProfilerCallback::CProfilerCallback() {
	try {
		InitializeCriticalSection(&callbackSynchronization);
		getShutdownGuard().setInstance(this);
	}
	catch (...) {
		handleException("Constructor");
	}
}

CProfilerCallback::~CProfilerCallback() {
	try {
		// make sure we flush to disk and disable access to this instance for other threads
		// even if the .NET framework doesn't call Shutdown() itself
		getShutdownGuard().shutdownInstance(false);
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

	if (!config.isProfilingEnabled()) {
		return S_OK;
	}

	// Place the attach log next to the config and profiler dll
	std::string configPath = StringUtils::removeLastPartOfPath(config.getConfigPath());
	attachLog.createLogFile(configPath);
	attachLog.logAttach();

	traceLog.createLogFile(config.getTargetDir());
	traceLog.info("looking for configuration options in: " + config.getConfigPath());

	for (std::string problem : config.getProblems()) {
		traceLog.error(problem);
	}

	if (config.shouldUseLightMode()) {
		traceLog.info("Mode: light");
	}
	else {
		traceLog.info("Mode: force re-jitting");
	}

	traceLog.info("Eagerness: " + std::to_string(config.getEagerness()));

	if (config.shouldStartUploadDaemon()) {
		traceLog.info("Starting upload deamon");
		createDaemon().launch(traceLog);
	}

#ifdef TIA
	if (callbackInstance == NULL) {
		callbackInstance = this;
	}

	if (config.isTiaEnabled()) {
		traceLog.info("TIA enabled. SUB: " + config.getTiaSubscribeSocket() + " REQ: " + config.getTiaRequestSocket());
		enableFunctionHooks = true;
		std::function<void(std::string)> testStartCallback = std::bind(&CProfilerCallback::onTestStart, this, std::placeholders::_1);
		std::function<void(std::string, std::string)> testEndCallback = std::bind(&CProfilerCallback::onTestEnd, this, std::placeholders::_1, std::placeholders::_2);
		std::function<void(std::string)> errorCallback = std::bind(&TraceLog::error, this->traceLog, std::placeholders::_1);
		this->ipc = new Ipc(&this->config, testStartCallback, testEndCallback, errorCallback);
		std::string testName = this->ipc->getCurrentTestName();
		if (!testName.empty()) {
			traceLog.startTestCase(testName);
		}
	}
#endif

	char appPool[BUFFER_SIZE];
	if (GetEnvironmentVariable("APP_POOL_ID", appPool, sizeof(appPool))) {
		std::string message = "IIS AppPool: ";
		message += appPool;
		traceLog.info(message);
	}

	std::string message = "Command Line: ";
	message += GetCommandLine();
	traceLog.info(message);

	if (config.shouldDumpEnvironment()) {
		dumpEnvironment();
	}

	HRESULT hr = pICorProfilerInfoUnkown->QueryInterface(IID_ICorProfilerInfo2, (LPVOID*)&profilerInfo);
	if (FAILED(hr) || profilerInfo.p == NULL) {
		return E_INVALIDARG;
	}

	DWORD dwEventMask = getEventMask();
	profilerInfo->SetEventMask(dwEventMask);
	profilerInfo->SetFunctionIDMapper(&CProfilerCallback::functionMapper);
#ifdef TIA
	// TODO use query v3
	profilerInfo->SetEnterLeaveFunctionHooks2(onFunctionEnterStatic, NULL, NULL);
#endif
	traceLog.logProcess(WindowsUtils::getPathOfThisProcess());

	return S_OK;
}

void CProfilerCallback::dumpEnvironment() {
	std::vector<std::string> environmentVariables = WindowsUtils::listEnvironmentVariables();
	if (environmentVariables.empty()) {
		traceLog.error("Failed to list the environment variables");
		return;
	}

	for (size_t i = 0; i < environmentVariables.size(); i++)
	{
		traceLog.logEnvironmentVariable(environmentVariables.at(i));
	}
}

void CProfilerCallback::initializeConfig() {
	std::string configFile = WindowsUtils::getConfigValueFromEnvironment("CONFIG");

	bool configFileWasManuallySpecified = !configFile.empty();
	if (!configFileWasManuallySpecified) {
		configFile = Config::getDefaultConfigPath();
	}

	config.load(configFile, WindowsUtils::getPathOfThisProcess(), configFileWasManuallySpecified);
}

UploadDaemon CProfilerCallback::createDaemon() {
	std::string profilerPath = StringUtils::removeLastPartOfPath(WindowsUtils::getConfigValueFromEnvironment("PATH"));
	return UploadDaemon(profilerPath);
}

void CProfilerCallback::ShutdownOnce(bool clrIsAvailable) {
	if (!config.isProfilingEnabled()) {
		return;
	}

	EnterCriticalSection(&callbackSynchronization);
	writeFunctionInfosToLog();
	attachLog.logDetach();

	traceLog.shutdown();
	attachLog.shutdown();

#ifdef TIA
	callbackInstance = NULL;
	if (this->ipc != NULL) {
		delete ipc;
		ipc = NULL;
	}
#endif
	if (config.shouldStartUploadDaemon()) {
		createDaemon().notifyShutdown();
	}
	if (clrIsAvailable) {
		profilerInfo->ForceGC();
	}

	LeaveCriticalSection(&callbackSynchronization);
}

HRESULT CProfilerCallback::Shutdown() {
	try {
		getShutdownGuard().shutdownInstance(true);
	}
	catch (...) {
		handleException("Shutdown");
	}
	return S_OK;
}

DWORD CProfilerCallback::getEventMask() {
	DWORD dwEventMask = 0;
	dwEventMask |= COR_PRF_MONITOR_JIT_COMPILATION;
	dwEventMask |= COR_PRF_MONITOR_ASSEMBLY_LOADS;

	// disable force re-jitting for the light variant
	if (!config.shouldUseLightMode()) {
		dwEventMask |= COR_PRF_DISABLE_ALL_NGEN_IMAGES;
	}

#ifdef TIA
	if (config.isTiaEnabled()) {
		dwEventMask |= COR_PRF_MONITOR_ENTERLEAVE;
	}
#endif

	return dwEventMask;
}

UINT_PTR CProfilerCallback::functionMapper(FunctionID functionId, BOOL* pbHookFunction) {
	try {
		// Disable hooking of functions unless in TIA mode
#ifdef TIA
		// FIXME this causes fail: callbackInstance != NULL && callbackInstance->config.isTiaEnabled();
		* pbHookFunction = enableFunctionHooks;
#elif
		* pbHookFunction = false;
#endif

		// Always return original function id.
		return functionId;
	}
	catch (...) {
		Debug::getInstance().logErrorWithStracktrace("functionMapper");
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
	if (!config.isProfilingEnabled()) {
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
	traceLog.logAssembly(assemblyInfo);

	// Always return OK
	return S_OK;
}

int CProfilerCallback::registerAssembly(AssemblyID assemblyId) {
	int assemblyNumber = assemblyCounter;
	assemblyCounter++;
	assemblyMap[assemblyId] = assemblyNumber;
	return assemblyNumber;
}

void CProfilerCallback::getAssemblyInfo(AssemblyID assemblyId, WCHAR* assemblyName, WCHAR* assemblyPath, ASSEMBLYMETADATA* metadata) {
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
	Debug::getInstance().logErrorWithStracktrace(context);
	if (!config.shouldIgnoreExceptions()) {
		throw;
	}
}

HRESULT CProfilerCallback::JITCompilationFinishedImplementation(FunctionID functionId,
	HRESULT hrStatus, BOOL fIsSafeToBlock) {
	if (config.isProfilingEnabled()) {
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
	if (config.isProfilingEnabled()) {
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

	if (config.isTiaEnabled() && info.assemblyNumber == 1) {
		return;
	}

	recordedFunctionInfos->push_back(info);

	if (shouldWriteEagerly()) {
		writeFunctionInfosToLog();
	}
}

inline bool CProfilerCallback::shouldWriteEagerly() {
	// Must be called from synchronized context
	size_t overallCount = inlinedMethods.size() + jittedMethods.size();
#if TIA
	overallCount += calledMethods.size();
#endif
	return config.getEagerness() > 0 && static_cast<int>(overallCount) >= config.getEagerness();
}

void CProfilerCallback::writeFunctionInfosToLog() {
	// Must be called from synchronized context
	traceLog.writeInlinedFunctionInfosToLog(&inlinedMethods);
	inlinedMethods.clear();

	traceLog.writeJittedFunctionInfosToLog(&jittedMethods);
	jittedMethods.clear();

#if TIA
	traceLog.writeCalledFunctionInfosToLog(&calledMethods);
	calledMethods.clear();
#endif
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

#ifdef TIA

void CProfilerCallback::onTestStart(std::string testName)
{
	if (config.isProfilingEnabled() && config.isTiaEnabled()) {
		EnterCriticalSection(&callbackSynchronization);

		writeFunctionInfosToLog();
		traceLog.startTestCase(testName);

		calledMethodIds.clear();

		LeaveCriticalSection(&callbackSynchronization);
	}
}

void CProfilerCallback::onTestEnd(std::string result, std::string message)
{
	if (config.isProfilingEnabled() && config.isTiaEnabled()) {
		EnterCriticalSection(&callbackSynchronization);

		writeFunctionInfosToLog();
		traceLog.endTestCase(result, message);

		calledMethodIds.clear();

		LeaveCriticalSection(&callbackSynchronization);
	}
}

void __stdcall CProfilerCallback::onFunctionEnterStatic(FunctionID functionId, UINT_PTR clientData, COR_PRF_FRAME_INFO func, COR_PRF_FUNCTION_ARGUMENT_INFO* argumentInfo)
{
	if (callbackInstance != NULL) {
		callbackInstance->onFunctionEnter(functionId);
	}
}

void CProfilerCallback::onFunctionEnter(FunctionID functionId)
{
	try {
		if (!config.isProfilingEnabled()) {
			return;
		}

		EnterCriticalSection(&callbackSynchronization);

		if (calledMethodIds.insert(functionId).second == true) {
			recordFunctionInfo(&calledMethods, functionId);
		}

		LeaveCriticalSection(&callbackSynchronization);
	}
	catch (...) {
		handleException("onFunctionEnter");
	}
}

#endif
