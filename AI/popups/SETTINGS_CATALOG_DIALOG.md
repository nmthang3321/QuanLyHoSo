# Popup - Settings catalog dialog

Dung khi task lien quan popup quan ly danh muc.

Files:
- `Views\Settings\SettingsView.xaml`
- `Views\Settings\SettingsView.xaml.cs`
- `ViewModels\SettingsViewModel.cs`
- `Models\SettingsModels.cs`
- `Infrastructure\Data\AppDataService.cs`

State/commands:
- `IsCatalogDialogOpen`
- `OpenCatalogDialogCommand`
- `CloseCatalogDialogCommand`
- `SaveCatalogValueCommand`
- `CancelCatalogEditCommand`
- `SelectCatalogValueCommand`
- `DeleteCatalogValueForRowCommand`

Catalog groups:
- `ReceiveSource`
- `CaseType`
- `Field`
- `ContentGroup`
- `Priority`
- `ProcessorName`
- `ExpectedHandlingMethod`

Notes:
- ListBox `CatalogValuesListBox` ho tro keo tha sap xep trong code-behind.
- `ProcessorName` duoc sync tu `Records.ProcessorName`.

