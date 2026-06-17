using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

public static class GitHelper
{
    public static string RunGit(string arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };

        using (Process process = Process.Start(startInfo))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return $"Command: git {arguments}\nExit Code: {process.ExitCode}\nOutput:\n{output}\nError:\n{error}\n";
        }
    }
}
