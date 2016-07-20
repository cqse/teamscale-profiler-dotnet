/*
 * @ConQAT.Rating GREEN Hash: D8578F734D13784915D81A018AE3E758
 */

#define WIN32_LEAN_AND_MEAN // Exclude rarely-used stuff from Windows headers
#include <windows.h>
#include <stdio.h>
#include "CProfilerCallback.h"
#include "CClassFactory.h"

#define ARRAY_LENGTH(s) (sizeof(s) / sizeof(s[0]))

BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved) {
	// Save off the instance handle for later use.
	if (dwReason == DLL_PROCESS_ATTACH) {
		DisableThreadLibraryCalls(hInstance);
		profilerInstance = hInstance;
	}

	return TRUE;
}

/** Unregisters the profiler. */
STDAPI DllUnregisterServer() {
	char szID[128];        // The class ID to unregister.
	char szCLSID[128];     // CLSID\\szID.
	OLECHAR szWID[128];    // Helper for the class ID to unregister.
	char rcProgID[128];    // szProgIDPrefix.szClassProgID
	char rcIndProgID[128]; // rcProgID.iVersion

	// Format the prog ID values.
	sprintf_s(rcProgID, 128, "%s.%s", szProgIDPrefix, profilerGuid);
	sprintf_s(rcIndProgID, 128, "%s.%d", rcProgID, 1);

	memset(szCLSID, 0, sizeof(szCLSID));
	UnregisterClassBase(CLSID_PROFILER, rcProgID, rcIndProgID, szCLSID,
	ARRAY_LENGTH(szCLSID));

	DeleteKey(szCLSID, inProcessServerDeclaration);

	StringFromGUID2(CLSID_PROFILER, szWID, ARRAY_LENGTH( szWID ));
	WideCharToMultiByte(CP_ACP, 0, szWID, -1, szID, sizeof(szID), NULL, NULL);

	DeleteKey("CLSID", szCLSID);

	return S_OK;
}

/** Registers the Profiler. */
STDAPI DllRegisterServer() {
	HRESULT hr = S_OK;
	char szModule[_MAX_PATH];

	DllUnregisterServer();
	GetModuleFileNameA(profilerInstance, szModule, ARRAY_LENGTH(szModule));

	char rcCLSID[maxLength];      // CLSID\\szID.
	char rcProgID[maxLength];     // szProgIDPrefix.szClassProgID
	char rcIndProgID[maxLength];  // rcProgID.iVersion
	char rcInproc[maxLength + 2]; // CLSID\\InprocServer32

	// Format the prog ID values.
	sprintf_s(rcIndProgID, maxLength, "%s.%s", szProgIDPrefix, profilerGuid);
	sprintf_s(rcProgID, maxLength, "%s.%d", rcIndProgID, 1);

	// Do the initial portion.
	hr = RegisterClassBase(CLSID_PROFILER, "Profiler", rcProgID, rcIndProgID,
			rcCLSID, ARRAY_LENGTH (rcCLSID));

	if (SUCCEEDED(hr)) {
		// Set the server path.
		CreateFullKeyAndRegister(rcCLSID, inProcessServerDeclaration, szModule);

		// Add the threading model information.
		sprintf_s(rcInproc, maxLength + 2, "%s\\%s", rcCLSID,
				inProcessServerDeclaration);
		CreateRegistryKeyAndSetValue(rcInproc, "ThreadingModel", "Both");
	} else {
		DllUnregisterServer();
	}
	return hr;
}

/** Sets the class object for the profiler. */
STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID FAR *ppv) {
	HRESULT hr = CLASS_E_CLASSNOTAVAILABLE;

	if (rclsid == CLSID_PROFILER) {
		hr = ProfilerClassFactory.QueryInterface(riid, ppv);
	}
	return hr;
}

/** Implementation of the COM QueryInterface. */
HRESULT CClassFactory::QueryInterface(REFIID riid, void **ppInterface) {
	if (!ppInterface){
		return E_INVALIDARG;
	}

	if (riid == IID_IUnknown) {
		*ppInterface = this;
	} else if (riid == IID_IClassFactory) {
		*ppInterface = this;
	} else {
		*ppInterface = NULL;
		return E_NOINTERFACE;
	}

	AddRef();
	return S_OK;
}

/** Used to determine whether the DLL can be unloaded by COM. */
STDAPI DllCanUnloadNow(void) {
	return S_OK;
}

/** Instantiates the Profiler (CProfilerCallback) */
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
	char szID[64];     // The class ID to register.

	CreateIDs(rclsid, szOutCLSID, nOutCLSIDLen, szID);

	BOOL success = TRUE;

	// Create ProgID keys.
	success &= CreateFullKeyAndRegister(szProgID, NULL, szDesc);
	success &= CreateFullKeyAndRegister(szProgID, "CLSID", szID);

	// Create VersionIndependentProgID keys.
	success &= CreateFullKeyAndRegister(szIndepProgID, NULL, szDesc);
	success &= CreateFullKeyAndRegister(szIndepProgID, "CurVer", szProgID);
	success &= CreateFullKeyAndRegister(szIndepProgID, "CLSID", szID);

	// Create entries under CLSID.
	success &= CreateFullKeyAndRegister(szOutCLSID, NULL, szDesc);
	success &= CreateFullKeyAndRegister(szOutCLSID, "ProgID", szProgID);
	success &= CreateFullKeyAndRegister(szOutCLSID, "VersionIndependentProgID",
			szIndepProgID);
	success &= CreateFullKeyAndRegister(szOutCLSID, "NotInsertable", NULL);

	if (success) {
		return S_OK;
	} else {
		return S_FALSE;
	}
}

HRESULT UnregisterClassBase(REFCLSID rclsid, const char *szProgID,
		const char *szIndepProgID, char *szOutCLSID, size_t nOutCLSIDLen) {
	char szID[64];     // The class ID to register.
	CreateIDs(rclsid, szOutCLSID, nOutCLSIDLen, szID);

	BOOL success = TRUE;

	// Delete the version independent prog ID settings.
	success &= DeleteKey(szIndepProgID, "CurVer");
	success &= DeleteKey(szIndepProgID, "CLSID");
	success &= RegDeleteKeyA(HKEY_CLASSES_ROOT, szIndepProgID);

	// Delete the prog ID settings.
	success &= DeleteKey(szProgID, "CLSID");
	success &= RegDeleteKeyA(HKEY_CLASSES_ROOT, szProgID);

	// Delete the class ID settings.
	success &= DeleteKey(szOutCLSID, "ProgID");
	success &= DeleteKey(szOutCLSID, "VersionIndependentProgID");
	success &= DeleteKey(szOutCLSID, "NotInsertable");
	success &= RegDeleteKeyA(HKEY_CLASSES_ROOT, szOutCLSID);

	if (success) {
		return S_OK;
	} else {
		return S_FALSE;
	}
}

void CreateIDs(REFCLSID rclsid, char *szOutCLSID, size_t nOutCLSIDLen, char* szID){
	OLECHAR szWID[64]; // Helper for the class ID to register.

	StringFromGUID2(rclsid, szWID, ARRAY_LENGTH( szWID ));
	WideCharToMultiByte(CP_ACP, 0, szWID, -1, szID, sizeof(szID), NULL, NULL);

	strcpy_s(szOutCLSID, nOutCLSIDLen, "CLSID\\");
	strcat_s(szOutCLSID, nOutCLSIDLen, szID);
}

BOOL DeleteKey(const char *szKey, const char *szSubkey) {
	char rcKey[maxLength]; // buffer for the full key name.
	sprintf_s(rcKey, ARRAY_LENGTH(rcKey), "%s\\%s", szKey, szSubkey);

	char buf[256];
	sprintf_s(buf, ARRAY_LENGTH(buf), "Length=%zu", strlen(rcKey));

	// Delete the registration key.
	RegDeleteKeyA(HKEY_CLASSES_ROOT, rcKey);

	return TRUE;
}

BOOL CreateFullKeyAndRegister(const char *szKey, const char *szSubkey,
		const char *szValue) {
	char rcKey[maxLength]; // Buffer for the full key name.

	// Initialize the key with the base key name.
	strcpy_s(rcKey, maxLength, szKey);

	// Append the subkey name (if there is one).
	if (szSubkey != NULL) {
		strcat_s(rcKey, maxLength, "\\");
		strcat_s(rcKey, maxLength, szSubkey);
	}

	return CreateRegistryKeyAndSetValue(rcKey, NULL, szValue);
}

BOOL CreateRegistryKeyAndSetValue(const char *szKeyName, const char *szKeyword, const char *szValue) {
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
