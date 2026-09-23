#pragma once
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <unknwn.h>

class CClassFactory : public IClassFactory
{
public:
    CClassFactory();

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppv) override;
    ULONG   STDMETHODCALLTYPE AddRef() override;
    ULONG   STDMETHODCALLTYPE Release() override;

    HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown *pUnkOuter, REFIID riid, void **ppv) override;
    HRESULT STDMETHODCALLTYPE LockServer(BOOL fLock) override;

private:
    virtual ~CClassFactory();
    LONG m_refCount;
};
