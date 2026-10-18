// Optional Windows build helper. Leaves the installed editor executable untouched.
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

static class BeeLimited
{
    static string Quote(string value)
    {
        var result = new StringBuilder("\"");
        int slashes = 0;
        foreach (char c in value)
        {
            if (c == '\\') { slashes++; continue; }
            result.Append('\\', c == '"' ? slashes * 2 + 1 : slashes);
            result.Append(c); slashes = 0;
        }
        result.Append('\\', slashes * 2); result.Append('"');
        return result.ToString();
    }
    static int Main(string[] args)
    {
        string executable = Environment.GetEnvironmentVariable("SHANHAI_BEE_EXECUTABLE");
        string jobs = Environment.GetEnvironmentVariable("SHANHAI_BEE_JOBS") ?? "2";
        int count;
        if (string.IsNullOrEmpty(executable) || !int.TryParse(jobs, out count) || count < 1)
            return 64;
        var info = new ProcessStartInfo(executable, "-j " + count + " " + string.Join(" ", args.Select(Quote)))
        { UseShellExecute = false, CreateNoWindow = true };
        // Inherit standard handles: Bee's IPC and log streams pass through unchanged.
        using (var process = Process.Start(info)) { process.WaitForExit(); return process.ExitCode; }
    }
}
