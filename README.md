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
