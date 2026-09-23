#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <commdlg.h>
#include <shellapi.h>
#include <string>
#include <fstream>
#include <sstream>

#pragma comment(lib, "comdlg32.lib")

const wchar_t CLASS_NAME[] = L"DemoViewerWindow";

#define IDC_EDIT  101
#define IDC_OPEN  102
#define IDC_CLOSE 103

HWND hEdit = NULL;

std::wstring Utf8ToWide(const std::string &s)
{

    if (s.empty())
        return L"";

    int len = MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.size(), NULL, 0);
    std::wstring w(len, 0);
    MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.size(), &w[0], len);
    return w;

}

void OpenFileIntoEdit(const std::wstring &path)
{

    std::ifstream f(path.c_str(), std::ios::binary);
    if (!f)
    {
        SetWindowTextW(hEdit, (L"Could not open file:\n" + path).c_str());
        return;

    }

    std::ostringstream ss;
    ss << f.rdbuf();

    std::wstring text = Utf8ToWide(ss.str());
    SetWindowTextW(hEdit, text.c_str());
}

void BrowseForFile(HWND owner)
{
    wchar_t path[MAX_PATH] = L"";

    OPENFILENAMEW ofn = { sizeof(ofn) };
    ofn.hwndOwner = owner;
    ofn.lpstrFilter = L"ABC Files (*.abc)\0*.abc\0All Files\0*.*\0";
    ofn.lpstrFile = path;
    ofn.nMaxFile = MAX_PATH;
    ofn.lpstrDefExt = L"abc";
    ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST;


    if (GetOpenFileNameW(&ofn))
        OpenFileIntoEdit(path);
}

void LayoutControls(HWND hwnd)
{
    RECT rc;
    GetClientRect(hwnd, &rc);


    int margin = 10, btnW = 90, btnH = 28;

    MoveWindow(hEdit, margin, margin,
        rc.right - margin * 2,
        rc.bottom - margin * 3 - btnH, TRUE);


    int y = rc.bottom - margin - btnH;
    MoveWindow(GetDlgItem(hwnd, IDC_OPEN), rc.right - margin - btnW * 2 - margin, y, btnW, btnH, TRUE);
    MoveWindow(GetDlgItem(hwnd, IDC_CLOSE), rc.right - margin - btnW, y, btnW, btnH, TRUE);
}

LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    switch (msg)
    {
    case WM_CREATE:
    {
        
        HFONT font = (HFONT)GetStockObject(DEFAULT_GUI_FONT);
        HINSTANCE hInst = ((LPCREATESTRUCT)lParam)->hInstance;


        hEdit = CreateWindowExW(WS_EX_CLIENTEDGE, L"EDIT", L"",
            WS_CHILD | WS_VISIBLE | WS_VSCROLL | ES_MULTILINE | ES_READONLY | ES_AUTOVSCROLL,
            0, 0, 0, 0, hwnd, (HMENU)IDC_EDIT, hInst, NULL);
        SendMessageW(hEdit, WM_SETFONT, (WPARAM)font, TRUE);

        HWND btnOpen = CreateWindowW(L"BUTTON", L"Open...",
            WS_CHILD | WS_VISIBLE, 0, 0, 0, 0, hwnd, (HMENU)IDC_OPEN, hInst, NULL);
        SendMessageW(btnOpen, WM_SETFONT, (WPARAM)font, TRUE);


        HWND btnClose = CreateWindowW(L"BUTTON", L"Close",
            WS_CHILD | WS_VISIBLE, 0, 0, 0, 0, hwnd, (HMENU)IDC_CLOSE, hInst, NULL);
        SendMessageW(btnClose, WM_SETFONT, (WPARAM)font, TRUE);

        break;


    }

    case WM_SIZE:
        LayoutControls(hwnd);
        break;

    case WM_COMMAND:
        if (LOWORD(wParam) == IDC_OPEN)
            BrowseForFile(hwnd);
        else if (LOWORD(wParam) == IDC_CLOSE)
            DestroyWindow(hwnd);
        break;

    case WM_DESTROY:
        PostQuitMessage(0);
        break;

    default:
        return DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    return 0;
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE, LPSTR, int nCmdShow)
{
    WNDCLASSW wc = { };
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = CLASS_NAME;
    wc.hCursor = LoadCursorW(NULL, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)(COLOR_BTNFACE + 1);
    RegisterClassW(&wc);

    HWND hwnd = CreateWindowW(CLASS_NAME, L"DemoViewer", WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT, CW_USEDEFAULT, 640, 480, NULL, NULL, hInstance, NULL);

    ShowWindow(hwnd, nCmdShow);
    UpdateWindow(hwnd);

    int argc;
    LPWSTR *argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (argc > 1)
        OpenFileIntoEdit(argv[1]);
    LocalFree(argv);


    MSG msg;
    while (GetMessageW(&msg, NULL, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }

    return 0;
}
