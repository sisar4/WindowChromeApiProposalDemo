# WindowChrome API Proposal Demo

This repository contains a **demo application** created solely to accompany and illustrate the API proposal for WPF:

> [API Proposal]: WindowChrome extensions for system frame integration, DWM caption/border colors, corner preference, backdrop and extended client area
> https://github.com/dotnet/wpf/issues/11973

## Purpose

The app showcases the scenarios described in the proposal (system frame integration, DWM caption/border colors, corner preference, backdrop, and extended client area) using several sample windows (Explorer-like, Ribbon, Tabbed, Snipping Tool-like, Task Manager-like, etc.).

## Disclaimer

This project is **not** intended for any other purpose. It is not a production-ready application, a library, or a reusable framework, and it is not affiliated with or endorsed by Microsoft or the .NET Foundation. Any UI resemblance to existing  applications is for demonstration purposes only.

Please direct any feedback or discussion about the proposal to the [issue #11973](https://github.com/dotnet/wpf/issues/11973).

## Requirements

- Windows 11 (some DWM features are not available on earlier versions)
- .NET SDK matching the target framework in the `.csproj`
- Visual Studio (recommended)

## Build and run

```powershell
dotnet run --project WindowChromeApiProposalDemo
```

## Details

Snap layout (OrchardExplorerWindow.xaml):

<img width="996" height="605" alt="Screenshot 2026-10-05 193830" src="https://github.com/user-attachments/assets/1547adc2-66f1-47c7-a0c1-256ed4513d2b" />

Full acrylic (SnippingToolWindow.xaml):

<img width="819" height="542" alt="Screenshot 2026-10-05 193905" src="https://github.com/user-attachments/assets/f4abe302-b67b-4834-8726-8d1d192a37f3" />

Partial acrylic - Extended mode (TaskManagerWindow.xaml):

<img width="930" height="591" alt="Screenshot 2026-10-05 193945" src="https://github.com/user-attachments/assets/00dfb17d-12c6-435d-9ae5-24fe59f32c61" />
<img width="531" height="578" alt="Screenshot 2026-10-05 193956" src="https://github.com/user-attachments/assets/403f9332-f86d-470c-b1c8-a5d49e5cadb3" />

Tabbed file explorer - Extended mode (TabbedExplorerWindow.xaml):

<img width="974" height="601" alt="Screenshot 2026-10-05 193059" src="https://github.com/user-attachments/assets/9a09694f-309c-4541-ac6d-55c7de931e02" />

Known HwndHost issue (ExplorerWindow.xaml):

<img width="722" height="610" alt="Screenshot 2026-10-05 192422" src="https://github.com/user-attachments/assets/eafad3d4-26f8-4ff2-8717-02c73b72e5a8" />

Fixed RibbonWindow via new api usage:

<img width="955" height="622" alt="Screenshot 2026-10-05 193226" src="https://github.com/user-attachments/assets/795921df-0256-4757-896e-8775bdf96223" />

