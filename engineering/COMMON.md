# Common context - QuanLyHoSo

## Overview

- Desktop application: WPF, C#, custom MVVM, `.NET 5.0-windows`.
- Projects: `QuanLyHoSo` WPF client/admin UI; `QuanLyHoSo.Core` data, network, configuration, logging, security, and document generation; `QuanLyHoSo.Shared` models/DTOs; `QuanLyHoSo.Server` WPF/tray LAN server.
- SQLite runs on the server. WPF defaults to `Client` mode and uses `QuanLyHoSo.Server` through `AdminServerUrl`.
- Default database: `%LocalAppData%\QuanLyHoSo\Data\quanlyhoso.db`.
- Default log: `%LocalAppData%\QuanLyHoSo\Logs\quanlyhoso-yyyyMMdd.log`.
- `AppLogger` retains 30 days, cleans once per day, and deletes only files matching `quanlyhoso-yyyyMMdd.log`.
- Settings: `%LocalAppData%\QuanLyHoSo\Settings\path-settings.json`.
- Main data service: `Infrastructure\Data\AppDataService.cs`.
- Shell/navigation: `ViewModels\ShellViewModel.cs`, `MainWindow.xaml`.
- Logger: `Infrastructure\Logging\AppLogger.cs`.

## Roles and authorization

Main files: `Models\AuthModels.cs`, `Infrastructure\Security\AuthContext.cs`, `ViewModels\LoginViewModel.cs`, and `Views\Auth\LoginView.xaml`.

- `Admin`: full access, including record create/edit/delete through the server when WPF runs as a client.
- `Leader`: read-only.
- `Officer`: can view, edit, classify, and process records whose `Records.ProcessorName == AuthContext.CurrentDisplayName`; cannot create or delete records.

Login, logout, and Settings user management are implemented. The seeded default account is `admin/admin123`.

## Verification

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
```

`NETSDK1138` about unsupported `.NET 5.0-windows` is an existing warning.
