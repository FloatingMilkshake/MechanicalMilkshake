namespace MechanicalMilkshake.Commands;

public class Err : ApplicationCommandModule
{
    [SlashCommand("err", "Look up a Microsoft error code with the Microsoft Error Lookup Tool.")]
    public static async Task ErrCmd(InteractionContext ctx, [Option("code", "The error code to look up, in any format.")] string code)
    {
        // I know this is cursed, I don't really care.
        // I run the bot on Linux and don't want to use Wine to run the tool.
        // So SSH it is (+ wake on lan in case the Windows machine is sleeping).
        // I'm sorry lol
        
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
        if (Program.DisabledCommands.Contains("err"))
        {
            if (Program.DisabledCommands.Contains("cdn"))
            {
                await CommandHandlerHelpers.FailOnMissingInfo(ctx, true);
                return;
            }
        }
        
        // Shoot a wake on LAN packet to the Windows machine to ensure it is awake
        
        // Parse MAC address to byte array
        byte[] mac = Program.ConfigJson.WakeOnLan.MacAddress.Split(':')
            .Select(b => Convert.ToByte(b, 16))
            .ToArray();

        // Create the magic packet
        byte[] packet = new byte[102];
        for (int i = 0; i < 6; i++) packet[i] = 0xFF;
        for (int i = 6; i < 102; i++) packet[i] = mac[i % mac.Length];

        // Send the magic packet
        using var client = new UdpClient();
        client.Connect(Program.ConfigJson.WakeOnLan.IpAddress, Program.ConfigJson.WakeOnLan.Port);
        await client.SendAsync(packet, packet.Length);
        
        // Sanitize input
        code = code.Replace("\"", "").Replace(";", "").Replace("&", "").Replace("|", "").Replace("&&", "").Replace("||", "");
        
        // SSH into the Windows machine and run the tool
        var cmd = $"ssh -o ConnectTimeout=30 {Program.ConfigJson.Err.SshUsername}@{Program.ConfigJson.Err.SshHost} \"$PSStyle.OutputRendering = \"PlainText\"; C:\\err.exe {code} 2>&1 | Out-String\"";
        var result = await EvalCommands.RunCommand(cmd);
        
        var response = result.ExitCode switch
        {
            0 or 1 => $"```\n{result.Output}\n```",
            _ => "Something went wrong! Please try again."
        };
        
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response));
    }
}