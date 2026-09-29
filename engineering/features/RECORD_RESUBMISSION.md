# Resubmitted resolved records and sender history

Updated: 2026-09-13. This business behavior was explicitly approved by the user.

- On new-record save, `GetSenderRecords(draft)` searches the sender's full history without a ±30-day limit. Inline overlay `SenderHistoryDialog` lets the operator review content and processing history/results.
- The sender-history grid uses five explicit columns; the Status column fills the remaining width so no blank pseudo-column appears. Its grid lines, cell spacing, and selected-row palette match the Record List. Active and inactive selection brushes are identical, keeping the selected row highlighted when the application loses focus. Dialog actions use the shared secondary/primary button styles with visible horizontal padding. The primary action always uses white text and is not visually faded when disabled.
- Saving as a new intake creates `Mới tiếp nhận`. Saving against an eligible record requires a reason, creates a new code with `Đã giải quyết — hồ sơ gửi lại`, links `OriginalRecordCode`, and creates no fake `ProcessHistories` or result documents. Eligibility (since 2026-09-29) is: same sender identity, same normalized area and case type, not trashed, and not itself a resubmission — the original's status no longer matters.
- The application never decides duplicate content automatically. The operator must confirm the same matter and no new circumstances. Resubmitted or trashed records cannot be originals; since 2026-09-29 the original may be in any status, including in-progress cases (the resubmission then continues through the original via the classify redirect).
- Sender normalization trims/collapses spaces, removes accents, and ignores case. Phone normalization keeps digits and maps +84/0084 to 0. Identity is name+phone, or name+address when both phones are missing; name alone is insufficient.
- The server resolves `SenderId`; it does not trust arbitrary client IDs. Existing matching records are assigned in the save transaction. Editing identifying fields re-resolves identity. There is no sender-merge UI.
- Record details show only the viewed group: the independent original plus resubmissions linked to that original. Intake matching still searches the sender's complete history.
- Resubmissions count toward intake totals, status distribution, lists, and export, but not resolved totals, processing progress, staff KPI, overdue counts, or the queue. Updated 2026-09-29: the classify action is shown on resubmission rows; clicking it opens the processing detail of the linked original record (via `RecentRecord.OriginalRecordCode`) and the processing page displays an amber note that the work happens on the original record. The resubmission record itself still cannot be processed directly, while an original with linked resubmissions can now be reprocessed/reopened through this flow (the former reopen block in `UpdateProcessingRecord` was removed).
- Editing retains the original link/reason and validates sender/area/case type. An original with linked resubmissions still cannot be deleted, renumbered, or have identity/matter fields changed.
- Schema adds `Records.SenderId`, `OriginalRecordCode`, `ResubmissionReason` and two indexes through idempotent migration.
- LAN routes: `records/sender-history` and `records/save-resubmission`. Client and server must be updated together.

Popup behavior:
- The former Window/ShowDialog flow is now an overlay inside `RecordInputView`; the form is locked while open and Escape/×/Cancel closes it without clearing the form.
- It auto-selects an eligible original, not simply the newest row. The whole row is selectable, commands live in the ViewModel, and ineligibility/missing-reason messages appear inline.
- A server failure leaves the overlay open. Success closes it and updates the form without an extra success dialog.

Files: record models, data-service abstraction/implementation, LAN server, record input/list/processing ViewModels, sender-history dialog, record input/list XAML, and `StatusToBrushConverter.cs`.

Tests: `RecordResubmissionTests.cs` and `RecordResubmissionViewModelTests.cs`. Migration tests use database copies; WPF previews use synthetic data. The latest historical verification was 74 passing tests (40 unit/ViewModel, 33 integration, 1 UI smoke).
