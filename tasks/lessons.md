# Lessons

- Never use `pkill -f <pattern>` / `pgrep -f` from the Bash tool: the pattern is part of the tool's own shell
  command line, so it kills the shell itself (exit 144). Track processes by PID file (`$!`) instead.
- Node output piped to `tail` under `timeout` is lost when the process is killed; write to a file and poll it.
