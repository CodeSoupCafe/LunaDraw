The build failed because a previous instance of the LunaDraw application is still running and has locked the output file. The error message confirms this:

`The file is locked by: "LunaDraw (28156)"`

This prevents the build process from overwriting the old `.exe` file with the new one.

To fix this, please find the running "LunaDraw.exe" process with the Process ID (PID) **28156** and terminate it. You can usually do this from the Task Manager (under the "Details" tab).

Once you have terminated the process, I will try the build again.
