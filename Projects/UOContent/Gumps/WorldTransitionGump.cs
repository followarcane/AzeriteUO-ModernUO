using System;

namespace Server.Gumps;

public sealed class WorldTransitionGump : StaticGump<WorldTransitionGump>
{
    private readonly Map _destination;

    private WorldTransitionGump(Map destination) : base(55, 65) => _destination = destination;

    public override bool Singleton => true;

    public static void DisplayTo(Mobile player, Map destination)
    {
        if (player.NetState == null)
        {
            return;
        }

        var panel = new WorldTransitionGump(destination);
        player.SendGump(panel);
        Timer.DelayCall(TimeSpan.FromSeconds(1.5), () =>
        {
            if (player.FindGump<WorldTransitionGump>() == panel)
            {
                player.CloseGump<WorldTransitionGump>();
            }
        });
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.SetNoMove();
        builder.AddBackground(0, 0, 440, 160, 5054);
        builder.AddImageTiled(16, 16, 408, 128, 2624);
        builder.AddHtml(24, 35, 392, 26, "AZERITE UO", "#CAA46A", 3, align: TextAlignment.Center);
        builder.AddHtmlPlaceholder(24, 77, 392, 42, "destination");
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder) =>
        builder.SetHtmlText("destination", $"{_destination.BaseFacet.Name} · Dünya {_destination.InstanceId}",
            "#F1E5CA", 4, align: TextAlignment.Center);
}
