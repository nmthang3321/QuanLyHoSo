# Feature - Navigation, back behavior, and sidebar state

Files: `ViewModels\ShellViewModel.cs`, `MainWindow.xaml`, and the source/destination page ViewModels.

Required flow:

```text
Record list selected in sidebar
-> open details/classification from a row
-> processing details open while Record list remains selected
-> Back returns to Record list with the same sidebar selection
```

Relevant methods: `ShellViewModel.NavigateTo(key, selectedNavigationKey)`, `ClassifyRecordFromList(...)`, `RecordProcessingViewModel.OpenRecord(..., returnToPreviousPage: true)`, `BackToQueue()`, and `PrepareQueue()`.

Directly clicking the Processing sidebar calls `PrepareQueue()` to close details/popups, clear the return-to-source flag, and reload the queue. Opening classification from the record list preserves details and Back returns to the list. After a direct Processing sidebar click, Back from queue-opened details returns only to the queue.

Every direct sidebar click uses `NavigateTo(..., resetPage: true)` and recreates the destination ViewModel, clearing filters and temporary dialog/detail state. Internal flows such as edit, classify-from-list, and Back do not reset the ViewModel.
