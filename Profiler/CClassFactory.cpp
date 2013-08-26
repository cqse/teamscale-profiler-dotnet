/*
 * @ConQAT.Rating RED Hash: B1C383095EDD96175583C6F92271C8B7
 */

#define WIN32_LEAN_AND_MEAN // Exclude rarely-used stuff from Windows headers
#include <windows.h>
#include <stdio.h>
#include "CProfilerCallback.h"

// TODO [NG]: Why are the following declarations here and not in a header file?
// TODO [NG]: The methods lack comments.
HRESULT RegisterClassBase(REFCLSID rclsid, const char *szDesc,
		const char *szProgID, const char *szIndepProgID, char *szOutCLSID,
		size_t nOutCLSIDLen);

HRESULT UnregisterClassBase(REFCLSID rclsid, const char *szProgID,
		const char *szIndepProgID, char *szOutCLSID, size_t nOutCLSIDLen);

BOOL SetRegValue(char *szKeyName, char *szKeyword, char *szValue);

BOOL DeleteKey(const char *szKey, const char *szSubkey);

BOOL SetKeyAndValue(const char *szKey, const char *szSubkey,
		const char *szValue);

// TODO [NG]: I think we should avoid macros wherever possible and use 'normal'
//            constants.
#define MAX_LENGTH 256
#define PROFILER_GUID "{DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}"

extern const GUID CLSID_PROFILER = { 0xDD0A1BB6, 0x11CE, 0x11DD, { 0x8E, 0xE8,
		0x3F, 0x9E, 0x55, 0xD8, 0x95, 0x93 } };

static const char *g_szProgIDPrefix = "Profiler";

// TODO [NG]: I think this macro adds only noise and should be removed/inlined.
// I am unsure if it is used to mark the methods as COM interfaces.
#define COM_METHOD(TYPE) TYPE STDMETHODCALLTYPE

HINSTANCE g_hInst; // Instance handle to this piece of code.

BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved) {
	// Save off the instance handle for later use.
	if (dwReason == DLL_PROCESS_ATTACH) {
		DisableThreadLibraryCalls(hInstance);
		g_hInst = hInstance;
	}

	return TRUE;
}

// TODO [NG]: The class and its members need comments.
class CClassFactory: public IClassFactory {
public:
	CClassFactory() {
		m_refCount = 1;
	}

	virtual ~CClassFactory() {
		// Nothing to do.
	}

	COM_METHOD( ULONG ) AddRef() {
		return InterlockedIncrement(&m_refCount);
	}

	COM_METHOD( ULONG ) Release() {
		return InterlockedDecrement(&m_refCount);
	}

	COM_METHOD( HRESULT ) QueryInterface(REFIID riid, void **ppInterface);

	// IClassFactory methods
	COM_METHOD( HRESULT ) LockServer(BOOL fLock) {
		return S_OK;
	}
	COM_METHOD( HRESULT ) CreateInstance(IUnknown *pUnkOuter, REFIID riid,
			void **ppInterface);

private:

	long m_refCount;
};

CClassFactory g_ProfilerClassFactory;

STDAPI DllUnregisterServer() {
	char szID[128];        // The class ID to unregister.
	char szCLSID[128];     // CLSID\\szID.
	OLECHAR szWID[128];    // Helper for the class ID to unregister.
	char rcProgID[128];    // szProgIDPrefix.szClassProgID
	char rcIndProgID[128]; // rcProgID.iVersion

	// Format the prog ID values.
	sprintf_s(rcProgID, 128, "%s.%s", g_szProgIDPrefix, PROFILER_GUID);
	sprintf_s(rcIndProgID, 128, "%s.%d", rcProgID, 1);

	memset(szCLSID, 0, sizeof(szCLSID));
	UnregisterClassBase(CLSID_PROFILER, rcProgID, rcIndProgID, szCLSID,
	ARRAY_SIZE(szCLSID));
	// TODO [NG]: 'InprocServer32' should be a constant (redundant literal).
	DeleteKey(szCLSID, "InprocServer32");

	StringFromGUID2(CLSID_PROFILER, szWID, ARRAY_SIZE( szWID ));
	WideCharToMultiByte(CP_ACP, 0, szWID, -1, szID, sizeof(szID), NULL, NULL);

	DeleteKey("CLSID", szCLSID);

	return S_OK;
}

STDAPI DllRegisterServer() {
	HRESULT hr = S_OK;
	char szModule[_MAX_PATH];

	DllUnregisterServer();
	GetModuleFileNameA(g_hInst, szModule, ARRAY_SIZE(szModule));

	char rcCLSID[MAX_LENGTH];      // CLSID\\szID.
	char rcProgID[MAX_LENGTH];     // szProgIDPrefix.szClassProgID
	char rcIndProgID[MAX_LENGTH];  // rcProgID.iVersion
	char rcInproc[MAX_LENGTH + 2]; // CLSID\\InprocServer32

	// Format the prog ID values.
	sprintf_s(rcIndProgID, MAX_LENGTH, "%s.%s", g_szProgIDPrefix,
	PROFILER_GUID);
	sprintf_s(rcProgID, MAX_LENGTH, "%s.%d", rcIndProgID, 1);

	// Do the initial portion.
	hr = RegisterClassBase(CLSID_PROFILER, "Profiler", rcProgID, rcIndProgID,
			rcCLSID, ARRAY_SIZE (rcCLSID));

	if (SUCCEEDED(hr)) {
		// Set the server path.
		SetKeyAndValue(rcCLSID, "InprocServer32", szModule);

		// Add the threading model information.
		sprintf_s(rcInproc, MAX_LENGTH + 2, "%s\\%s", rcCLSID,
				"InprocServer32");
		SetRegValue(rcInproc, "ThreadingModel", "Both");
	} else {
		DllUnregisterServer();
	}
	return hr;
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID FAR *ppv) {
	HRESULT hr = CLASS_E_CLASSNOTAVAILABLE;

	if (rclsid == CLSID_PROFILER) {
		hr = g_ProfilerClassFactory.QueryInterface(riid, ppv);
	}
	return hr;
}

HRESULT CClassFactory::QueryInterface(REFIID riid, void **ppInterface) {
	if (riid == IID_IUnknown) {
		// TODO [NG]: Do we need the explicit cast here?
		*ppInterface = static_cast<IUnknown *>(this);
	} else if (riid == IID_IClassFactory) {
		// TODO [NG]: Do we need the explicit cast here?
		*ppInterface = static_cast<IClassFactory *>(this);
	} else {
		*ppInterface = NULL;
		return E_NOINTERFACE;
	}

	// TODO [NG]: Why do we need the potentially dangerous cast here? Can't we
	//            just call AddRef as ppInterface points to 'this'?
	reinterpret_cast<IUnknown *>(*ppInterface)->AddRef();
	return S_OK;
}

/** Used to determine whether the DLL can be unloaded by COM. */
STDAPI DllCanUnloadNow(void) {
	return S_OK;
}

HRESULT CClassFactory::CreateInstance(IUnknown *pUnkOuter, REFIID riid,
		void **ppInstance) {
	// Aggregation is not supported by these objects.
	if (pUnkOuter != NULL) {
		return CLASS_E_NOAGGREGATION;
	}

	CProfilerCallback *pProfilerCallback = new CProfilerCallback();
	*ppInstance = (void *) pProfilerCallback;

	return S_OK;
}

HRESULT RegisterClassBase(REFCLSID rclsid, const char *szDesc,
		const char *szProgID, const char *szIndepProgID, char *szOutCLSID,
		size_t nOutCLSIDLen) {
	// TODO [NG]: The following six statements are identical with the first six
	//            statements of the following method. It would be nice if the
	//            redundancy was removed.
	char szID[64];     // The class ID to register.
	OLECHAR szWID[64]; // Helper for the class ID to register.

	StringFromGUID2(rclsid, szWID, ARRAY_SIZE( szWID ));
	WideCharToMultiByte(CP_ACP, 0, szWID, -1, szID, sizeof(szID), NULL, NULL);

	strcpy_s(szOutCLSID, nOutCLSIDLen, "CLSID\\");
	strcat_s(szOutCLSID, nOutCLSIDLen, szID);

	BOOL success = TRUE;

	// Create ProgID keys.
	success &= SetKeyAndValue(szProgID, NULL, szDesc);
	success &= SetKeyAndValue(szProgID, "CLSID", szID);

	// Create VersionIndependentProgID keys.
	success &= SetKeyAndValue(szIndepProgID, NULL, szDesc);
	success &= SetKeyAndValue(szIndepProgID, "CurVer", szProgID);
	success &= SetKeyAndValue(szIndepProgID, "CLSID", szID);

	// Create entries under CLSID.
	success &= SetKeyAndValue(szOutCLSID, NULL, szDesc);
	success &= SetKeyAndValue(szOutCLSID, "ProgID", szProgID);
	success &= SetKeyAndValue(szOutCLSID, "VersionIndependentProgID",
			szIndepProgID);
	success &= SetKeyAndValue(szOutCLSID, "NotInsertable", NULL);

	if (success) {
		return S_OK;
	} else {
		return S_FALSE;
	}
}

HRESULT UnregisterClassBase(REFCLSID rclsid, const char *szProgID,
		const char *szIndepProgID, char *szOutCLSID, size_t nOutCLSIDLen) {
	// TODO [NG]: The following six statements are identical with the first six
	//            statements of the previous method. It would be nice if the
	//            redundancy was removed.
	char szID[64];     // The class ID to register.
	OLECHAR szWID[64]; // Helper for the class ID to register.

	StringFromGUID2(rclsid, szWID, ARRAY_SIZE( szWID ));
	WideCharToMultiByte(CP_ACP, 0, szWID, -1, szID, sizeof(szID), NULL, NULL);

	strcpy_s(szOutCLSID, nOutCLSIDLen, "CLSID\\");
	strcat_s(szOutCLSID, nOutCLSIDLen, szID);

	// TODO [NG]: Why are the return values of 'DeleteKey' and 'RegDeleteKeyA'
	//            ignored? In the previous method, the return value of
	//            'SetKeyAndValue' was not ignored. I would expect similar
	//            behavior here.
	// Delete the version independent prog ID settings.
	DeleteKey(szIndepProgID, "CurVer");
	DeleteKey(szIndepProgID, "CLSID");
	RegDeleteKeyA(HKEY_CLASSES_ROOT, szIndepProgID);

	// Delete the prog ID settings.
	DeleteKey(szProgID, "CLSID");
	RegDeleteKeyA(HKEY_CLASSES_ROOT, szProgID);

	// Delete the class ID settings.
	DeleteKey(szOutCLSID, "ProgID");
	DeleteKey(szOutCLSID, "VersionIndependentProgID");
	DeleteKey(szOutCLSID, "NotInsertable");
	RegDeleteKeyA(HKEY_CLASSES_ROOT, szOutCLSID);

	return S_OK;
}

BOOL DeleteKey(const char *szKey, const char *szSubkey) {
	char rcKey[MAX_LENGTH]; // buffer for the full key name.
	sprintf_s(rcKey, ARRAY_SIZE(rcKey), "%s\\%s", szKey, szSubkey);

	char buf[256];
	sprintf_s(buf, ARRAY_SIZE(buf), "Length=%d", strlen(rcKey));

	// Delete the registration key.
	RegDeleteKeyA(HKEY_CLASSES_ROOT, rcKey);

	return TRUE;
}

// TODO [NG]: This method contains the complete following method. The redundancy
//            should be removed.
BOOL SetKeyAndValue(const char *szKey, const char *szSubkey,
		const char *szValue) {
	HKEY hKey;              // Handle to the new registry key.
	char rcKey[MAX_LENGTH]; // Buffer for the full key name.

	// Initialize the key with the base key name.
	strcpy_s(rcKey, MAX_LENGTH, szKey);

	// Append the subkey name (if there is one).
	if (szSubkey != NULL) {
		strcat_s(rcKey, MAX_LENGTH, "\\");
		strcat_s(rcKey, MAX_LENGTH, szSubkey);
	}

	// Create the registry key.
	long ec = RegCreateKeyExA(HKEY_CLASSES_ROOT, rcKey, 0, NULL,
			REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hKey, NULL);
	if (ec == ERROR_SUCCESS) {
		// Set the value (if there is one).
		if (szValue != NULL) {
			RegSetValueExA(hKey, NULL, 0, REG_SZ, (BYTE *) szValue,
					(DWORD)(((strlen(szValue) + 1) * sizeof(char))));
		}

		RegCloseKey(hKey);
		return TRUE;
	}
	return FALSE;
}

// TODO [NG]: This method is completely contained in the previous method. The
//            redundancy should be removed.
BOOL SetRegValue(char *szKeyName, char *szKeyword, char *szValue) {
	HKEY hKey; // Handle to the new registry key.

	// create the registration key.
	if (RegCreateKeyExA(HKEY_CLASSES_ROOT, szKeyName, 0, NULL,
			REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hKey, NULL)
			== ERROR_SUCCESS) {
		// set the value (if there is one).
		if (szValue != NULL) {
			RegSetValueExA(hKey, szKeyword, 0, REG_SZ, (BYTE *) szValue,
					(DWORD)((strlen(szValue) + 1) * sizeof(char)));
		}

		RegCloseKey(hKey);
		return TRUE;
	}

	return FALSE;
}
