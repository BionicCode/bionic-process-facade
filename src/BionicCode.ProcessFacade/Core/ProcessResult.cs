namespace BionicCode.ProcessFacade.Core;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
/// <param name="ExitCode">A value indicating the exit code of the process.</param>
/// <param name="StandardOutput">The standard output of the process.</param>
/// <param name="StandardError">The standard error of the process.</param>
public readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError);