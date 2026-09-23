#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <shobjidl.h>
#include <propsys.h>
#include <ocidl.h>

class CAbcPreviewHandler :
    public IPreviewHandler,
    public IPreviewHandlerVisuals,
    public IObjectWithSite,
    public IInitializeWithStream
{
public:
    CAbcPreviewHandler();

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppv) override;
    ULONG   STDMETHODCALLTYPE AddRef() override;
    ULONG   STDMETHODCALLTYPE Release() override;

    HRESULT STDMETHODCALLTYPE Initialize(IStream *pStream, DWORD grfMode) override;

    HRESULT STDMETHODCALLTYPE SetWindow(HWND hwnd, const RECT *prc) override;
    HRESULT STDMETHODCALLTYPE SetRect(const RECT *prc) override;
    HRESULT STDMETHODCALLTYPE DoPreview() override;
    HRESULT STDMETHODCALLTYPE Unload() override;
    HRESULT STDMETHODCALLTYPE SetFocus() override;
    HRESULT STDMETHODCALLTYPE QueryFocus(HWND *phwnd) override;
    HRESULT STDMETHODCALLTYPE TranslateAccelerator(MSG *pmsg) override;

    HRESULT STDMETHODCALLTYPE SetSite(IUnknown *pUnkSite) override;
    HRESULT STDMETHODCALLTYPE GetSite(REFIID riid, void **ppvSite) override;

    HRESULT STDMETHODCALLTYPE SetBackgroundColor(COLORREF color) override;
    HRESULT STDMETHODCALLTYPE SetFont(const LOGFONTW *plf) override;
    HRESULT STDMETHODCALLTYPE SetTextColor(COLORREF color) override;

private:
    virtual ~CAbcPreviewHandler();

    void ReleaseStream();
    void DestroyPreviewWindow();
    void LoadStreamTextIntoControl();

    LONG      m_refCount;
    IStream  *m_pStream;
    IUnknown *m_pSite;
    HWND      m_hwndParent;
    HWND      m_hwndEdit;
    RECT      m_rcParent;
    HFONT     m_hFont;
};

HRESULT CAbcPreviewHandler_CreateInstance(REFIID riid, void **ppv);
