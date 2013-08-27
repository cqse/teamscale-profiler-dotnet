 /*
 * @ConQAT.Rating YELLOW Hash: 65079FDB2DAFB2A2145E9C6E948D7DB0
 */

#ifndef _CClassFactory_H_
#define _CClassFactory_H_

// TODO [NG]: Why are the following declarations here and not in a header file?
// TODO [NG]: The methods lack comments.
HRESULT RegisterClassBase(REFCLSID rclsid, const char *szDesc,
		const char *szProgID, const char *szIndepProgID, char *szOutCLSID,
		size_t nOutCLSIDLen);

HRESULT UnregisterClassBase(REFCLSID rclsid, const char *szProgID,
		const char *szIndepProgID, char *szOutCLSID, size_t nOutCLSIDLen);

void CreateIDs(REFCLSID rclsid, char *szOutCLSID, size_t nOutCLSIDLen, char* szID);

BOOL CreateRegistryKeyAndSetValue(const char *szKeyName, const char *szKeyword, const char *szValue);

BOOL DeleteKey(const char *szKey, const char *szSubkey);

BOOL CreateFullKeyAndRegister(const char *szKey, const char *szSubkey,
		const char *szValue);

static const int g_maxLength = 256;
static const char *g_profilerGuid = "{DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}";

extern const GUID CLSID_PROFILER = { 0xDD0A1BB6, 0x11CE, 0x11DD, { 0x8E, 0xE8,
		0x3F, 0x9E, 0x55, 0xD8, 0x95, 0x93 } };

static const char *g_szProgIDPrefix = "Profiler";
static const char *g_inProcessServerDeclaration = "InprocServer32";

// TODO [NG]: I think this macro adds only noise and should be removed/inlined.
// -> I think it is used to mark the methods as COM interfaces.
#define COM_METHOD(TYPE) TYPE STDMETHODCALLTYPE

HINSTANCE g_hInst; // Instance handle to this piece of code.

#endif