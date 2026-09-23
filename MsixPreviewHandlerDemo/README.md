# MsixPreviewHandlerDemo

A working demo of a Windows Preview Handler (COM) registered through MSIX,
for files with the `.abc` extension, packaged with Advanced Installer.

## Structure

- `src/` — the Visual Studio solution (`MsixPreviewHandlerDemo.sln`) with two projects:
  - `DemoViewer/` — the desktop (Win32) app that opens `.abc` files.
  - `AbcPreviewHandler/` — the COM DLL implementing the preview handler
    (`IPreviewHandler`, `IInitializeWithStream`, `IObjectWithSite`,
    `IPreviewHandlerVisuals`).
- `installer/` — the Advanced Installer project (`.aip`) that packages both
  binaries into an MSIX and registers the COM server + file association.
- `prebuilt/` — precompiled `DemoViewer.exe` and `AbcPreviewHandler.dll`,
  for quick testing without building.
- `TestFile.abc` — sample test file.

## Build

Open `src/MsixPreviewHandlerDemo.sln` in Visual Studio 2019/2022,
select the `Release | x64` configuration, then Build Solution.

## Packaging

Open `installer/MsixPreviewHandlerDemo.aip` in Advanced Installer,
verify the binary references (Files and Folders), then Build.

CLSID used by the preview handler: `CC12C1A5-7B5B-4367-8D3E-EBCEF1D697B9`
(replace it with your own GUID for any real project).
