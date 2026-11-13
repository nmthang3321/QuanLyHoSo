# Page - Record intake form

Files: `RecordInputView.xaml[.cs]`, `RecordInputViewModel.cs`, `RecordModels.cs`, `RecordCodeRules.cs`, and `AppDataService.cs`.

Service methods: current `GetSenderRecords` matching, legacy `FindSimilarRecord`, `SaveRecordForm`, and `DeleteRecord`. `GetNextRecordCode` remains only as an older-client compatibility endpoint and returns an empty string.

- See `engineering/features/RECORD_RESUBMISSION.md` for sender-history review before a new save.
- New intake performs a 400 ms duplicate-history check when sender name is present. Sender name is the only required lookup key; area and phone are optional filters, so each must match when entered and is ignored when blank. Matching shows a small inline notice linked to the history overlay. Changing any check field hides the notice immediately and clears a staged resubmission choice.
- WPF defaults to Client; Admin intake/edit/delete operations go through LAN APIs.
- Officers do not see Intake and operate only within processing authorization.
- The record-code field starts blank and must be entered by the user. Accept any non-whitespace value, trim only leading/trailing whitespace, preserve casing/content, and reject duplicates at both the ViewModel and service boundary. There is no automatic generation, fixed format, forced uppercase conversion, or length limit.
- New records default `Mức độ vụ việc` to `Ít nghiêm trọng`. `Số điện thoại` and `Địa chỉ xảy ra vụ việc` are optional; the remaining required-field rules are unchanged.
- The 2026 Excel importer preserves the source `Số, ký hiệu văn bản` value as the record code, skips blank codes, and keeps only the first row when duplicate codes occur.
- While typing, the record code is checked for duplicates after a 400 ms debounce (`RefreshRecordCodeDuplicateCheck` via `GetRecordForm`): a duplicate shows a red inline warning and disables the Save button; the save flow re-checks and blocks with a warning MessageBox. Editing a record never flags its own code, and the server-side unique constraint remains the final backstop.
- In edit mode opened from the record-list pencil action, the page has no delete or cancel action. `Quay lại` is in the fixed bottom action bar beside `Cập nhật`; the former header back button was removed. New-intake mode keeps its Save and Cancel actions.
- Word attachments (`.doc`, `.docx`) expose a save-edited-copy action after opening. It refreshes the staged attachment bytes so the next Save/Update persists the edited document; PDF and image attachments do not expose this action.
- See area and attachment feature docs. Manual save/update keeps `AreaName = $areaName` unchanged.
