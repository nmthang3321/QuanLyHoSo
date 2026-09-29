# Page - Record intake form

Files: `RecordInputView.xaml[.cs]`, `RecordInputViewModel.cs`, `RecordModels.cs`, `RecordCodeRules.cs`, and `AppDataService.cs`.

Service methods: `GetNextRecordCode`, current `GetSenderRecords` matching, legacy `FindSimilarRecord`, `SaveRecordForm`, and `DeleteRecord`.

- See `engineering/features/RECORD_RESUBMISSION.md` for sender-history review before a new save.
- WPF defaults to Client; Admin intake/edit/delete operations go through LAN APIs.
- Officers do not see Intake and operate only within processing authorization.
- The suggested record code is editable. Accept only unique values matching `HS-<year>-<6 digits>` at both the ViewModel and service boundary.
- See area and attachment feature docs. Manual save/update keeps `AreaName = $areaName` unchanged.
