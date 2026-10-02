# Session handoff - QuanLyHoSo

Last updated: 2026-09-30

## Current application changes - 2026-09-30

- The record-input edit page opened from the record-list pencil now has only `Quay lại` and `Cập nhật` in its fixed bottom action bar. The former header back button and edit-mode `Xóa`/`Hủy bỏ` actions were removed; new-intake Save/Cancel behavior is unchanged.
- Intake duplicate detection is now proactive: after sender name and area are entered, a 400 ms check shows a top-of-page warning beside the existing resubmission banner when matching history exists; an entered phone must match after normalization, while a blank phone is ignored. The warning disappears immediately when any check field changes. Its link opens the existing comparison overlay, now without the blue availability note or Save-as-new action; `Xác nhận hồ sơ gửi lại` is enabled only for an original-record row and disabled for a resubmission row, `Lý do gửi lại *` is required, and confirming stages the selected original until the fully validated main form is saved. Ignoring the warning and pressing the main Save action creates an ordinary new intake. Case type, content group, field, and related person are not duplicate-link conditions. Server validation mirrors the same rule. The receiver ComboBox binds by selected value so its displayed officer remains synchronized through overlay confirmation. Verification: Release build passed; unit 69/69 and integration 68/68.
- The processing queue now has an eighth metric/filter card, `ĐÃ XỬ LÝ`, which counts and lists records whose stored status is `Đã giải quyết`. Automatically resolved resubmission rows are excluded because they are not processed independently. The existing dynamic four-column metric grid displays the eight cards as two complete rows; the filter keeps the same role scope, received-date range, pagination, and secondary-filter reset behavior as the other cards.
- The record-list table no longer flashes a phantom trailing column when the page opens (two combined fixes). Root cause one: the seven columns whose `Visibility` binds through the `ViewModelProxy` binding proxy while hidden by default (the six optional document/transfer columns and the selection "Chọn" column) evaluated their bindings before the cached ViewModel reached the proxy (`Data` still null → binding failure → column rendered with the default `Visible` state, then collapsed once the DataContext arrived). Each of those bindings now carries `FallbackValue=Collapsed`. Root cause two: the reload flow raised `TableHeight` (the grid's `MaxHeight`) only after the whole batch of rows was re-added, so during the reload the grid rendered against a stale height and flashed a vertical scrollbar strip at the table's right edge that vanished on the final layout pass. `Records.CollectionChanged` now raises `TableHeight` immediately, keeping `MaxHeight` in exact lockstep with the row count so the scrollbar never appears. Columns visible by default are untouched. New unit test `RecordListTableHeightTests.TableHeight_ShouldUpdateImmediatelyOnCollectionChanges`; no service or schema change.
- Duplicate complaint detection at intake was hardened: `GetSenderRecords` no longer silently returns nothing when the typed sender has no phone. The strict identity rules are unchanged (name + phone, or name + address when both phones are empty; confirmed rows remain linkable as resubmissions), but when the strict match finds nothing the service now falls back to a name-only comparison and flags those rows `SenderRecordHistory.IsConfirmedSender = false` (additive JSON). Unconfirmed rows render "Trùng tên, chưa xác thực" in the new sender-history "Xác thực" column, can never be linked (`CanLinkAsResubmission` requires `IsConfirmedSender`), and server-side `ValidateResubmission` still enforces the strict identity, so older clients cannot bypass it. `ResubmissionAvailability` explains how to confirm (enter the matching phone). `SenderId` resolution is untouched — name-only matches never merge identities. Two legacy tests were updated to the new warning behavior (`SameNameWithDifferentPhone_ShouldWarnWithoutConfirmingAndNewCaseShouldRemainOpen`, `SenderWithoutPhone_ShouldConfirmByAddressAndWarnByNameOtherwise`); new coverage in `SenderRecordIdentityTests` and `SenderHistoryMatchingTests`.
- Restore now keeps the built-in `admin` login and migrates the restored database immediately. `RestoreDatabaseOnly` captures the current `admin` `PasswordHash`/`MustChangePassword` before the swap and reapplies them after the restore, and the restore runs the same idempotent preparation sequence a fresh server start runs (`Initialize`'s database steps were extracted into `PrepareDatabase(connection, seedSampleRecords)` — schema migrations, seeds, normalizations; sample seeding stays startup-only). A restored older-schema backup is therefore usable without a server restart, fixing the "no such column: CommanderApproverName" record-list breakage class observed after today's restore. Every other account comes from the restored database. Coverage: `RestoreBehaviorTests` (admin password preserved across restore; legacy `.db` without the 2026-09-29 columns restores working and record-lists fine). Test totals after both changes: unit 69/69, integration 66/66.
- The staff-tracking performance chart was redesigned for large officer counts (36+) and now ranks officers with an objective composite score instead of the raw on-time rate (which rewarded the empty-sample 100% rule): `composite = 60% on-time rate + 40% completed volume`, where completed volume is CompletedCount relative to the highest CompletedCount in the period. Officers with no records at all in the period are excluded. The card "TOP 5 CÁN BỘ XUẤT SẮC" is visible to every role as a public commendation board — officers load it through the new aggregate LAN route `staff/top-officers` (`AppDataService.GetTopOfficers`, computed server-side without the officer self-scope; the detail table and info panel stay scoped to their own data, and the client call is try/caught for older-server compatibility). The card is styled exactly like the Dashboard "HỒ SƠ THEO ĐỊA BÀN - TOP 5" card (name | single horizontal bar | composite score, bar ∝ score, max 270px) with a `MetricInfoButton` (i) icon next to the title explaining the formula, and a bar tooltip showing the full breakdown (on-time x% with a/b sample counts, completed volume y%, composite score, evaluation). `StaffPerformanceRow` gained `OnTimeCompletedCount`/`DeadlineTrackedCount` from the service. The full per-officer statistics remain in the paginated table above. `StaffBarStat` was replaced by a slim `StaffPerformanceBarRow`; `AppDataService.EvaluateTopOfficers` (public static) is covered by six unit tests (unit suite 60/60, integration 59/59). Bars animate horizontally (`AnimateScaleX`).

## Current application changes - 2026-09-29

- The record-detail popup's linked-records block was redesigned as a bottom-of-popup card titled `Danh sách hồ sơ gửi lại` (after attachments), visible only when the record belongs to a resubmission group (`RecordFormDraft.HasLinkedRecords`: more than one sender-history row, or a resubmission reason is present). The status chip sits in the popup header next to the record code so every record keeps showing status. Each linked record renders as a collapsed group-down row — record code, a `Hồ sơ gốc`/`Gửi lại <code>` tag, and the colored status chip; expanding shows receive date, case type, and for resubmission rows the `Lý do gửi lại` (per-row `SenderRecordHistory.ResubmissionReason`, newly selected by `GetSenderRecords` — additive JSON) while the processing history block only renders for original rows. Every row keeps a `Xem chi tiết hồ sơ này` button (existing `OpenSenderRecordCommand`). Status text uses the shared `RecordStatusDisplay.GetDisplay` mapping (also used by `RecordListRowViewModel.StatusDisplay`), so the stored `Đã giải quyết — hồ sơ gửi lại` renders as the short colored `Hồ sơ gửi lại` chip. Implemented in `RecordListView.xaml` (new `LinkedRecordExpanderTemplate`) plus get-only computed properties on the models; no database schema change.
- The sender-history comparison dialog (intake) no longer turns selected-row text blue — selecting a row highlights only the row area (background plus border), while text keeps the normal color. Its three actions were synced to the record-input action-bar format: 12px spacing, MinWidth 128 secondary / 138 primary, save glyph on the primary. `Lưu là hồ sơ gửi lại` now binds `IsEnabled` to `CanSaveResubmission` (enabled only when a linkable original row is selected) and dims to 45% opacity while disabled via the dialog-local `PrimaryButtonWithDisabledState` style — the only primary button in the app with a visible disabled state.
- A sweep removed every remaining user-visible raw `Đã giải quyết — hồ sơ gửi lại` string: the record-list status filter dropdown now renders the short label through the new `StatusDisplayConverter` (items stay stored values, so queries are unchanged), the processing-detail status chip binds `ProcessingRecordDetail.StatusDisplay`, the trash grid "Trạng thái trước khi xóa" binds `DeletedRecord.StatusDisplay`, the sender-history dialog status column binds `SenderRecordHistory.StatusDisplay`, the intake resubmission banner (`ResubmissionSummary`) uses `RecordStatusDisplay.GetDisplay`, and dashboard status distribution labels (`GetStatusStats` names) are mapped at read time while colors still derive from the stored status. Clipboard content, exports, and technical logs intentionally keep the full stored value. The processing-queue task cards also dropped the `WorkLabel` action text and now show the record code with the colored status chip inline.

- The record input form checks the typed record code for duplicates (400 ms debounce) and shows a red inline warning plus a disabled Save button; the save flow re-checks and blocks with a warning MessageBox. Editing never flags a record's own code. Covered by the `DuplicateRecordCode_ShouldWarnAndBlockSave` unit test; the server-side unique constraint remains the backstop.

- In the record list, resubmission rows display the status chip as `Hồ sơ gửi lại` with a dedicated magenta color (#B42467 on #FCE3EF) instead of sharing the resolved green; `StatusDisplay` performs the mapping while stored status, clipboard, and exports keep the full value. Filter dropdowns also render the short label (via `StatusDisplayConverter`) while still querying by the stored value.

- The processing update card no longer has a `Ghi chú` field; `Nội dung xử lý` starts blank and the typed content always appears in the refreshed processing history after an update (server keeps the record's stored note when the request note is empty). `Người xử lý` is locked to the signed-in officer and only Admin can change it (`CanChangeProcessor`).

- Resubmission linking no longer requires the original to be resolved: `CanLinkAsResubmission` and `ValidateResubmission` accept any original status (still same sender identity, area, and case type; never a resubmission or trashed record). The intake dialog shows an informational note for in-progress originals ("Hồ sơ gốc chưa giải quyết; có thể lưu gửi lại và tiếp tục xử lý trên hồ sơ gốc."). Covered by `Resubmission_ShouldAllowUnresolvedOriginal` (integration) and the rewritten `Popup_ShouldAllowUnresolvedOriginalAndExplainUnavailableSelection` (unit).

- Resubmission rows in the record list now expose the classify action; it opens the processing detail of the linked original record (list rows carry `OriginalRecordCode`) with an amber "đang xử lý trên hồ sơ gốc" banner on the processing page. The former reopen block for originals with linked resubmissions was removed from `UpdateProcessingRecord` (delete/renumber/identity protections remain; the resubmission record itself still cannot be processed). This revises the 2026-09-13 policy. Verified by the updated `LinkedRecords_ShouldKeepReferenceOnEditAndAllowReprocessing` integration test and the new `ResubmissionRow_ShouldAllowClassifyAndPointToOriginal` unit test.

- The record list gained six optional columns ("Cột hiển thị"): `Chỉ huy duyệt`, `Lãnh đạo duyệt`, `Phiếu chuyển đơn số`, `Ngày chuyển đơn` (persisted on `Records` when step-5 documents are generated — new `EnsureInitialResultDocumentSchema` migration), `Đơn vị chuyển đến` (`AreaName` for step-6+ records), and `Kết quả của đơn vị` (step-8 history content of resolved records). The six columns are hidden by default, are also exported when visible, and are covered by the `FilteredRecords_ShouldExposeDocumentAndTransferColumns` integration test. Client/server remain compatible across versions because the list model change is additive JSON.
- The `Chuyển cơ quan khác` processing-history milestone now records the destination agency (`Chuyển đến: <agency>.`); re-transfers update the existing milestone, and older milestones fall back to the record's current destination agency at read time. Covered by the eight-step-workflow integration test.
- The record-list table adjusts its maximum height to the number of rows currently loaded (`38 + row count × 34`). Short result sets contract vertically; longer sets remain constrained by the available page height and scroll internally. Wheel over the table still closes the area filter popup.
- The processing-detail page keeps `Quay lại`, `Hủy xử lý`, and `Cập nhật` in a fixed bottom action bar that stays visible while scrolling; the old header back button and the in-card button pair were removed. `Hủy xử lý`/`Cập nhật` remain gated by `CanUpdateProcessing`.
- Record code is editable at intake and accepts externally supplied values only in the form `HS-<year>-<6 digits>`; duplicate codes are rejected. Automatic next-code generation remains the default convenience value.
- A fresh production database no longer seeds processor names. Sample-data mode still contains named processors for demonstrations.
- All new attachment bytes are uploaded to the Server and stored below `QuanLyHoSoFiles\Attachments`; generated Word files are stored below `QuanLyHoSoFiles\GeneratedDocuments`. Clients download files through the authenticated API and use local copies only as cache. Accessible legacy paths migrate into managed storage during initialization.
- The normal `.qlhbackup` format includes SQLite plus all server-managed attachments and generated documents. Restore stages and validates the package and creates a complete pre-restore safety package. Legacy `.db` files remain supported but are database-only.
- The processing strip now has exactly eight steps: `Tiếp nhận`, `Phân loại`, `Phân công`, `Xác minh`, `Kết quả xử lý ban đầu`, `Chuyển cơ quan khác`, `Chờ kết quả`, and `Lưu hồ sơ`. Old `Đang chờ bổ sung tài liệu` values normalize to step 5; `Đã giải quyết` maps to step 8.
- Saving `Chuyển cơ quan khác` records the transfer step and automatically advances the current status to `Chờ kết quả`.
- The initial-result document popup starts `Nhận xét` and `Đề xuất` blank and requires both as multiline text. It also requires `Tên đội trưởng` and `Tên lãnh đạo duyệt`; `Cán bộ đề xuất` comes from the record's current processor. Generated Word documents preserve line breaks.
- Release build verification completed with zero errors. Unit tests passed 56/56 and integration tests passed 57/57. Existing warnings include .NET 5 end-of-support and NU1900 when NuGet vulnerability metadata is unavailable.
- Markdown documentation was updated for these changes. The customer PDF was intentionally not regenerated in this documentation pass.

## Independent Client updates - 2026-09-28

- Client and Server product versions are independent. LAN health and business routes no longer require exact product-version equality, and the Client installer checks server availability rather than version compatibility.
- For rollout against an older strict Server, the new Client automatically adopts the Server-requested value in the legacy `X-QuanLyHoSo-Version` header after health checking, while reporting its real version in `X-QuanLyHoSo-Client-Version`. This permits a Client-only upgrade without first updating the Server.
- The Settings update flow updates only the current Client workstation. Internal discovery and download accept only packages named `QuanLyHoSo-Client-<version>.zip`; Server packages are never selected.
- The legacy health response version fields remain for compatibility and diagnostics. `ServerVersion` reports the actual Server version, `RequiredClientVersion` is empty, and `IsClientVersionSupported` is always true.
- Verification completed after the change: Release build passed with zero errors; the full non-UI suite passed 87 tests (43 unit and 44 integration), including different-version and older-Server compatibility regressions; smoke passed 6/6.
- The customer PDF was regenerated from the canonical builder. It remains 58 A4 portrait pages with zero rotation and embedded Arial; every rendered page was visually reviewed, and stale exact-version requirement phrases are absent.

## Role-aware Client/Server updates - 2026-10-02

- The Settings card is titled `CẬP NHẬT PHẦN MỀM`. Officer and Leader roles remain Client-only; Admin can choose Client only or Server and Client.
- Internal release packages now include both `QuanLyHoSo-Client-<version>.zip` and `QuanLyHoSo-Server-<version>.zip`. Client and Server versions are still compared independently.
- A Server update is authorized by the Admin LAN session, launches an elevated updater on the Server machine, restarts the Server, then restarts the current Client at Login. Client-only updates also restart at Login.
- The Client package is downloaded before a combined Server restart so the cleared Server session cannot interrupt the Client package download. New Clients retain a Client-only fallback against older Servers that do not expose the update-overview route.

## Portable restored attachment paths - 2026-10-02

- `HS-2026-000002` exposed absolute attachment metadata pointing at an old `C:\ProgramData\QuanLyHoSo\...` Server root while the packaged files were correctly present under the current `%LocalAppData%\QuanLyHoSo\Data\QuanLyHoSoFiles` root.
- Attachment lookup now falls back to the canonical current Server path by record code and filename, repairs `RecordAttachments.FilePath`, and logs `Attachments/RebaseStoredPath`. Server initialization and `.qlhbackup` restore bulk-rebase the same stale metadata when the physical file exists.
- The reported backup contained all seven attachment/generated-document entries for `HS-2026-000002`; this was a stale absolute-path defect, not backup data loss.
- After path rebasing exposed the next failure, `HS-2026-000002` showed that `HttpListener` rejected Vietnamese filenames placed directly in `Content-Disposition`. File responses now send an ASCII fallback plus RFC 5987 UTF-8 `filename*`. The reported PDF was verified byte-identical to the backup, readable as an unencrypted four-page PDF, and visually rendered successfully.

This file is the compact starting context for future maintenance sessions. For detailed behavior, follow the links in `engineering/INDEX.md` instead of loading every document.

## Start here

1. Read `engineering/INDEX.md` and `engineering/RULES.md`.
2. Run `git status --short --branch`.
3. Read only the page, popup, feature, or infrastructure document relevant to the current task.

Documentation routing:

- Small pages: `engineering/pages/*.md`
- Popups and overlays: `engineering/popups/*.md`
- Shared features: `engineering/features/*.md`
- Database, LAN, build, and testing: `engineering/infra/*.md`
- Automated-test inventory: `engineering/infra/TEST_MATRIX.md`

## Documentation language policy

- All engineering/developer-facing files are consolidated under `engineering/` and written in English.
- The root `README.md` and customer-facing documents under `doc/` remain in Vietnamese unless the user explicitly requests otherwise.
- Vietnamese UI labels, business statuses, database values, and exact user-visible messages remain unchanged when referenced in English technical documentation.

## Current uncommitted documentation-template change

- `phieu_de_xuat.docx`, `phieu_huong_dan.docx`, and `thong_bao.docx` were moved from `doc/` to `doc/templates/`.
- The client, server, integration tests, project copy rules, and `InitialResultDocumentGenerator` now use `doc/templates/`.
- Output filenames and document-generation behavior are unchanged.
- Verification completed before the documentation translation: client/server builds passed, five document tests passed, and the smoke suite passed 6/6.

## Fresh customer database initialization - 2026-09-17

- Normal production initialization creates the built-in `admin`, standard areas, and all seven default catalog groups, but creates no records.
- The 105 demo records are seeded only when the server is started with the explicit `--sample-data` option. That mode still recreates its isolated sample database from `SampleData/quanlyhoso-demo.db` on every start.
- Existing customer databases are preserved during application updates; the change does not delete or replace existing records.
- Regression coverage verifies both the empty production database and the explicit 105-record sample seed.

## Protected severity catalog - 2026-09-17

- `Priority` (`Mức độ vụ việc`) remains seeded for record entry but is no longer displayed in the Settings catalog list.
- Its canonical values drive the processing-queue high-priority card/filter, so the data service rejects add, update, delete, and reorder requests for this catalog, including requests from older LAN clients.
- The six other system catalog groups remain editable by Admin.

## Record intake defaults and 2026 data update - 2026-09-30

- New intake forms default `Mức độ vụ việc` to `Ít nghiêm trọng`; phone number and incident address are optional in both the UI and validation.
- `tools/import_nam2026.py` applies the same severity default and defaults both receiver and processor to `Lê Võ Mỹ Ý`, including update-existing runs.
- Imported record codes use column A (`STT`) from the workbook instead of the physical worksheet row. A single broken `#REF!` between 4400 and 4402 is deterministically inferred as 4401; the one unnumbered extra record uses the next available number, 4459, and both exceptions are recorded in `AdditionalNote` for manual review.
- The local production database was backed up, then all 4,429 `HS-2026-*` records were updated to severity `Ít nghiêm trọng` and processor `Lê Võ Mỹ Ý`; the same processor was applied to their 779 processing-history rows. Post-update SQLite integrity and row-count verification passed.
- The local production database received a second safety backup before 350 record codes were remapped to the Excel `STT`. All 4,429 final codes match the computed source mapping, the two exceptional rows are annotated, no temporary migration codes remain, and SQLite integrity passed.
- Verification passed: 62 unit tests, 59 integration tests, and 8 smoke tests.

## Automatic backup and Settings layout - 2026-09-16

- The server checks automatic backup on startup and once per hour while running. It creates a new complete package when the latest automatic backup is at least seven days old.
- Automatic filenames use `quanlyhoso_auto_yyyyMMdd_HHmmss.qlhbackup` under `%LocalAppData%\QuanLyHoSo\Backup` on the server.
- Cleanup retains the ten newest recognized `.qlhbackup` and legacy `.db` files across automatic, manual, and pre-restore safety backups. Cleanup runs on each automatic check.
- The Data Backup card is split into automatic-backup status, manual backup, and restore sections. Long paths use ellipsis and tooltips; restore uses a mild warning treatment.
- In the Admin layout, Data Backup is to the left of Quick Actions. Quick Actions is top-aligned and keeps its content height instead of stretching to match the backup card.
- Restore uploads use staging data that is deleted after the restore attempt finishes. A complete pre-restore `.qlhbackup` safety package is created before live database/files are replaced.

See `engineering/features/BACKUP_RESTORE.md` and `engineering/pages/SETTINGS_HOME.md`.

## Initial-result transfer documents - 2026-09-14

- Confirming document creation opens an overlay for transfer number, transfer date, complaint-forwarding date, team leader, approving leader, review, and proposal. These values are required; cancel keeps the form, success closes the overlay, and errors keep it open.
- Review and proposal always open blank, accept long multiline input, and do not read stored record notes. The proposing officer comes from the current `Người xử lý` field.
- `InitialResultDocumentDetails` travels through the service/LAN boundary to the generator. Client and server must therefore be updated together; no schema change was introduced.
- The extra values are used only for the current document-generation operation and are not stored as record fields.
- The three Word templates preserve their existing runs/styles and map the highlighted regions to the new values. The generator preserves review/proposal line breaks and replaces the complete static leader name so no rank prefix remains.

## Processing and form behavior - 2026-09-14

- The workflow has eight Vietnamese guidance icons in business order. `Chuyển cơ quan khác` is step 6, `Chờ kết quả` is step 7, and `Lưu hồ sơ` is step 8; no leadership-approval icon is displayed.
- Destination agency is required when forwarding to another agency.
- The record-input action bar is fixed below the scrollable content and is disabled while the comparison overlay is open.
- Phone number and incident address are optional. The other General Information fields remain required, including contact address and record code. New records default severity to `Ít nghiêm trọng`; record code is editable but must match `HS-<year>-<6 digits>` and be unique, and whitespace-only required values are rejected.
- Leaders can open processing details in read-only mode but cannot save, delete, or otherwise update processing.
- All roles land on Dashboard after sign-in, including after mandatory password changes.
- Processing details now fall back to the record-form attachment list when the processing payload contains no attachments, preventing persisted files from being shown as missing. A ViewModel regression test covers this case.
- The save-edited-copy attachment action is limited to Word files (`.doc`, `.docx`) and is hidden for PDF/images. The same action is available in intake/edit attachment lists; it reloads the edited Word bytes before the form Save/Update persists them.

See `engineering/pages/PROCESSING_DETAIL.md`, `engineering/pages/RECORD_INPUT_FORM.md`, and `engineering/features/NAVIGATION.md`.

## Tables, metrics, and selectable text - 2026-09-14

- Sidebar navigation caches page ViewModels for the signed-in session, so revisiting a page no longer repeats constructor-time LAN calls. List, processing, and staff refreshes run in the background; independent first-visit catalog calls run concurrently. Input and Processing direct-navigation cleanup behavior is preserved.
- Dashboard LAN loading no longer stacks six independent round trips sequentially. The requests start concurrently, and an explicit loading surface covers the initially empty charts until the snapshot is ready; a regression test verifies both behaviors.
- Record-list text is selectable and copyable. Full-row selection remains visually distinct; multi-record checkboxes continue to use their own data selection state.
- Double-clicking text selects the full cell text. Dashboard task cards preserve text selection while empty card space remains clickable.
- Shared metric info icons explain dashboard, processing, and staff-tracking calculations in Vietnamese tooltips.
- Staff KPI and status use the current on-time-rate formula and the 90%/80% thresholds. The query logic was not changed.

## Resubmitted records - 2026-09-13

- New input compares unlimited sender history. The officer may save an independent record or link a resubmission to an already resolved original record; a reason is mandatory for resubmission.
- A resubmission keeps its own code, receive date, content, and attachments; it uses status `Đã giải quyết — hồ sơ gửi lại` and links to the original record.
- Resubmissions do not enter the pending queue, inflate resolved/KPI counts, or generate fake verification history.
- The server determines normalized sender identity. A sender name alone is not sufficient identification, and sender-merging is not implemented.
- Schema changes consist of three metadata columns and two indexes with idempotent migration. Client and server must be upgraded together.
- The server protects links when editing, deleting, changing record code/sender/incident, or reopening an original record.
- The sender-history comparison grid no longer leaves a blank area resembling an extra column. Its grid lines, cell spacing, selected-row colors, and secondary/primary actions now match the Record List and shared application styles; the selected row remains highlighted while the application is inactive. Shared button templates apply their declared horizontal padding. Primary buttons always render white text and are not visually faded when disabled.

See `engineering/features/RECORD_RESUBMISSION.md`.

## Customer documentation

- Development builds now default both Client and Server assembly/product versions to `1.1.0`, matching the latest repository tag. Login and Settings display the running assembly version instead of a hard-coded `1.0.0` string.

- Release `v1.0.0` was published at `https://github.com/nmthang3321/QuanLyHoSo/releases/tag/v1.0.0`, from tagged commit `89d9c4b`. It contains exactly four uploaded assets: Server setup, Client setup, the 58-page portrait customer PDF, and `SHA256.txt`. Both installers compiled locally with product version 1.0.0; all four uploaded assets were downloaded and SHA-256 verified before publication. GitHub Actions run `35236552104` could not start because the account was locked due to a billing issue, so publication used the verified local build and GitHub Releases API. Resolve billing before relying on future tag-triggered builds. The Client installer compile error was fixed by assigning COM `ResponseText` to a Pascal string before calling `Pos`.
- Customer release assets are limited to the Server setup EXE, Client setup EXE, matching-version customer PDF, and `SHA256.txt` (hashing the first three files). `scripts/build-release.ps1` and `.github/workflows/release.yml` share this packaging flow; update ZIPs are not attached. A `v<version>` tag triggers verification, build, and publication.
- For subsequent PDF content edits, first read `engineering/infra/CUSTOMER_PDF.md`. Reuse the canonical builder at `engineering/scripts/build_customer_pdf.py` and its existing format; update the source Markdown/GUI images or `appendix_story()` as appropriate. Do not restart the layout or use the obsolete scratch script in `tmp/pdfs/`. Re-exporting still recalculates pagination and contents, but no new template is needed. The runbook records typography, colors, margins, image rules, metadata locations, and verification steps.
- On 2026-09-17, `doc/QuanLyHoSo_TaiLieu_KhachHang_1.0.0.pdf` was rebuilt with embedded Arial, white bold table headers, lossless original GUI images, and enlarged dialog viewports. The user subsequently required every page to be portrait: the latest export has 58 A4 portrait pages, zero rotation, and no landscape pages. All pages were visually reviewed; all 33 unique embedded images matched original PNG pixels, with no text outside page bounds. The canonical builder now enforces portrait-only layout. Regeneration instructions: `engineering/infra/CUSTOMER_PDF.md` and `engineering/scripts/build_customer_pdf.py`.
- `doc/Huong_dan_su_dung_QuanLyHoSo.md` was refreshed on 2026-09-16 to use the current Admin, Leader, Officer, and Server screenshots.
- The user guide now documents sender-history comparison/resubmission, the initial-result document preview, automatic seven-day backups with ten-file retention, and restore safety/temp-file behavior.

## Architecture snapshot

- The solution is split into `QuanLyHoSo.Shared`, `QuanLyHoSo.Core`, the WPF client, and `QuanLyHoSo.Server`.
- The server owns SQLite, logs, backup/restore, and the LAN API. The WPF application normally runs in Client mode; only explicit `AdminHost` mode uses a local database.
- The server has a compact WPF administration/tray UI. Closing or minimizing hides it to the tray; only the explicit exit action stops the server.
- The client sends a 30-second heartbeat. The server considers unique machine names active for 90 seconds.
- The server can start/stop LAN listening, reset the built-in `admin`, open data/log folders, and copy the client URL.
- Attachment operations upload content to Server-managed storage. Authenticated clients download the durable copy; full backup/restore includes the managed file tree.
- Technical log files matching `quanlyhoso-yyyyMMdd.log` are retained for 30 days.

## Verification baseline

- Latest noninteractive verification on 2026-09-29: solution build (Debug) completed with zero errors; unit tests passed 58/58 and integration tests passed 58/58, covering the new transfer-history, record-list-column, resubmission-processing, and duplicate-record-code regressions. UI tests were not included. Build warnings included the existing .NET 5 end-of-support warning and NU1900 because NuGet vulnerability metadata could not be fetched. The build ran on the Windows SDK invoked from the WSL shell (`dotnet.exe` interop); no .NET installation inside WSL is possible because the Bosch DNS does not resolve public hosts.
- Historical full verification on 2026-09-14: Release solution build and `tests/Scripts/run-all.ps1 -IncludeUI` passed 80 tests (40 unit, 39 integration, 1 UI smoke).
- Test projects target .NET 8 while the application remains on .NET 5.
- Each integration test uses an isolated temporary database. Generated test reports are ignored by Git.
- Existing tests do not constitute full feature coverage. Gaps include full authenticated UI workflows, all processing transitions, LAN failures/concurrency, export contents, and file-lock/permission failures.
- Re-run verification after code changes; historical results are context, not proof for new work.
