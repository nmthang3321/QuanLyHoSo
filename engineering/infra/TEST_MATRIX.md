# Risk-based test matrix

| Feature | Risk | Test type | Automated cases | Status / priority |
|---|---|---|---|---|
| Database initialization | Very high | Integration + smoke | Fresh file, schema/seeds, empty production processor catalog, explicit sample officers, reopen | Automated / P0 |
| Authentication | Very high | Unit + integration | Valid, wrong password, unknown/empty user, Unicode password | Automated / P0 |
| Authorization | Very high | Unit + integration | Role matrix, assigned-record access, data-layer create denial | Automated / P0 |
| Resubmitted resolved records | Very high | Integration + ViewModel | Intake/reference/reopen/attachments, no extra resolution/work, invalid links, sender normalization/access/edit identity, copied legacy migration, authenticated LAN round trip, form reference refresh, popup original selection/reason/continuation/cancel, detail isolates independent intake groups | Automated; full dialog click-through remains manual / P0 |
| Record persistence | Very high | Unit + integration | Create/read/update/reopen, editable code format/normalization/duplicate rollback, attachments, Unicode | Automated / P0 |
| Trash / restore | Very high | Integration | Soft delete, matching batch restore, stale undo, permanent delete | Automated / P0 |
| Backup / restore | Very high | Integration | Legacy DB restore, full `.qlhbackup` database/files restore, safety backup, corrupt input, invalid destination, automatic 7-day interval and 10-file retention | Automated / P0 |
| Password change | High | ViewModel unit | Required fields, min length, mismatch, reuse, wrong current, success, cancel | Automated / P1 |
| Search | High | Unit + integration | Exact, partial, case-insensitive, Unicode, no results | Automated / P1 |
| Pagination | High | Integration | Consecutive pages with page size 1 | Automated / P1 |
| Commands / notifications | Medium | Unit | Execute, parameter, CanExecute, event, repeated dispose | Automated / P1 |
| Area selector/filter | Medium | Unit | Null/blank, duplicate, grouped filtering, case | Automated / P1 |
| WPF startup/login surface | High | UI smoke | Launch, title, stable login controls, clean close | Automated; interactive / P1 |
| Processing transitions | Very high | Integration + UI | Eight-step order and transfer compatibility covered; forbidden/repeated transitions and restart mid-flow remain | Partial / P0 |
| Export | High | Integration | Columns/rows/Unicode/filter/empty/locked path | Gap / P1 |
| LAN API | High | Integration | Resubmission/history authenticated round trip and unauthenticated rejection; disconnect/concurrency remain uncovered | Partial / P1 |
| Full authenticated workflows | High | UI | Login, navigation, create/edit/search/status/logout | Gap; interactive / P1 |
| File failure handling | High | Integration | Physical server attachment persistence and full restore covered; locked/read-only/invalid/long paths remain | Partial / P1 |
| Concurrency | High | Integration | Double save/delete, simultaneous reads/writes, shutdown during save | Gap / P1 |
| Document generation | Medium | Integration | All 3 templates: transfer number/dates, team/approving leaders, processor, multiline review/proposal, sender/content, highlights removed, repeat generation skipped; required fields rollback; LAN details serialization | Partial / P2; authenticated popup and file locks remain gaps |
