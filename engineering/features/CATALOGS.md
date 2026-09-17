# Feature - Catalogs

Files: `Views\Settings\SettingsView.xaml[.cs]`, `ViewModels\SettingsViewModel.cs`, `Models\SettingsModels.cs`, and `Infrastructure\Data\AppDataService.cs`.

Service methods: `GetCatalogValues`, `GetCatalogItems`, `AddCatalogItem`, `UpdateCatalogItem`, `DeleteCatalogItem`, `UpdateCatalogItemOrders`, and `SyncProcessorCatalogFromRecords`.

Editable catalog types shown in Settings: `ReceiveSource`, `CaseType`, `Field`, `ContentGroup`, `ProcessorName`, and `ExpectedHandlingMethod`.

`Priority` remains an internal seeded catalog used by record-entry severity choices and the processing-queue `HighPriority` metric. It is intentionally absent from Settings, and `AppDataService` rejects add, update, delete, and reorder requests for this type because the metric relies on the canonical values `Nghiêm trọng`, `Rất nghiêm trọng`, and `Đặc biệt nghiêm trọng`.
