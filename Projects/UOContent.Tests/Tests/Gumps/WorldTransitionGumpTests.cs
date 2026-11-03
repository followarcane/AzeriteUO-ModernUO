using Server.Gumps;
using Server.Mobiles;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests.Gumps;

[Collection("Sequential UOContent Tests")]
public class WorldTransitionGumpTests
{
    [Fact]
    public void OnlySuccessfulTransitionsShowPanelAndLatestPanelClosesOnTime()
    {
        using var clock = new TimerWheelScope();
        using var ns = PacketTestUtilities.CreateTestNetState();
        var player = new PlayerMobile();
        try
        {
            player.MoveToWorld(new Point3D(1800, 1800, 0), Map.Felucca);
            ns.Account = new MockAccount();
            ns.Mobile = player;
            player.NetState = ns;
            Assert.False(player.TryChangeInstance(99));
            Assert.True(player.TryChangeInstance(0));
            Assert.False(player.HasGump<WorldTransitionGump>());
            Assert.True(player.TryChangeInstance(42));
            var first = player.FindGump<WorldTransitionGump>();
            Assert.NotNull(first);
            TimerWheelScope.RunFor(800);
            Assert.True(player.TryChangeInstance(0));
            var second = player.FindGump<WorldTransitionGump>();
            Assert.NotNull(second);
            Assert.NotSame(first, second);
            TimerWheelScope.RunFor(800);
            Assert.Same(second, player.FindGump<WorldTransitionGump>());
            TimerWheelScope.RunFor(800);
            Assert.False(player.HasGump<WorldTransitionGump>());
        }
        finally
        {
            ns.Mobile = null;
            player.Delete();
        }
    }
}
