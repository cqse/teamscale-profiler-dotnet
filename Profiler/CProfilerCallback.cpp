#include "CProfilerCallback.h"
#include "version.h"
#include "Uploader.h"
#include "FileSystemUtils.cpp"
#include <fstream>
#include <algorithm>
#include <winuser.h>

using namespace std;

#pragma comment(lib, "version.lib")
#pragma intrinsic(strcmp,labs,strcpy,_rotl,memcmp,strlen,_rotr,memcpy,_lrotl,_strset,memset,_lrotr,abs,strcat)

CProfilerCallback::CProfilerCallback() {
	// nothing to do
}

CProfilerCallback::~CProfilerCallback() {
	// nothing to do
}

/** Whether the given value ends with the given suffix. */
inline bool endsWith(std::string const & value, std::string const & suffix)
{
	if (suffix.size() > value.size()) {
		return false;
	}
	return std::equal(suffix.rbegin(), suffix.rend(), value.rbegin());
}

HRESULT CProfilerCallback::Initialize(IUnknown* pICorProfilerInfoUnkown) {
	std::string process = getProcessInfo();
	std::string processToProfile = getConfigValueFromEnvironment("PROCESS");
	std::transform(process.begin(), process.end(), process.begin(), toupper);
	std::transform(processToProfile.begin(), processToProfile.end(), processToProfile.begin(), toupper);

	isProfilingEnabled = processToProfile.empty() || endsWith(process, processToProfile);
	if (!isProfilingEnabled) {
		return S_OK;
	}

	log.createLogFile();
	readConfig();

	if (getOption("LIGHT_MODE") == "1") {
		isLightMode = true;
		log.info("Mode: light");
	}
	else {
		log.info("Mode: force re-jitting");
	}

	if (getOption("EAGER_MODE") == "1") {
		isEagerMode = true;
		log.info("Mode: eager");
	}
	else {
		log.info("Mode: lazy");
	}

	if (getOption("UPLOAD") == "1") {
		startUpload();
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

	HRESULT hr = pICorProfilerInfoUnkown->QueryInterface(IID_ICorProfilerInfo2, (LPVOID*)&profilerInfo);
	if (FAILED(hr) || profilerInfo.p == NULL) {
		return E_INVALIDARG;
	}

	DWORD dwEventMask = getEventMask();
	profilerInfo->SetEventMask(dwEventMask);
	profilerInfo->SetFunctionIDMapper(functionMapper);

	log.logProcess(getProcessInfo());

	return S_OK;
}

void CProfilerCallback::startUpload() {
	std::string uploaderPath = FileSystemUtils::removeLastPartOfPath(getConfigValueFromEnvironment("PATH"));
	std::string traceDirectory = FileSystemUtils::removeLastPartOfPath(log.getLogFilePath());

	Uploader uploader(uploaderPath, traceDirectory, &log);
	uploader.launch();
}

std::string CProfilerCallback::getConfigValueFromEnvironment(std::string suffix) {
	char value[BUFFER_SIZE];
	std::string name = "COR_PROFILER_" + suffix;
	if (GetEnvironmentVariable(name.c_str(), value, sizeof(value)) == 0) {
		return "";
	}
	return value;
}

void CProfilerCallback::readConfig() {
	std::string configFile = getConfigValueFromEnvironment("CONFIG");
	if (configFile.empty()) {
		configFile = getConfigValueFromEnvironment("PATH") + ".config";
	}
	log.info("looking for configuration options in: " + configFile);

	std::ifstream inputStream(configFile);
	this->configOptions = std::map<std::string, std::string>();
	for (std::string line; getline(inputStream, line);) {
		size_t delimiterPosition = line.find("=");
		if (delimiterPosition == std::string::npos) {
			log.warn("invalid line in config file: " + line);
			continue;
		}

		std::string optionName = line.substr(0, delimiterPosition);
		std::string optionValue = line.substr(delimiterPosition + 1);
		std::transform(optionName.begin(), optionName.end(), optionName.begin(), toupper);
		this->configOptions[optionName] = optionValue;
	}
}

std::string CProfilerCallback::getOption(std::string optionName) {
	std::string value = getConfigValueFromEnvironment(optionName);
	if (!value.empty()) {
		return value;
	}
	return this->configOptions[optionName];
}

std::string CProfilerCallback::getProcessInfo() {
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
	return process;
}

HRESULT CProfilerCallback::Shutdown() {
	if (!isProfilingEnabled) {
		return S_OK;
	}

	if (!isEagerMode) {
		char buffer[BUFFER_SIZE];

		// Write inlined methods.
		sprintf_s(buffer, "%zu methods inlined", inlinedMethods.size());
		log.info(buffer);
		log.writeInlinedFunctionInfosToLog(&inlinedMethods);

		// Write jitted methods.
		sprintf_s(buffer, "%zu methods jitted", jittedMethods.size());
		log.info(buffer);
		log.writeJittedFunctionInfosToLog(&jittedMethods);
	}

	log.shutdown();
	profilerInfo->ForceGC();

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

HRESULT CProfilerCallback::AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus) {
	if (!isProfilingEnabled) {
		return S_OK;
	}

	int assemblyNumber = registerAssembly(assemblyId);

	char assemblyInfo[BUFFER_SIZE];
	int writtenChars = 0;

	WCHAR assemblyName[BUFFER_SIZE];
	WCHAR assemblyPath[BUFFER_SIZE];
	ASSEMBLYMETADATA metadata;
	getAssemblyInfo(assemblyId, assemblyName, assemblyPath, &metadata);

	// Log assembly load.
	writtenChars += sprintf_s(assemblyInfo + writtenChars, BUFFER_SIZE - writtenChars, "%S:%i",
		assemblyName, assemblyNumber);

	writtenChars += sprintf_s(assemblyInfo + writtenChars, BUFFER_SIZE - writtenChars, " Version:%i.%i.%i.%i",
		metadata.usMajorVersion, metadata.usMinorVersion, metadata.usBuildNumber, metadata.usRevisionNumber);

	if (getOption("ASSEMBLY_FILEVERSION") == "1") {
		writtenChars += writeFileVersionInfo(assemblyPath, assemblyInfo + writtenChars, BUFFER_SIZE - writtenChars);
	}

	if (getOption("ASSEMBLY_PATHS") == "1") {
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

HRESULT CProfilerCallback::JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) {
	if (!isProfilingEnabled) {
		return S_OK;
	}

	FunctionInfo info;
	getFunctionInfo(functionId, &info);

	if (isEagerMode) {
		std::vector<FunctionInfo> infoList = { info };
		log.writeJittedFunctionInfosToLog(&infoList);
	}
	else {
		// Notify monitor that method has been jitted.
		jittedMethods.push_back(info);
	}

	// Always return OK
	return S_OK;
}

HRESULT CProfilerCallback::JITInlining(FunctionID callerID, FunctionID calleeId, BOOL* pfShouldInline) {
	if (!isProfilingEnabled) {
		return S_OK;
	}

	FunctionInfo info;
	getFunctionInfo(calleeId, &info);

	if (isEagerMode) {
		std::vector<FunctionInfo> infoList = { info };
		log.writeInlinedFunctionInfosToLog(&infoList);
	}
	else {
		// Save information about inlined method.
		if (inlinedMethodIds.insert(calleeId).second == true) {
			inlinedMethods.push_back(info);
		}
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
		IID_IMetaDataImport, (IUnknown**)&pMDImport, &functionToken);
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