using System;
using Server.Spells;
using Xunit;

namespace Server.Tests.Spells;

[Collection("Sequential UOContent Tests")]
public class WorldInstanceSpellTests
{
    private sealed class SpellTarget : Mobile
    {
        public override bool CanRegenHits => false;
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ScheduledDamageBelongsToOriginalWorld(bool elemental, bool switchWorlds)
    {
        using var clock = new TimerWheelScope();
        var caster = new Mobile(World.NewMobile);
        var target = new SpellTarget();
        try
        {
            caster.DefaultMobileInit();

            var location = new Point3D(1700, 1700, 0);
            caster.MoveToWorld(location, Map.Felucca);
            target.MoveToWorld(location, Map.Felucca);
            target.Hits = target.HitsMax;
            var before = target.Hits;
            var delay = TimeSpan.FromMilliseconds(100);
            if (elemental)
            {
                SpellHelper.Damage(delay, target, caster, 10, 100, 0, 0, 0, 0);
            }
            else
            {
                SpellHelper.Damage(delay, target, caster, 10);
            }

            if (switchWorlds)
            {
                Assert.True(target.TryChangeInstance(42));
                Assert.True(caster.TryChangeInstance(42));
            }

            TimerWheelScope.RunFor(250);
            Assert.Equal(switchWorlds ? before : before - 10, target.Hits);
        }
        finally
        {
            caster.Delete();
            target.Delete();
        }
    }
}
