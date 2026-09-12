# AI instructions

Read `.ai/ARCHITECTURE.md` and `.ai/CODING_GUIDELINES.md` before changing code. Use the historical feature routing in `AI/INDEX.md` when a task touches a specific screen or workflow.

The governing rule is: same UI, logic, workflow, data meaning, and output unless a defect is confirmed and documented. Prefer a small, buildable extraction over a broad rewrite. Never assume an unusual Vietnamese status, role, default, date range, or navigation branch is accidental.

Build to isolated output because a locally running client/server may lock normal `bin` files:

```powershell
dotnet build QuanLyHoSo.csproj --configuration Debug --no-restore -o .verify-builds/current/client
dotnet build QuanLyHoSo.Server/QuanLyHoSo.Server.csproj --configuration Debug --no-restore -o .verify-builds/current/server
```

Do not run those two commands concurrently: both projects update shared intermediate files.
