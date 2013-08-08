 /*
 * @ConQAT.Rating YELLOW Hash: 74CCDB9FF909088C601ABBC2FA63ED72
 */

#include "CProfilerCallbackBase.h"

/** Constructor */
CProfilerCallbackBase::CProfilerCallbackBase() : m_ref_cnt(0){
	// do nothing but inialization performed in m_ref_cnt
}

/** Destructor */
CProfilerCallbackBase::~CProfilerCallbackBase() {
	// do nothing 
}

//====================================================
// IUnknown implementation
//====================================================
ULONG CProfilerCallbackBase::AddRef() {
	return InterlockedIncrement(&m_ref_cnt);
}

ULONG CProfilerCallbackBase::Release() {
	return InterlockedDecrement(&m_ref_cnt);
}

HRESULT CProfilerCallbackBase::QueryInterface( REFIID riid, void **ppInterface ) {
	if(riid == IID_IUnknown) {
		*ppInterface = static_cast<ICorProfilerCallback*>(this);
		return S_OK;
	}
	else if(riid == IID_ICorProfilerCallback) {
		*ppInterface = static_cast<ICorProfilerCallback*>(this);
		return S_OK;
	}
	else if(riid == IID_ICorProfilerCallback2) {
		*ppInterface = static_cast<ICorProfilerCallback2*>(this);
		return S_OK;
	}
    else if(riid == IID_ICorProfilerCallback3) {
		*ppInterface = static_cast<ICorProfilerCallback3*>(this);
		return S_OK;
	}
    return E_NOTIMPL;
}
//====================================================
// End IUnknown implementation
//====================================================



//====================================================
// Profiling interface implementation
// 
// Provides default implementations for all methods. 
// Implementations are contained in the subclass.
// This allows for clean separation of default implementations 
// of callbacks that are not required for coverage profiling, 
// and code specific for the coverage profiler.
//
//====================================================
STDMETHODIMP CProfilerCallbackBase::Initialize(IUnknown *pICorProfilerInfoUnk) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::Shutdown() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::AppDomainCreationStarted(AppDomainID appDomainID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::AppDomainCreationFinished(AppDomainID appDomainID, HRESULT hrStatus) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::AppDomainShutdownStarted(AppDomainID appDomainID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::AppDomainShutdownFinished(AppDomainID appDomainID, HRESULT hrStatus) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::AssemblyLoadStarted(AssemblyID assemblyID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::AssemblyLoadFinished(AssemblyID assemblyID, HRESULT hrStatus) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::AssemblyUnloadStarted(AssemblyID assemblyID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::AssemblyUnloadFinished(AssemblyID assemblyID, HRESULT hrStatus) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ModuleLoadStarted(ModuleID moduleID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ModuleLoadFinished(ModuleID moduleID, HRESULT hrStatus) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ModuleUnloadStarted(ModuleID moduleID) {
    return S_OK;
}
	  
STDMETHODIMP CProfilerCallbackBase::ModuleUnloadFinished(ModuleID moduleID, HRESULT hrStatus) {
	return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ModuleAttachedToAssembly(ModuleID moduleID, AssemblyID assemblyID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ClassLoadStarted(ClassID classID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ClassLoadFinished(ClassID classID, HRESULT hrStatus) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ClassUnloadStarted(ClassID classID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ClassUnloadFinished(ClassID classID, HRESULT hrStatus) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::FunctionUnloadStarted(FunctionID functionID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::JITCompilationStarted(FunctionID functionID, BOOL fIsSafeToBlock) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::JITCompilationFinished(FunctionID functionID, HRESULT hrStatus, BOOL fIsSafeToBlock) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::JITCachedFunctionSearchStarted(FunctionID functionID, BOOL *pbUseCachedFunction) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::JITCachedFunctionSearchFinished(FunctionID functionID, COR_PRF_JIT_CACHE result) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::JITFunctionPitched(FunctionID functionID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::JITInlining(FunctionID callerID, FunctionID calleeID, BOOL *pfShouldInline) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::UnmanagedToManagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ManagedToUnmanagedTransition(FunctionID functionID, COR_PRF_TRANSITION_REASON reason) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ThreadCreated(ThreadID threadID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ThreadDestroyed(ThreadID threadID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ThreadAssignedToOSThread(ThreadID managedThreadID, DWORD osThreadID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RemotingClientInvocationStarted() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RemotingClientSendingMessage(GUID *pCookie, BOOL fIsAsync) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RemotingClientReceivingReply(GUID *pCookie, BOOL fIsAsync) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RemotingClientInvocationFinished() {
	return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RemotingServerReceivingMessage(GUID *pCookie, BOOL fIsAsync) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RemotingServerInvocationStarted() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RemotingServerInvocationReturned() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RemotingServerSendingReply(GUID *pCookie, BOOL fIsAsync) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RuntimeSuspendFinished() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RuntimeSuspendAborted() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RuntimeResumeStarted() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RuntimeResumeFinished() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RuntimeThreadSuspended(ThreadID threadID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RuntimeThreadResumed(ThreadID threadID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::MovedReferences(ULONG cmovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], ULONG cObjectIDRangeLength[]) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ObjectAllocated(ObjectID objectID, ClassID classID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ObjectsAllocatedByClass(ULONG classCount, ClassID classIDs[], ULONG objects[]) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ObjectReferences(ObjectID objectID, ClassID classID, ULONG objectRefs, ObjectID objectRefIDs[]) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RootReferences(ULONG rootRefs, ObjectID rootRefIDs[]) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionThrown(ObjectID thrownObjectID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionUnwindFunctionEnter(FunctionID functionID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionUnwindFunctionLeave() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionSearchFunctionEnter(FunctionID functionID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionSearchFunctionLeave() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionSearchFilterEnter(FunctionID functionID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionSearchFilterLeave() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionSearchCatcherFound(FunctionID functionID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionCLRCatcherFound() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionCLRCatcherExecute() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionOSHandlerEnter(FunctionID functionID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionOSHandlerLeave(FunctionID functionID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionUnwindFinallyEnter(FunctionID functionID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionUnwindFinallyLeave() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionCatcherEnter(FunctionID functionID, ObjectID objectID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ExceptionCatcherLeave() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::COMClassicVTableCreated(ClassID wrappedClassID, REFGUID implementedIID, void *pVTable, ULONG cSlots) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::COMClassicVTableDestroyed(ClassID wrappedClassID, REFGUID implementedIID, void *pVTable) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ThreadNameChanged(ThreadID threadID, ULONG cchName, WCHAR name[]) {
	return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason) {
	return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::SurvivingReferences(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], ULONG cObjectIDRangeLength[]) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::GarbageCollectionFinished() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::FinalizeableObjectQueued(DWORD finalizerFlags, ObjectID objectID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::RootReferences2(ULONG cRootRefs, ObjectID rootRefIDs[], COR_PRF_GC_ROOT_KIND rootKinds[], COR_PRF_GC_ROOT_FLAGS rootFlags[], UINT_PTR rootIDs[]) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::HandleCreated(GCHandleID handleID, ObjectID initialObjectID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::HandleDestroyed(GCHandleID handleID) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::InitializeForAttach(IUnknown * pCorProfilerInfoUnk,void * pvClientData, UINT cbClientData) {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ProfilerAttachComplete() {
    return S_OK;
}

STDMETHODIMP CProfilerCallbackBase::ProfilerDetachSucceeded() {
    return S_OK;
}

//====================================================
// End of Profiling interface implementation
//====================================================