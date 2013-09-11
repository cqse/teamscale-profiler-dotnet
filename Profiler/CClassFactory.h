/*
 * @ConQAT.Rating YELLOW Hash: 7F1F1DC8B5EFD933B973A4291FF698C9
 */

#ifndef _CClassFactory_H_
#define _CClassFactory_H_

/** Registers the class of the profiler by setting the registry entries. */
HRESULT RegisterClassBase(REFCLSID rclsid, const char *szDesc,
		const char *szProgID, const char *szIndepProgID, char *szOutCLSID,
		size_t nOutCLSIDLen);

/** Unregisters the profiler class by deleting the registry keys. */
HRESULT UnregisterClassBase(REFCLSID rclsid, const char *szProgID,
		const char *szIndepProgID, char *szOutCLSID, size_t nOutCLSIDLen);

/** Helper function to create the class IDs needed for registration.*/
void CreateIDs(REFCLSID rclsid, char *szOutCLSID, size_t nOutCLSIDLen,
		char* szID);

/** Deletes the key from the registry. */
BOOL DeleteKey(const char *szKey, const char *szSubkey);

/** Creates a full registry key and creates it in the registry. */
BOOL CreateFullKeyAndRegister(const char *szKey, const char *szSubkey,
		const char *szValue);

/**
 * Creates a registry key and sets its value. Returns true if the operations
 * have been executed successfully.
 */
BOOL CreateRegistryKeyAndSetValue(const char *szKeyName, const char *szKeyword,
		const char *szValue);

// Maximum length used for strings.
static const int maxLength = 256;

// String representation of the GUID used to register the profiler.
static const char *profilerGuid = "{DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}";

// The GUID of the profiler.
extern const GUID CLSID_PROFILER = { 0xDD0A1BB6, 0x11CE, 0x11DD, { 0x8E, 0xE8,
		0x3F, 0x9E, 0x55, 0xD8, 0x95, 0x93 } };

// Prefix of the program ID used to register the profiler.
static const char *szProgIDPrefix = "Profiler";

// Declaration of the threading model used for the profiler.
static const char *inProcessServerDeclaration = "InprocServer32";

// Mark methods as part of the COM interface.
#define COM_METHOD(TYPE) TYPE STDMETHODCALLTYPE

// COM instance handle to the profiler object.
HINSTANCE profilerInstance;

/** Class handling the registration and unregistration of the profiler. */
class CClassFactory: public IClassFactory {
public:
	/** Constructor. */
	CClassFactory() {
		referenceCount = 1;
	}

	/** Destructor. */
	virtual ~CClassFactory() {
		// Nothing to do.
	}

	/** COM method to add new references to the COM interface. */
	COM_METHOD( ULONG ) AddRef() {
		return InterlockedIncrement(&referenceCount);
	}

	/** COM method to release references to the COM interface. */
	COM_METHOD( ULONG ) Release() {
		return InterlockedDecrement(&referenceCount);
	}

	/** Implementation of the COM query interface. */
	COM_METHOD( HRESULT ) QueryInterface(REFIID riid, void **ppInterface);

	/** Overriding IClassFactory.LockServer method. */
	COM_METHOD( HRESULT ) LockServer(BOOL fLock) {
		return S_OK;
	}

	/** Overriding IClassFactory.CreateInstance method. */
	COM_METHOD( HRESULT ) CreateInstance(IUnknown *pUnkOuter, REFIID riid,
			void **ppInterface);

private:
	/** Counts the references to the COM interface of the ClassFactory. */
	long referenceCount;
};

/** The CClassFactory instance. */
CClassFactory ProfilerClassFactory;

#endif
