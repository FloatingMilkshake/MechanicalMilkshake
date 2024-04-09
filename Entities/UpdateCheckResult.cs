namespace MechanicalMilkshake.Entities;

public class UpdateCheckResult
{
    public UpdateCheckResult(string hostname, int updateCount, bool restartRequired)
    {
        Hostname = hostname;
        UpdateCount = updateCount;
        RestartRequired = restartRequired;
    }
    public string Hostname { get; }
    public int UpdateCount { get; }
    public bool RestartRequired { get; }
}