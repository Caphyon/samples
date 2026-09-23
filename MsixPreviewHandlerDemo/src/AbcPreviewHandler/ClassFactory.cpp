#include "ClassFactory.h"
#include "PreviewHandlerImpl.h"
#include "Globals.h"

CClassFactory::CClassFactory() : m_refCount(1)
{
    InterlockedIncrement(&g_cDllRef);
}

CClassFactory::~CClassFactory()
{
    InterlockedDecrement(&g_cDllRef);
}

HRESULT STDMETHODCALLTYPE CClassFactory::QueryInterface(REFIID riid, void **ppv)
{
    if (!ppv) return E_POINTER;
    *ppv = nullptr;

    if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_IClassFactory))
    {

        *ppv = static_cast<IClassFactory*>(this);
        AddRef();
        return S_OK;

    }
    return E_NOINTERFACE;
}

ULONG STDMETHODCALLTYPE CClassFactory::AddRef()
{
    return InterlockedIncrement(&m_refCount);

}

ULONG STDMETHODCALLTYPE CClassFactory::Release()
{
    LONG cRef = InterlockedDecrement(&m_refCount);
    if (cRef == 0)
        delete this;
    return static_cast<ULONG>(cRef);

}

HRESULT STDMETHODCALLTYPE CClassFactory::CreateInstance(IUnknown *pUnkOuter, REFIID riid, void **ppv)
{
    if (!ppv) return E_POINTER;
    *ppv = nullptr;

    if (pUnkOuter)
        return CLASS_E_NOAGGREGATION;


    return CAbcPreviewHandler_CreateInstance(riid, ppv);

}

HRESULT STDMETHODCALLTYPE CClassFactory::LockServer(BOOL fLock)
{
    if (fLock)
        InterlockedIncrement(&g_cDllRef);
    else
        InterlockedDecrement(&g_cDllRef);
    return S_OK;
}
