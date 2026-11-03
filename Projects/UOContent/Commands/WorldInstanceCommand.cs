namespace Server.Commands;

public static class WorldInstanceCommand
{
    // Register before World.Load. Slots 32-37 permanently identify world 42 in saves.
    [CallPriority(3)]
    public static void Configure()
    {
        Map.RegisterWorldInstance(42, 32);
        CommandSystem.Register("Instance", AccessLevel.GameMaster, OnCommand);
    }

    [Usage("Instance [id]")]
    [Description("Shows your world instance, or moves you to the same coordinates in another instance.")]
    private static void OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        if (e.Arguments.Length == 0)
        {
            from.SendMessage($"World instance {from.InstanceId} ({from.Map}).");
            return;
        }

        if (e.Arguments.Length != 1 || !int.TryParse(e.Arguments[0], out var id) || !from.TryChangeInstance(id))
        {
            from.SendMessage("Choose a registered instance on this facet. Usage: [Instance <id>");
            return;
        }

        from.SendMessage($"Entered world instance {from.InstanceId} ({from.Map}).");
    }
}
