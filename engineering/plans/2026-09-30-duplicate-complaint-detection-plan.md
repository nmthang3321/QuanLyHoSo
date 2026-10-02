# Duplicate complaint detection hardening — 2026-09-30

> Superseded on 2026-10-02 by the proactive sender-name + area (+ optional entered phone) intake warning documented in `engineering/features/RECORD_RESUBMISSION.md`. This remains the historical plan for the earlier behavior.

Branch: `main` (direct fix on main, as agreed with user)

## Problem

When entering a duplicate complaint (same sender), the sender-history comparison
dialog did not appear and the record was silently saved as a new record.
Reproduced as admin duplicating `HS-2026-000019` (sender `Đặng Thị G`, phone
`0910 393 283`).

Root cause: `GetSenderRecords` only matches a sender by name **plus** phone
(or name + address when both phones are empty). Since phone/address became
optional at intake (2026-09-30), a draft without a phone can never match a
history record that has a phone, so the comparison dialog never opens and the
duplicate sails through.

## Design (agreed with user)

**Phone is a necessary condition for confirming sender identity — never a
sufficient one, and its absence must not silence the warning.**

- Confirmed match (linkable): normalized name + normalized phone equal, or the
  legacy case (both phones empty + address equal). Unchanged from today.
- Warning-only match (new): when the strict match finds nothing and the draft
  has a sender name, fall back to a name-only comparison. Rows found this way
  are flagged `IsConfirmedSender = false`.
- Unconfirmed rows can never be linked as resubmission
  (`CanLinkAsResubmission` requires `IsConfirmedSender`). Server-side
  `ValidateResubmission` keeps the strict identity rules, so a stale client
  cannot bypass this.
- The guard that skipped comparison entirely when the draft had neither phone
  nor address is relaxed: a sender name alone now triggers comparison.
- `SenderId` assignment (`ResolveSenderId`) stays strictly identity-based —
  name-only matches never merge sender identities.

## Changes

1. `Models/RecordModels.cs` — `SenderRecordHistory`: add
   `IsConfirmedSender` (default true, additive JSON) and `SenderMatchDisplay`;
   `CanLinkAsResubmission` now also requires `IsConfirmedSender`.
2. `Infrastructure/Data/AppDataService.cs` — `GetSenderRecords`: relax the
   entry guard; add the name-only fallback query when the strict query returns
   nothing; fallback rows are marked `IsConfirmedSender = false`.
3. `ViewModels/RecordInputViewModel.cs` — `ResubmissionAvailability` explains
   the unconfirmed state and how to confirm (enter the matching phone).
4. `Views/Records/SenderHistoryDialog.xaml` — add an "Xác thực" column showing
   the confirmed/unconfirmed state per row.
5. Tests — unit: link rules for confirmed/unconfirmed rows; integration:
   same-name-no-phone draft yields an unconfirmed warning row, same-name-same-
   phone yields a confirmed linkable row, different phone yields unconfirmed,
   different name yields nothing.

## Compatibility

- Additive JSON only; no schema change. Desktop clients and server can be
  upgraded independently (old clients keep working; server validation remains
  the backstop).
- LAN route contracts unchanged.

## Verification

- `dotnet build` solution, zero errors.
- Full non-UI test suite green (unit + integration), including new tests.
- Manual smoke: admin duplicates HS-2026-000019 with no phone → dialog appears
  with the unconfirmed warning; entering the matching phone → row confirmed
  and linkable.
- Report: `engineering/plans/2026-09-30-duplicate-complaint-detection-report.md`.
