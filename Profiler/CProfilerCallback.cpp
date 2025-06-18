#include "CProfilerCallback.h"
#include "version.h"
#include "UploadDaemon.h"
#include "utils/StringUtils.h"
#include "utils/WindowsUtils.h"
#include "utils/Debug.h"
#include <fstream>
#include <algorithm>
#include <winuser.h>
#include <iostream>
#include <utils/MethodEnter.h>

#pragma comment(lib, "version.lib")
#pragma intrinsic(strcmp,labs,strcpy,_rotl,memcmp,strlen,_rotr,memcpy,_lrotl,_strset,memset,_lrotr,abs,strcat)

namespace Profiler {

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

		~ShutdownGuard() = default;

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
			if (instance != nullptr) {
				try {
					instance->ShutdownOnce(clrIsAvailable);
				}
				catch (...) {
					Debug::getInstance().logErrorWithStracktrace("Shutdown was interrupted.");
				}
				instance = nullptr;
			}
			LeaveCriticalSection(&section);
		}

	private:
		CRITICAL_SECTION section;
		CProfilerCallback* instance = nullptr;
	};

	static ShutdownGuard& getShutdownGuard() {
		// C++ 11 guarantees that this initialization will only happen once and in a thread-safe manner
		static ShutdownGuard instance;
		return instance;
	}

	void CProfilerCallback::ShutdownFromDllMainDetach() {
		getShutdownGuard().shutdownInstance(false);
	}

	CProfilerCallback::CProfilerCallback() {
		try {
			InitializeCriticalSection(&methodSetSynchronization);
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
			DeleteCriticalSection(&methodSetSynchronization);
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
		for (const std::string& problem : config.getProblems()) {
			traceLog.error(problem);
			std::cerr << problem;
		}
		if (!config.getProblems().empty()) {
			WindowsUtils::reportError("Error when loading configuration file", "Couldn't load Profiler.yml configuration for the Teamscale .NET Profiler! See related errors in the standard error stream or in the log file.");
			// If configuration was incorrect, make it visible to the user by closing the application
			exit(-1);
		}

		if (config.shouldUseLightMode()) {
			traceLog.info("Mode: light");
		}
		else {
			traceLog.info("Mode: force re-jitting");
		}

		traceLog.info("Eagerness: " + std::to_string(config.getEagerness()));

		if (config.shouldStartUploadDaemon()) {
			traceLog.info("Starting upload daemon");
			createDaemon().launch(traceLog);
		}

		if (config.isTiaEnabled()) {
			traceLog.info("TIA enabled. REQ Socket: " + config.getTiaRequestSocket());
			std::function<void(std::string)> testStartCallback = [this](std::string testName) {
				this->onTestStart(testName);
				};
			std::function<void(std::string, std::string)> testEndCallback = [this](std::string result, std::string duration) {
				this->onTestEnd(result, duration);
				};
			std::function<void(std::string)> errorCallback = [this](std::string message) {
				this->traceLog.error(message);
				};
			this->ipc = std::make_unique<Ipc>(&this->config, testStartCallback, testEndCallback, errorCallback);

			setCriticalSection(&methodSetSynchronization);
			setCalledMethodsSet(&calledMethodIds);
		}

		std::array<char, BUFFER_SIZE> appPool;
		if (GetEnvironmentVariable("APP_POOL_ID", appPool.data(), static_cast<DWORD>(appPool.size()))) {
			std::string message = std::string("IIS AppPool: ") + appPool.data();
			traceLog.info(message);
		}

		std::string message = std::string("Command Line: ") + GetCommandLine();
		traceLog.info(message);

		if (config.shouldDumpEnvironment()) {
			dumpEnvironment();
		}

		HRESULT hr = pICorProfilerInfoUnkown->QueryInterface(IID_ICorProfilerInfo3, reinterpret_cast<LPVOID*>(&profilerInfo));
		if (FAILED(hr) || profilerInfo.p == nullptr) {
			return E_INVALIDARG;
		}

		adjustEventMask();
		if (config.isTiaEnabled()) {
			profilerInfo->SetEnterLeaveFunctionHooks3((FunctionEnter3*)&FnEnterCallback, nullptr, nullptr);
		}
		traceLog.logProcess(WindowsUtils::getPathOfThisProcess());

		return S_OK;
	}

	void CProfilerCallback::dumpEnvironment() {
		const std::vector<std::string> environmentVariables = WindowsUtils::listEnvironmentVariables();
		if (environmentVariables.empty()) {
			traceLog.error("Failed to list the environment variables");
			return;
		}

		for (const std::string envVariable : environmentVariables)
		{
			traceLog.logEnvironmentVariable(envVariable);
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
		std::string profilerPath = StringUtils::removeLastPartOfPath(WindowsUtils::getPathOfProfiler());
		return UploadDaemon(profilerPath);
	}

	void CProfilerCallback::ShutdownOnce(bool clrIsAvailable) {
		if (!config.isProfilingEnabled()) {
			return;
		}
		EnterCriticalSection(&callbackSynchronization);
		EnterCriticalSection(&methodSetSynchronization);
		writeFunctionInfosToLog();
		LeaveCriticalSection(&methodSetSynchronization);
		LeaveCriticalSection(&callbackSynchronization);
		attachLog.logDetach();

		traceLog.shutdown();
		attachLog.shutdown();
		if (ipc != nullptr) {
			ipc.reset();
		}

		if (config.shouldStartUploadDaemon()) {
			createDaemon().notifyShutdown();
		}
		if (clrIsAvailable) {
			profilerInfo->ForceGC();
		}
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

	void CProfilerCallback::adjustEventMask() {
		DWORD dwEventMaskLow;
		DWORD dwEventMaskHigh;
		profilerInfo->GetEventMask2(&dwEventMaskLow, &dwEventMaskHigh);
		dwEventMaskLow |= COR_PRF_MONITOR_ASSEMBLY_LOADS;

		if (config.isTgaEnabled()) {
			dwEventMaskLow |= COR_PRF_MONITOR_JIT_COMPILATION;
			// disable force re-jitting for the light variant
			if (!config.shouldUseLightMode()) {
				dwEventMaskLow |= COR_PRF_DISABLE_ALL_NGEN_IMAGES;
			}
		}

		if (config.isTiaEnabled()) {
			dwEventMaskLow |= COR_PRF_MONITOR_ENTERLEAVE;
			dwEventMaskLow |= COR_PRF_DISABLE_INLINING;
		}

		profilerInfo->SetEventMask2(dwEventMaskLow, dwEventMaskHigh);
	}

	HRESULT CProfilerCallback::AssemblyLoadFinished(AssemblyID assemblyId, HRESULT) {
		try {
			return AssemblyLoadFinishedImplementation(assemblyId);
		}
		catch (...) {
			handleException("AssemblyLoadFinished");
			return S_OK;
		}
	}

	HRESULT CProfilerCallback::AssemblyLoadFinishedImplementation(AssemblyID assemblyId) {
		if (!config.isProfilingEnabled()) {
			return S_OK;
		}
		EnterCriticalSection(&callbackSynchronization);

		int assemblyNumber = registerAssembly(assemblyId);

		std::array<WCHAR, BUFFER_SIZE> assemblyName;
		std::array<WCHAR, BUFFER_SIZE> assemblyPath;
		ASSEMBLYMETADATA metadata;
		getAssemblyInfo(assemblyId, assemblyName.data(), assemblyPath.data(), &metadata);

		LeaveCriticalSection(&callbackSynchronization);

		std::wostringstream out;

		out << assemblyName.data() << ":" << assemblyNumber
			<< " Version:"
			<< metadata.usMajorVersion << "."
			<< metadata.usMinorVersion << "."
			<< metadata.usBuildNumber << "."
			<< metadata.usRevisionNumber;

		if (config.shouldLogAssemblyFileVersion()) {
			writeFileVersionInfo(assemblyName.data(), out);
		}

		if (config.shouldLogAssemblyPaths()) {
			out << " Path:" << assemblyPath.data();
		}
		traceLog.logAssembly(out.str());

		// Always return OK
		return S_OK;
	}

	int CProfilerCallback::registerAssembly(AssemblyID assemblyId) {
		int assemblyNumber = assemblyCounter++;
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
		IMetaDataAssemblyImport* pMetaDataAssemblyImport = nullptr;
		profilerInfo->GetModuleMetaData(moduleId, ofRead,
			IID_IMetaDataAssemblyImport, (IUnknown**)&pMetaDataAssemblyImport);

		// Get the assembly token.
		mdAssembly ptkAssembly = 0;
		pMetaDataAssemblyImport->GetAssemblyFromScope(&ptkAssembly);

		// Call GetAssemblyProps:
		// Allocate memory for the pointers, as GetAssemblyProps seems to
		// dereference the pointers (it crashed otherwise). We allocate the minimum
		// amount of memory as we do not know how much is needed to store the array.
		// This information would be available in the fields metadata.cbLocale,
		// metadata.ulProcessor and metadata.ulOS after the call to
		// GetAssemblyProps. However, we do not need the additional data and save
		// the second call to GetAssemblyProps with the correct amount of memory.
		// We have to explicitly set these to null, otherwise the .NET framework will try
		// to access these pointers at a later time and crash, because they are not
		// valid. This happened when we started an application multiple times on
		// the same machine in rapid succession.
		metadata->szLocale = nullptr;
		metadata->rProcessor = nullptr;
		metadata->rOS = nullptr;

		pMetaDataAssemblyImport->GetAssemblyProps(ptkAssembly, nullptr, nullptr, nullptr,
			nullptr, 0, nullptr, metadata, nullptr);
	}

	HRESULT CProfilerCallback::JITCompilationFinished(FunctionID functionId, HRESULT, BOOL) {
		try {
			return JITCompilationFinishedImplementation(functionId);
		}
		catch (...) {
			handleException("JITCompilationFinished");
			return S_OK;
		}
	}

	void CProfilerCallback::handleException(const std::string& context) {
		Debug::getInstance().logErrorWithStracktrace(context);
		if (!config.shouldIgnoreExceptions()) {
			std::exit(1);
		}
	}

	HRESULT CProfilerCallback::JITCompilationFinishedImplementation(FunctionID functionId) {
		if (config.isProfilingEnabled() && config.isTgaEnabled()) {
			EnterCriticalSection(&callbackSynchronization);
			EnterCriticalSection(&methodSetSynchronization);
			recordFunctionInfo(jittedMethods, functionId);
			if (shouldWriteEagerly()) {
				writeFunctionInfosToLog();
			}
			LeaveCriticalSection(&methodSetSynchronization);
			LeaveCriticalSection(&callbackSynchronization);
		}
		return S_OK;
	}

	HRESULT CProfilerCallback::JITInlining(FunctionID, FunctionID calleeId, BOOL* pfShouldInline) {
		try {
			return JITInliningImplementation(calleeId, pfShouldInline);
		}
		catch (...) {
			handleException("JITInlining");
			return S_OK;
		}
	}

	HRESULT CProfilerCallback::JITInliningImplementation(FunctionID calleeId, BOOL* pfShouldInline) {
		if (config.isProfilingEnabled() && config.isTgaEnabled()) {
			// Save information about inlined method (if not already seen)

			if (!inlinedMethodIds.contains(calleeId)) {
				EnterCriticalSection(&callbackSynchronization);
				EnterCriticalSection(&methodSetSynchronization);
				inlinedMethodIds.insert(calleeId);
				recordFunctionInfo(inlinedMethods, calleeId);
				if (shouldWriteEagerly()) {
					writeFunctionInfosToLog();
				}
				LeaveCriticalSection(&methodSetSynchronization);
				LeaveCriticalSection(&callbackSynchronization);
			}
		}

		// Always allow inlining.
		*pfShouldInline = true;

		return S_OK;
	}

	void CProfilerCallback::recordFunctionInfo(std::vector<FunctionInfo>& recordedFunctionInfos, FunctionID calleeId) {
		// Must be called from synchronized context

		FunctionInfo info;
		getFunctionInfo(calleeId, info);

		if (config.isTiaEnabled() && info.assemblyNumber == 1) {
			return;
		}

		recordedFunctionInfos.push_back(info);
	}

	inline bool CProfilerCallback::shouldWriteEagerly() {
		// Must be called from synchronized context
		size_t overallCount = inlinedMethods.size() + jittedMethods.size();
		overallCount += calledMethods.size();
		return config.getEagerness() > 0 && static_cast<int>(overallCount) >= config.getEagerness();
	}

	void CProfilerCallback::writeFunctionInfosToLog() {
		// Must be called from synchronized context
		if (config.isTgaEnabled()) {
			traceLog.writeInlinedFunctionInfosToLog(inlinedMethods);
			inlinedMethods.clear();

			traceLog.writeJittedFunctionInfosToLog(jittedMethods);
			jittedMethods.clear();
		}

		if (config.isTiaEnabled()) {
			for (unsigned int i = 0; i < calledMethodIds.size(); i++) {
				FunctionID value = calledMethodIds.at(i);
				if (value != 0) {
					recordFunctionInfo(calledMethods, value);
				}
			}

			calledMethodIds.clear();
			traceLog.writeCalledFunctionInfosToLog(calledMethods);
			calledMethods.clear();
		}
	}

	HRESULT CProfilerCallback::getFunctionInfo(const FunctionID functionId, FunctionInfo& info) {
		ModuleID moduleId = 0;
		HRESULT hr = profilerInfo->GetFunctionInfo2(functionId, 0,
			nullptr, &moduleId, &info.functionToken, 0, nullptr, nullptr);

		if (SUCCEEDED(hr) && moduleId != 0) {
			AssemblyID assemblyId;
			hr = profilerInfo->GetModuleInfo(moduleId, nullptr, 0L,
				nullptr, nullptr, &assemblyId);
			if (SUCCEEDED(hr)) {
				info.assemblyNumber = assemblyMap[assemblyId];
			}
		}

		return hr;
	}

	void CProfilerCallback::writeFileVersionInfo(const LPCWSTR assemblyPath, std::wostringstream& out) {
		DWORD infoSize = GetFileVersionInfoSizeW(assemblyPath, nullptr);
		if (!infoSize) {
			return;
		}

		std::unique_ptr<BYTE[]> versionInfo{ new BYTE[infoSize] };
		if (!GetFileVersionInfoW(assemblyPath, 0L, infoSize, versionInfo.get())) {
			return;
		}

		VS_FIXEDFILEINFO* fileInfo = nullptr;
		UINT fileInfoLength = 0;
		if (!VerQueryValueW(versionInfo.get(), L"\\", (void**)&fileInfo, &fileInfoLength)) {
			return;
		}
		if (fileInfo == nullptr) {
			return;
		}

		std::array<int, 4> version;
		version[0] = HIWORD(fileInfo->dwFileVersionMS);
		version[1] = LOWORD(fileInfo->dwFileVersionMS);
		version[2] = HIWORD(fileInfo->dwFileVersionLS);
		version[3] = LOWORD(fileInfo->dwFileVersionLS);

		out << " FileVersion:"
			<< version[0] << "."
			<< version[1] << "."
			<< version[2] << "."
			<< version[3];

		version[0] = HIWORD(fileInfo->dwProductVersionMS);
		version[1] = LOWORD(fileInfo->dwProductVersionMS);
		version[2] = HIWORD(fileInfo->dwProductVersionLS);
		version[3] = LOWORD(fileInfo->dwProductVersionLS);

		out << " ProductVersion:"
			<< version[0] << "."
			<< version[1] << "."
			<< version[2] << "."
			<< version[3];
	}

	void CProfilerCallback::onTestStart(const std::string& testName)
	{
		if (config.isProfilingEnabled() && config.isTiaEnabled()) {
			EnterCriticalSection(&methodSetSynchronization);
			writeFunctionInfosToLog();

			traceLog.startTestCase(testName);
			if (!testName.empty()) {
				setTestCaseRecording(true);
			}
			LeaveCriticalSection(&methodSetSynchronization);
		}
	}

	void CProfilerCallback::onTestEnd(const std::string& result, const std::string& duration)
	{
		if (config.isProfilingEnabled() && config.isTiaEnabled()) {
			EnterCriticalSection(&methodSetSynchronization);
			setTestCaseRecording(false);
			writeFunctionInfosToLog();
			traceLog.endTestCase(result, duration);

			LeaveCriticalSection(&methodSetSynchronization);
		}
	}

}

