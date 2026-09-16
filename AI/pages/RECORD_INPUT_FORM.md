# Page - Record intake form

Files: `RecordInputView.xaml[.cs]`, `RecordInputViewModel.cs`, `RecordModels.cs`, and `AppDataService.cs`.

Service methods: `GetNextRecordCode`, current `GetSenderRecords` matching, legacy `FindSimilarRecord`, `SaveRecordForm`, and `DeleteRecord`.

- See `AI/features/RECORD_RESUBMISSION.md` for sender-history review before a new save.
- WPF defaults to Client; Admin intake/edit/delete operations go through LAN APIs.
- Officers do not see Intake and operate only within processing authorization.
- See area and attachment feature docs. Manual save/update keeps `AreaName = $areaName` unchanged.
