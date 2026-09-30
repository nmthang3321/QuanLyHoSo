# Duplicate complaint detection report — 2026-09-30

Plan: `engineering/plans/2026-09-30-duplicate-complaint-detection-plan.md`

## What changed

1. `Models/RecordModels.cs` — `SenderRecordHistory` gained `IsConfirmedSender`
   (defaults to true for backward compatibility with older server payloads) and
   `SenderMatchDisplay` ("Đã xác thực" / "Trùng tên, chưa xác thực").
   `CanLinkAsResubmission` now requires `IsConfirmedSender`, so a name-only
   match can never be linked as a resubmission.
2. `Infrastructure/Data/AppDataService.cs` — `GetSenderRecords`:
   - The entry guard no longer requires a phone or address; a sender name alone
     triggers the comparison.
   - When the strict identity match (name + phone, or name + address with both
     phones empty) finds nothing, a name-only fallback query returns the same
     sender's records flagged `IsConfirmedSender = false`.
   - `SenderId` resolution (`ResolveSenderId`) is untouched — name-only matches
     never merge sender identities.
3. `ViewModels/RecordInputViewModel.cs` — `ResubmissionAvailability` explains
   the unconfirmed state: "Trùng tên người gửi nhưng chưa xác thực (thiếu hoặc
   khác số điện thoại/địa chỉ)...".
4. `Views/Records/SenderHistoryDialog.xaml` — new "Xác thực" column shows the
   per-row confirmation state.
5. Tests:
   - New `tests/QuanLyHoSo.UnitTests/SenderRecordIdentityTests.cs` (3 tests).
   - New `tests/QuanLyHoSo.IntegrationTests/SenderHistoryMatchingTests.cs`
     (5 tests): name-only draft surfaces an unconfirmed warning row; matching
     phone confirms and allows linking; different phone warns without
     confirming; different name returns nothing; server-side save of a
     resubmission against an unconfirmed row is rejected.
   - Updated two legacy tests that encoded the old "different phone = silent"
     behavior: `SameNameWithDifferentPhone_ShouldWarnWithoutConfirmingAndNewCaseShouldRemainOpen`
     (renamed from `...ShouldNotMatch...`) and
     `SenderWithoutPhone_ShouldConfirmByAddressAndWarnByNameOtherwise`
     (renamed from `...ShouldMatchNormalizedNameAndAddressOnly`).

## Behavior after the fix

- Entering a duplicate of `HS-2026-000019` (sender `Đặng Thị G`) without a
  phone now opens the comparison dialog listing the 13 same-name records
  (including 00019) as "Trùng tên, chưa xác thực" instead of silently creating
  a new record.
- Entering the same sender with the matching phone confirms the row and
  enables "Lưu là hồ sơ gửi lại" as before.
- "Lưu và xử lý như hồ sơ mới" remains available (existing product decision);
  the officer now sees the duplicates before choosing it.

## Verification

- `dotnet build QuanLyHoSo.Server` — compiles with zero C# errors (final copy
  to `bin\Debug` was blocked by the running server/client processes; a restart
  with the new build is required to pick the fix up).
- Unit tests: 69/69 passed (was 66; +3 new).
- Integration tests: 64/64 passed (was 59; +5 new, 2 updated).
- Production-data smoke (read-only query): the new fallback matches 13 records
  for the reported duplicate scenario, including `HS-2026-000019`.

## Deployment note

The running server and client instances still use the previous binaries. Close
the client windows and the server, rebuild (`dotnet build`), and restart both
to activate the fix.

## Deferred / follow-ups

- The restore feature still swaps the database file under a running server
  without re-running schema migrations (see the 2026-09-30 restore incident);
  fixing that needs its own plan.
- Hard-blocking "Lưu và xử lý như hồ sơ mới" when same-name same-case
  duplicates exist was considered and not implemented (product decision kept:
  warn, don't block).
