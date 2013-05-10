#define WIN32_LEAN_AND_MEAN // Exclude rarely-used stuff from Windows headers
#include <windows.h>
#include <stdio.h>
#include "ProfilerCallback.h"


HRESULT RegisterClassBase( REFCLSID rclsid,
                            const char *szDesc,                 
                            const char *szProgID,               
                            const char *szIndepProgID,          
                            char *szOutCLSID,
							size_t nOutCLSIDLen );

HRESULT UnregisterClassBase( REFCLSID rclsid,
                            const char *szProgID,
                            const char *szIndepProgID,
                            char *szOutCLSID,
							size_t nOutCLSIDLen );

BOOL SetRegValue( char *szKeyName, char *szKeyword, char *szValue );

BOOL DeleteKey( const char *szKey, const char *szSubkey );

BOOL SetKeyAndValue(const char *szKey,
                    const char *szSubkey,
                    const char *szValue );

//=============================================================
#define MAX_LENGTH 256
#define PROFILER_GUID "{DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}"

extern const GUID CLSID_PROFILER = 
{ 0xDD0A1BB6, 0x11CE, 0x11DD, { 0x8E, 0xE8, 0x3F, 0x9E, 0x55, 0xD8, 0x95, 0x93 } };




static const char *g_szProgIDPrefix = "Profiler";

#define COM_METHOD( TYPE ) TYPE STDMETHODCALLTYPE

HINSTANCE g_hInst;        // instance handle to this piece of code

//==========================================================

BOOL WINAPI DllMain( HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved )
{    
    // save off the instance handle for later use
    switch ( dwReason )
    {
        case DLL_PROCESS_ATTACH:
            DisableThreadLibraryCalls( hInstance );
            g_hInst = hInstance;
            break;        
    } 
        
    return TRUE;
}

//================================================================

class CClassFactory : public IClassFactory
{
    public:
        CClassFactory( ){ m_refCount = 1; }
    
        COM_METHOD( ULONG ) AddRef()
                            {
                                return InterlockedIncrement( &m_refCount );
                            }
        COM_METHOD( ULONG ) Release()
                            {
                                return InterlockedDecrement( &m_refCount );
                            }

        COM_METHOD( HRESULT ) QueryInterface(REFIID riid, void **ppInterface );         
        
        // IClassFactory methods
        COM_METHOD( HRESULT ) LockServer( BOOL fLock ) { return S_OK; }
        COM_METHOD( HRESULT ) CreateInstance( IUnknown *pUnkOuter,
                                              REFIID riid,
                                              void **ppInterface );
        
    private:
    
        long m_refCount;                        
};

CClassFactory g_ProfilerClassFactory;

//================================================================

//#pragma comment(linker, "/EXPORT:DllUnregisterServer=_DllUnregisterServer@0,PRIVATE")
STDAPI DllUnregisterServer()
{    
    char szID[128];         // the class ID to unregister.
    char szCLSID[128];      // CLSID\\szID.
    OLECHAR szWID[128];     // helper for the class ID to unregister.
    char rcProgID[128];    // szProgIDPrefix.szClassProgID
    char rcIndProgID[128]; // rcProgID.iVersion


    // format the prog ID values.
    sprintf_s( rcProgID,128, "%s.%s", g_szProgIDPrefix, PROFILER_GUID );
    sprintf_s( rcIndProgID,128, "%s.%d", rcProgID, 1 );

	memset (szCLSID, 0, sizeof(szCLSID));
    UnregisterClassBase( CLSID_PROFILER, rcProgID, rcIndProgID, szCLSID, ARRAY_SIZE(szCLSID) );
    DeleteKey( szCLSID, "InprocServer32" );

    StringFromGUID2(CLSID_PROFILER, szWID, ARRAY_SIZE( szWID ) );
    WideCharToMultiByte(CP_ACP, 0, szWID, -1, szID, sizeof(szID), NULL,NULL);

    DeleteKey( "CLSID", szCLSID );

    return S_OK;   
}

//================================================================

//#pragma comment(linker, "/EXPORT:DllRegisterServer=_DllRegisterServer@0,PRIVATE")
STDAPI DllRegisterServer()
{    
    HRESULT hr = S_OK;
    char  szModule[_MAX_PATH];  

    DllUnregisterServer();
    GetModuleFileNameA( g_hInst, szModule, ARRAY_SIZE(szModule) );

    char rcCLSID[MAX_LENGTH];           // CLSID\\szID.
    char rcProgID[MAX_LENGTH];          // szProgIDPrefix.szClassProgID
    char rcIndProgID[MAX_LENGTH];       // rcProgID.iVersion
    char rcInproc[MAX_LENGTH + 2];      // CLSID\\InprocServer32


    // format the prog ID values.
    sprintf_s( rcIndProgID,MAX_LENGTH, "%s.%s", g_szProgIDPrefix, PROFILER_GUID ) ;
    sprintf_s( rcProgID,MAX_LENGTH, "%s.%d", rcIndProgID, 1 );

    // do the initial portion.
    hr =  RegisterClassBase( CLSID_PROFILER, 
                            "Profiler", rcProgID, rcIndProgID, rcCLSID,
							ARRAY_SIZE (rcCLSID));

    if ( SUCCEEDED( hr ) )
    {
        // set the server path.
        SetKeyAndValue( rcCLSID, "InprocServer32", szModule );

        // add the threading model information.
        sprintf_s( rcInproc,MAX_LENGTH+2, "%s\\%s", rcCLSID, "InprocServer32" );
        SetRegValue( rcInproc, "ThreadingModel", "Both" );
    }   
    else
        DllUnregisterServer();
    
    return hr;    
}

//================================================================

//#pragma comment(linker, "/EXPORT:DllGetClassObject=_DllGetClassObject@12,PRIVATE")
STDAPI DllGetClassObject( REFCLSID rclsid, REFIID riid, LPVOID FAR *ppv )                  
{    
    HRESULT hr = E_OUTOFMEMORY;

    if ( rclsid == CLSID_PROFILER )
        hr = g_ProfilerClassFactory.QueryInterface( riid, ppv );

    return hr;   
}

//===========================================================

HRESULT CClassFactory::QueryInterface( REFIID riid, void **ppInterface )
{    
    if ( riid == IID_IUnknown )
        *ppInterface = static_cast<IUnknown *>( this ); 
    else if ( riid == IID_IClassFactory )
        *ppInterface = static_cast<IClassFactory *>( this );
    else
    {
        *ppInterface = NULL;                                  
        return E_NOINTERFACE;
    }
    
    reinterpret_cast<IUnknown *>( *ppInterface )->AddRef();
    
    return S_OK;
}

// Used to determine whether the DLL can be unloaded by COM
//#pragma comment(linker, "/EXPORT:DllCanUnloadNow=_DllCanUnloadNow@0,PRIVATE")
STDAPI DllCanUnloadNow(void)
{
    return S_OK;
}

//===========================================================

HRESULT CClassFactory::CreateInstance( IUnknown *pUnkOuter, REFIID riid,
                                        void **ppInstance )
{       
    // aggregation is not supported by these objects
    if ( pUnkOuter != NULL )
        return CLASS_E_NOAGGREGATION;

    CProfilerCallback * pProfilerCallback = new CProfilerCallback();

    *ppInstance = (void *)pProfilerCallback;

    return S_OK;
}

//===========================================================

HRESULT RegisterClassBase( REFCLSID rclsid,
                            const char *szDesc,                 
                            const char *szProgID,               
                            const char *szIndepProgID,          
                            char *szOutCLSID,
							size_t nOutCLSIDLen)              
{
    char szID[64];     // the class ID to register.
    OLECHAR szWID[64]; // helper for the class ID to register.

    StringFromGUID2( rclsid, szWID, ARRAY_SIZE( szWID ) );
    WideCharToMultiByte(CP_ACP, 0, szWID, -1, szID, sizeof(szID), NULL, NULL);

    strcpy_s( szOutCLSID, nOutCLSIDLen, "CLSID\\" );
    strcat_s( szOutCLSID, nOutCLSIDLen, szID );

	BOOL success = TRUE;

    // create ProgID keys.
    success &=SetKeyAndValue( szProgID, NULL, szDesc );
    success &=SetKeyAndValue( szProgID, "CLSID", szID );

    // create VersionIndependentProgID keys.
    success &=SetKeyAndValue( szIndepProgID, NULL, szDesc );
    success &= SetKeyAndValue( szIndepProgID, "CurVer", szProgID );
    success &=SetKeyAndValue( szIndepProgID, "CLSID", szID );

    // create entries under CLSID.
    success &=SetKeyAndValue( szOutCLSID, NULL, szDesc );
    success &=SetKeyAndValue( szOutCLSID, "ProgID", szProgID );
    success &=SetKeyAndValue( szOutCLSID, "VersionIndependentProgID", szIndepProgID );
    success &=SetKeyAndValue( szOutCLSID, "NotInsertable", NULL );
    
    if(success) return S_OK;
	else return S_FALSE;
}

//===========================================================

HRESULT UnregisterClassBase( REFCLSID rclsid,
                            const char *szProgID,
                            const char *szIndepProgID,
                            char *szOutCLSID,
							size_t nOutCLSIDLen )
{
    char szID[64];     // the class ID to register.
    OLECHAR szWID[64]; // helper for the class ID to register.

    StringFromGUID2( rclsid, szWID, ARRAY_SIZE( szWID ) );
    WideCharToMultiByte(CP_ACP, 0, szWID, -1, szID, sizeof(szID), NULL, NULL);

    strcpy_s( szOutCLSID, nOutCLSIDLen, "CLSID\\" );
    strcat_s( szOutCLSID, nOutCLSIDLen, szID );

    // delete the version independant prog ID settings.
    DeleteKey( szIndepProgID, "CurVer" );
    DeleteKey( szIndepProgID, "CLSID" );
    RegDeleteKeyA( HKEY_CLASSES_ROOT, szIndepProgID );

    // delete the prog ID settings.
    DeleteKey( szProgID, "CLSID" );
    RegDeleteKeyA( HKEY_CLASSES_ROOT, szProgID );

    // delete the class ID settings.
    DeleteKey( szOutCLSID, "ProgID" );
    DeleteKey( szOutCLSID, "VersionIndependentProgID" );
    DeleteKey( szOutCLSID, "NotInsertable" );
    RegDeleteKeyA( HKEY_CLASSES_ROOT, szOutCLSID );
    
    return S_OK;
}

//===========================================================

BOOL DeleteKey( const char *szKey,
                 const char *szSubkey )
{
    char rcKey[MAX_LENGTH]; // buffer for the full key name.

	sprintf_s (rcKey, ARRAY_SIZE(rcKey), "%s\\%s", szKey, szSubkey);

	char buf[256];
	sprintf_s (buf, ARRAY_SIZE(buf), "Length=%d", strlen (rcKey));

    // delete the registration key.
    RegDeleteKeyA( HKEY_CLASSES_ROOT, rcKey );
    
    return TRUE;
}

//===========================================================

BOOL SetKeyAndValue( const char *szKey,
                              const char *szSubkey,
                              const char *szValue )
{
    HKEY hKey;              // handle to the new reg key.
    char rcKey[MAX_LENGTH]; // buffer for the full key name.

    // init the key with the base key name.
    strcpy_s( rcKey,MAX_LENGTH, szKey );

    // append the subkey name (if there is one).
    if ( szSubkey != NULL )
    {
        strcat_s( rcKey,MAX_LENGTH, "\\" );
        strcat_s( rcKey,MAX_LENGTH, szSubkey );
    }

    // create the registration key.
	long ec = RegCreateKeyExA( HKEY_CLASSES_ROOT, 
                          rcKey, 
                          0, 
                          NULL,
                          REG_OPTION_NON_VOLATILE, 
                          KEY_ALL_ACCESS, 
                          NULL,
                          &hKey, 
                          NULL );
    if (ec  == ERROR_SUCCESS )
    {
        // set the value (if there is one).
        if ( szValue != NULL )
        {
            RegSetValueExA( hKey, NULL, 0, REG_SZ, (BYTE *)szValue,
                            (DWORD)(((strlen(szValue) + 1) * sizeof (char))));
        }
        
        RegCloseKey( hKey );

        return TRUE;
    }

    return FALSE;   
}

BOOL SetRegValue(char *szKeyName, char *szKeyword, char *szValue)
{
    HKEY hKey; // handle to the new reg key.

    // create the registration key.
    if ( RegCreateKeyExA( HKEY_CLASSES_ROOT, 
                          szKeyName, 0,  NULL,
                          REG_OPTION_NON_VOLATILE, 
                          KEY_ALL_ACCESS, 
                          NULL, &hKey, 
                          NULL) == ERROR_SUCCESS )
    {
        // set the value (if there is one).
        if ( szValue != NULL )
        {
            RegSetValueExA( hKey, szKeyword, 0, REG_SZ, 
                            (BYTE *)szValue, 
                            (DWORD)((strlen(szValue) + 1) * sizeof ( char )));
        }

        RegCloseKey( hKey );
        
        return TRUE;
    }
    
    return FALSE;
}
