# Lessons

- Never use `pkill -f <pattern>` / `pgrep -f` from the Bash tool: the pattern is part of the tool's own shell
  command line, so it kills the shell itself (exit 144). Track processes by PID file (`$!`) instead.
- Node output piped to `tail` under `timeout` is lost when the process is killed; write to a file and poll it.
- Resetting App_Data after test runs needs `git clean -fdx` (ignored files too): extension settings under
  App_Data/datastore/extensions are gitignored runtime files, and `git clean -fd` silently leaves them polluted.
- A text-presence check is not proof of success when the page ships hidden success AND error messages;
  assert visibility or verify the side effect (the stored data).
- Clean App_Data *before* `dotnet publish`: the publish copies App_Data, so leftovers from earlier test runs
  (e.g. a changed captcha answer) end up in the package and fail later tests for unrelated reasons.
- Scope per user (2026-09-29): only make the migrated app run and behave like the original; do not fix
  upstream logic bugs or add hardening. Classify review findings as "migration regression" vs "inherited".
- Reviewer subagents may overwrite shared /tmp helper scripts; give them their own file names/ports.
