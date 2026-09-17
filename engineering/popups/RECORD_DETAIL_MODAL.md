# Popup - Record detail

Files: record-list XAML/code-behind/ViewModel, record models, and `AppDataService`.

Service method: `GetRecordForm`.

The modal is an overlay at the end of the root `RecordListView.xaml` Grid. Close binds `CloseDetailCommand`; attachments come from `SelectedRecordDetail.Attachments`.
