 /*
 * @ConQAT.Rating YELLOW Hash: 63EF04EE3BF50DFCA85C227FB39835DA
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
void CreateIDs(REFCLSID rclsid, char *szOutCLSID, size_t nOutCLSIDLen, char* szID);


/** Deletes the key from the registry. */
BOOL DeleteKey(const char *szKey, const char *szSubkey);

/** Creates a full registry key and creates it in the registry. */
BOOL CreateFullKeyAndRegister(const char *szKey, const char *szSubkey,
		const char *szValue);

/** Creates a registry key and sets its value. Returns true if the operations habe been executed successfully. */
BOOL CreateRegistryKeyAndSetValue(const char *szKeyName, const char *szKeyword, const char *szValue);

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

#endif