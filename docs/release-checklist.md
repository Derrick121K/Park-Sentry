# Release checklist

- [ ] `dotnet build` succeeds
- [ ] `dotnet test` succeeds (unit + integration)
- [ ] No secrets in git
- [ ] FluentAssertions 7.x pinned
- [ ] Production config uses Manual/real payments (not silent Mock)
- [ ] Scanning honesty (capture ≠ OCR)
- [ ] Docker images build
- [ ] Compose prod files present
- [ ] CI workflow present
- [ ] Backup/restore scripts documented
- [ ] Docs updated
- [ ] Smoke test (two orgs) performed or scheduled
- [ ] Tag `v1.0.0-rc1` (or later) only when criteria met
- [ ] Actual deploy only with credentials + verified smoke
