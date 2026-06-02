#pragma once
#include "CProfilerCallbackBase.h"
#include "FunctionInfo.h"
#include "log/TraceLog.h"
#include "log/AttachLog.h"
#include "config/Config.h"
#include "utils/WindowsUtils.h"
#include <atlbase.h>
#include <string>
#include <vector>
#include <map>
#include <utils/FunctionIdSet/FunctionIdSet.h>
#include "UploadDaemon.h"
#include "utils/Ipc.h"
/**
 * Coverage profiler class. Implements JIT event hooks to record method
 * coverage.
 */

namespace Profiler {
	class CProfilerCallback : public CProfilerCallbackBase {
	public:

		/** Shuts down the profiler from the DllMain function on Dll detach if it is still running. */
		static void ShutdownFromDllMainDetach();

		/** Constructor. */
		CProfilerCallback();

		/** Destructor. */
		~CProfilerCallback() override;

		/** Initializer. Called at profiler startup. */
		STDMETHOD(Initialize)(IUnknown* pICorProfilerInfoUnk);

		/** Write coverage information to log file at shutdown. */
		STDMETHOD(Shutdown)();

		/** Store information about jitted method. */
		STDMETHOD(JITCompilationFinished)(FunctionID functionID, HRESULT hrStatus, BOOL fIsSafeToBlock);

		/** Write loaded assembly to log file. */
		STDMETHOD(AssemblyLoadFinished)(AssemblyID assemblyID, HRESULT hrStatus);

		/** Record inlining of method, but generally allow it. */
		STDMETHOD(JITInlining)(FunctionID callerID, FunctionID calleeID, BOOL* pfShouldInline);

		/**
		 * Implements the actual shutdown procedure. Must only be called once.
		 * If clrIsAvailable is true, also tries to force a GC.
		 * Note that forcing a GC after the CLR has shut down can result in deadlocks so this
		 * should be set only when calling from a CLR callback.
		 */
		void CProfilerCallback::ShutdownOnce(bool clrIsAvailable);

	private:
		/** Synchronizes profiling callbacks. */
		CRITICAL_SECTION callbackSynchronization;

		CRITICAL_SECTION methodSetSynchronization;

		/** Default size for arrays. */
		static const int BUFFER_SIZE = 2048;

		/** Counts the number of assemblies loaded. */
		int assemblyCounter = 1;

		bool isTestCaseRecording = false;
		CProfilerCallback* callbackInstance = nullptr;

		Config config = Config(WindowsUtils::getConfigValueFromEnvironment);

		/**
		 * Maps from assembly IDs to assemblyNumbers (determined by assemblyCounter).
		 * It is used to identify the declaring assembly for functions.
		 */
		std::map<AssemblyID, int> assemblyMap;

		/**
		 * Info object that keeps track of jitted methods.
		 */
		std::vector<FunctionInfo> jittedMethods;

		/**
		 * Keeps track of inlined methods.
		 * We use the set to efficiently determine if we already noticed an inlined method.
		 */
		FunctionIdSet inlinedMethodIds;

		/**
		 * Keeps track of inlined methods.
		 * We use the vector to uniquely store the information about inlined methods.
		 */
		std::vector<FunctionInfo> inlinedMethods;

		/** Smart pointer to the .NET framework profiler info. */
		CComQIPtr<ICorProfilerInfo8> profilerInfo;

		/** The log to write all results and messages to. */
		TraceLog traceLog;

		/** The log to write attach and detatch events to */
		AttachLog attachLog;

		/** Inter-process connection for TIA communication. null if not in TIA mode. */
		std::unique_ptr<Ipc> ipc{};

		/** Callback that is being called when a testcase starts. */
		void onTestStart(const std::string& testName);

		/** Callback that is being called when a testcase ends. */
		void onTestEnd(const std::string& result = "", const std::string& duration = "");

		/**
		 * Keeps track of called methods.
		 * We use the set to efficiently determine if we already noticed an called method.
		 */
		FunctionIdSet calledMethodIds;

		/**
		 * Keeps track of called methods.
		 * We use the vector to uniquely store the information about called methods.
		 */
		std::vector<FunctionInfo> calledMethods;

		/**
		* Returns the event mask which tells the CLR which callbacks the profiler wants to subscribe
		* to. We enable JIT compilation and assembly loads for coverage profiling. In
		* addition if light mode is disabled, EnterLeave hooks are enabled to force re-jitting of pre-jitted
		* code, in order to make coverage information independent of pre-jitted code.
		*/
		DWORD getEventMask();

		/**
		* Defines whether the given function should trigger a callback everytime it is executed.
		*
		* We do not register a single function callback in order to not affect
		* performance. In effect, we disable this feature here. It has been tested via
		* a performance benchmark, that this implementation does not impact call
		* performance.
		*
		* For coverage profiling, we do not need this callback. However, the event is
		* enabled in the event mask in order to force JIT-events for each first call to
		* a function, independent of whether a pre-jitted version exists.)
		*/
		static UINT_PTR _stdcall functionMapper(FunctionID functionId, BOOL* pbHookFunction) throw(...);
		void adjustEventMask();

		/** Dumps all environment variables to the log file. */
		void dumpEnvironment();

		void initializeConfig();

		/** Returns a proxy for the upload daemon process */
		static UploadDaemon createDaemon();

		/** Create method info object for a function id. */
		HRESULT getFunctionInfo(FunctionID functionID, FunctionInfo& info);

		/**  Store assembly counter for id. */
		int registerAssembly(AssemblyID assemblyId);

		/** Stores the assmebly name, path and metadata in the passed variables.*/
		void getAssemblyInfo(AssemblyID assemblyId, WCHAR* assemblyName, WCHAR* assemblyPath, ASSEMBLYMETADATA* moduleId);

		/** Triggers eagerly writing of function infos to log. */
		void recordFunctionInfo(std::vector<FunctionInfo>& list, FunctionID calleeId);

		/** Returns whether eager mode is enabled and amount of recorded method calls reached eagerness threshold. */
		bool shouldWriteEagerly();

		/** Write all information about the recorded functions to the log and clears the log. */
		void writeFunctionInfosToLog();

		/** Writes the fileVersionInfo into the provided buffer. */
		void writeFileVersionInfo(LPCWSTR assemblyPath, std::wostringstream&);

		HRESULT JITCompilationFinishedImplementation(FunctionID functionID);
		HRESULT AssemblyLoadFinishedImplementation(AssemblyID assemblyID);
		HRESULT JITInliningImplementation(FunctionID calleeID, BOOL* pfShouldInline);
		HRESULT InitializeImplementation(IUnknown* pICorProfilerInfoUnk);

		/** Logs a stack trace. May rethrow the caught exception. */
		void handleException(const std::string& context);
	};

}
