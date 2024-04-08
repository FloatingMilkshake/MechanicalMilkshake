namespace MechanicalMilkshake.Entities.EvalCommands;

public class ShellCommandResponse
{
    public ShellCommandResponse(int exitCode, string output, string error)
    {
        ExitCode = exitCode;
        Output = output;
        Error = error;
    }
    
    public ShellCommandResponse(int exitCode, string output)
    {
        ExitCode = exitCode;
        Output = output;
        Error = default;
    }

    public ShellCommandResponse()
    {
        ExitCode = default;
        Output = default;
        Error = default;
    }

    public int ExitCode { get; set; }
    public string Output { get; set; }
    public string Error { get; set; }
}