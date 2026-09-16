# Popup - Settings catalog management

Files: Settings XAML/code-behind/ViewModel, Settings models, and `AppDataService`.

State/commands: `IsCatalogDialogOpen`, open/close, save/cancel edit, select row, and delete row commands.

Catalog groups: `ReceiveSource`, `CaseType`, `Field`, `ContentGroup`, `Priority`, `ProcessorName`, and `ExpectedHandlingMethod`.

`CatalogValuesListBox` supports drag/drop ordering in code-behind. `ProcessorName` is synchronized from `Records.ProcessorName`.
