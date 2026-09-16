namespace BionicCode.ProcessFacade.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

internal sealed class ProcessRunner
{
    public async Task<ProcessResult> RunAsync(ProcessRequest processRequest, CancellationToken cancellationToken)
    {
        ThrowIfProcessRequestIsInvalid(processRequest);

        bool isShellEnabled = processRequest.ShellMode is ShellMode.Enabled
                && processRequest.OutputMode is not ProcessOutputMode.None
                && processRequest.WindowMode is not WindowMode.Hidden
                && !processRequest.IsRequireingAuthentication;
        string username = processRequest.IsRequireingAuthentication 
            ? processRequest.Username 
            : string.Empty;
        SecureString password = processRequest.IsRequireingAuthentication 
            ? processRequest.Password 
            : null;
        var processStartInfo = new ProcessStartInfo
        {
            FileName = processRequest.FileName,
            Arguments = processRequest.Arguments,
            UserName = username,
            Password = password,
            Domain = processRequest.Domain,
            WindowStyle = processRequest.WindowMode is WindowMode.Hidden 
            ? ProcessWindowStyle.Hidden 
            : processRequest.WindowStyle,
            WorkingDirectory = processRequest.WorkingDirectory,
            RedirectStandardOutput = processRequest.OutputMode.HasFlag(ProcessOutputMode.StandardOutput),
            RedirectStandardError = processRequest.OutputMode.HasFlag(ProcessOutputMode.StandardError),
            UseShellExecute = isShellEnabled,
            CreateNoWindow = processRequest.WindowMode is WindowMode.Hidden,
        };

        if (processRequest.ExecutionMode is ExecutionMode.Elevated)
        {
            processStartInfo.Verb = "runas";
        }

        using var process = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true
        };

        _ = process.Start();

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        Task waitForExitTask = process.WaitForExitAsync(cancellationToken);
        await Task.WhenAll(stdoutTask, stderrTask, waitForExitTask).ConfigureAwait(false);
        
        return new ProcessResult(
            process.ExitCode, 
            await stdoutTask, 
            await stderrTask);
    }

    private void ThrowIfProcessRequestIsInvalid(ProcessRequest processRequest)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(processRequest.FileName, nameof(processRequest.FileName));
        
        if (processRequest.OutputMode is WindowMode.Hidden && processRequest.IsRequireingAuthentication)
        {
            throw new ArgumentException($"Invalid combination of '{nameof(processRequest.WindowMode)}.{nameof(WindowMode.Hidden)}' and authentication requirement. When username and password are provided, the window mode cannot be hidden.");
        }
    }
}

public enum WindowMode
{
    Hidden,
    Visible
}

public enum ShellMode
{
    Disabled,
    Enabled
}

[Flags]
public enum ProcessOutputMode
{
    None = 0,
    StandardOutput = 1,
    StandardError = 2,
    All = StandardOutput | StandardError
}

public enum ExecutionMode
{
    Normal,
    Elevated
}
