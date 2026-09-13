# Risk-based test matrix

| Feature | Risk | Test type | Automated cases | Status / priority |
|---|---|---|---|---|
| Database initialization | Very high | Integration + smoke | Fresh file, schema/seeds, reopen | Automated / P0 |
| Authentication | Very high | Unit + integration | Valid, wrong password, unknown/empty user, Unicode password | Automated / P0 |
| Authorization | Very high | Unit + integration | Role matrix, assigned-record access, data-layer create denial | Automated / P0 |
| Record persistence | Very high | Integration | Create/read/update/reopen, attachments, Unicode, failed-update rollback | Automated / P0 |
| Trash / restore | Very high | Integration | Soft delete, matching batch restore, stale undo, permanent delete | Automated / P0 |
| Backup / restore | Very high | Integration | Backup, modify, restore, safety backup, corrupt input, invalid destination | Automated / P0 |
| Password change | High | ViewModel unit | Required fields, min length, mismatch, reuse, wrong current, success, cancel | Automated / P1 |
| Search | High | Unit + integration | Exact, partial, case-insensitive, Unicode, no results | Automated / P1 |
| Pagination | High | Integration | Consecutive pages with page size 1 | Automated / P1 |
| Commands / notifications | Medium | Unit | Execute, parameter, CanExecute, event, repeated dispose | Automated / P1 |
| Area selector/filter | Medium | Unit | Null/blank, duplicate, grouped filtering, case | Automated / P1 |
| WPF startup/login surface | High | UI smoke | Launch, title, stable login controls, clean close | Automated; interactive / P1 |
| Processing transitions | Very high | Integration + UI | Full state path, forbidden/repeated transitions, restart mid-flow | Gap / P0 |
| Export | High | Integration | Columns/rows/Unicode/filter/empty/locked path | Gap / P1 |
| LAN API | High | Integration | Client/server auth, protocol errors, disconnect, concurrency | Gap / P1 |
| Full authenticated workflows | High | UI | Login, navigation, create/edit/search/status/logout | Gap; interactive / P1 |
| File failure handling | High | Integration | Locked/read-only/invalid/long paths | Partial (invalid backup path) / P1 |
| Concurrency | High | Integration | Double save/delete, simultaneous reads/writes, shutdown during save | Gap / P1 |
| Document generation | Medium | Integration | Correct templates/content/Unicode/failure paths | Gap / P2 |
