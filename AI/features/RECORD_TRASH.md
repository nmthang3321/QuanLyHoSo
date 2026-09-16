# Feature - Record trash

- Admin opens an inline trash overlay from the record list. It supports search by code/sender/deleting user, row/multi/all-result selection, restore, and permanent deletion.
- Normal delete is soft delete. The record list shows no undo/success banner; restoration happens through Trash.
- Permanent delete requires confirmation (default No) and transactionally removes `Records`, `ProcessHistories`, and `RecordAttachments`, while preserving physical files because paths may be shared or user-owned. Trash is not automatically purged.
- `PermanentlyDeleteRecord(code, batchId)` and `records/trash/delete-permanently` delete only a still-trashed record with the matching deletion batch. Results are logged and the overlay reloads.

Data/API:
- `Records` adds `DeletedAt`, `DeletedBy`, and `DeletionBatchId` as non-null text with empty defaults. Migration is idempotent.
- `DeleteRecord(code, batchId)` marks an active row while preserving IDs, workflow state, processor, business timestamps, attachments, and history.
- `RestoreRecord(code, batchId)` requires the matching batch, preventing stale restore actions after a later deletion.
- Trash operations enforce Admin authorization in the service.
- New clients use `records/trash/move`; `records/delete` remains a soft-delete compatibility alias. `records/trash` returns `DeletedRecord[]`; `records/restore` accepts code+batch and returns bool.
- Business queries use `(SELECT * FROM Records WHERE DeletedAt = '') AS Records` to exclude trash from lists, details, dashboard, staff, queue, export/count, and duplicate matching. Code generation and seed logic still consider all rows to prevent code reuse.
- Save/update rejects inactive records. Move-to-trash and restore write `SystemLogs`.

Main files: `RecordTrashViewModel`, trash/list views, `AppDataService`, LAN server/DTOs, and record models. Client and server must be updated together.
