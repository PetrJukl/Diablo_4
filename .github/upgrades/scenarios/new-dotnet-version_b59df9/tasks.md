# Diablo 4 WinUI 3 Migrace - Tasks

## Overview

Tento dokument sleduje migraci aplikace Diablo 4 z .NET Framework 4.8.1 + Windows Forms na .NET 8 + WinUI 3.

**Progress**: 2/2 tasks complete (100%) ![100%](https://progress-bar.xyz/100)

---

## Tasks

### [✓] TASK-001: Verify prerequisites *(Completed: 2026-03-26 18:55)*
**References**: Plan §Fáze 0

- [✓] (1) Verify .NET 8 SDK installed using `dotnet --list-sdks` command
- [✓] (2) .NET 8 SDK version 8.0 or higher present (**Verify**)
- [✓] (3) Verify WinUI 3 templates available using `dotnet new list` command
- [✓] (4) WinUI 3 templates (Microsoft.WindowsAppSDK.Templates) present in list (**Verify**)

---

### [✓] TASK-002: Atomic WinUI 3 migration *(Completed: 2026-03-26 20:41)*
**References**: Plan §Fáze 1-5, Plan §Breaking Changes, Plan §Detailed Execution Steps

- [✓] (1) Create new WinUI 3 Desktop project (Blank App, Packaged) targeting .NET 8 and establish folder structure per Plan §Fáze 1
- [✓] (2) Migrate all business logic classes to appropriate folders: ProcessMonitor to /Services, utility methods to /Helpers per Plan §Fáze 1
- [✓] (3) Update ProcessMonitor to use DispatcherTimer instead of System.Timers.Timer per Plan §Fáze 1
- [✓] (4) Create MainWindow.xaml with background image, 3 TextBlocks, and data binding per Plan §Fáze 2
- [✓] (5) Implement MainViewModel with ObservableProperties and timer logic per Plan §Fáze 2
- [✓] (6) Create WeekendMotivationDialog as ContentDialog with ComboBox and game launching logic per Plan §Fáze 3
- [✓] (7) Install H.NotifyIcon.WinUI NuGet package and configure TaskbarIcon in App.xaml per Plan §Fáze 4
- [✓] (8) Implement tray behavior: left click restore, right click menu, hide on close per Plan §Fáze 4
- [✓] (9) Create UpdateService in /Services with GitHub manifest parsing and version comparison per Plan §Fáze 5
- [✓] (10) Integrate update check in App.OnLaunched with GitHub manifest URL per Plan §Fáze 5
- [✓] (11) Build solution and fix all compilation errors referenced in Plan §Breaking Changes
- [✓] (12) Solution builds with 0 errors (**Verify**)
- [✓] (13) Commit changes with message: "TASK-002: Complete WinUI 3 migration"

---





