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

Sidebar destinations are cached for the signed-in session instead of being disposed and recreated on every click. Returning to a previously opened page therefore switches immediately and preserves its current filters. Direct Input navigation still prepares a blank form, and direct Processing navigation still closes details/popups and returns to the queue.

Record-list, processing-queue, staff-tracking, and Dashboard refreshes run their independent LAN reads in the background. Catalog reads required on the first visit to Input, Record List, and Processing start concurrently, avoiding stacked network round trips. Signing out still disposes every cached page ViewModel.
