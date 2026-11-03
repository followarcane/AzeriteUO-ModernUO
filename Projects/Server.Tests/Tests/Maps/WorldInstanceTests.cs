using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests.Maps;

[Collection("Sequential Server Tests")]
public class WorldInstanceTests
{
    private static readonly Point3D Location = new(1400, 1400, 0);

    private static Mobile Spawn()
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.MoveToWorld(Location, Map.Felucca);
        mobile.Hits = mobile.HitsMax;
        return mobile;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void FacetsShareClientTerrainButUseDistinctSaveSlots(int facetIndex)
    {
        var facet = Map.Maps[facetIndex];
        var copy = facet.GetInstance(42);
        Assert.Same(facet, copy.GetInstance(0));
        Assert.Equal(32 + facetIndex, copy.MapIndex);
        Assert.Null(copy.GetInstance(99));
        PacketAssert.Sends(new MapChange(facet), ns => ns.SendMapChange(copy));
        Assert.NotEqual(new WorldLocation(Location, facet), new WorldLocation(Location, copy));
    }

    [Fact]
    public void SameCoordinatesIsolateCombatVisibilityAndGroundItems()
    {
        var source = Spawn();
        var target = Spawn();
        var ground = new Item(1);
        var carried = new Item(2);
        try
        {
            ground.MoveToWorld(Location, source.Map);
            target.AddItem(carried);
            Assert.True(target.TryChangeInstance(42));
            Assert.Equal(Location, target.Location);
            Assert.Equal(42, carried.InstanceId);
            Assert.False(source.CanSee(target));
            Assert.False(target.CanSee(source));
            Assert.False(target.CanSee(ground));
            Assert.False(ground.IsAccessibleTo(target));
            Assert.False(source.CanBeHarmful(target, false));
            Assert.False(target.CanBeBeneficial(source, false));
            Assert.False(target.Map.GetItemsInRange<Item>(Location, 0).GetEnumerator().MoveNext());
            var nearby = target.Map.GetMobilesInRange<Mobile>(Location, 0).GetEnumerator();
            Assert.True(nearby.MoveNext());
            Assert.Same(target, nearby.Current);
            Assert.False(nearby.MoveNext());
            target.Hits -= 5;
            var before = target.Hits;
            target.Damage(4, source);
            target.Heal(3, source);
            Assert.Equal(0, AOS.Damage(target, source, 4, 100, 0, 0, 0, 0));
            Assert.Equal(before, target.Hits);
            Assert.False(target.TryChangeInstance(99));
            target.MoveToWorld(Location, Map.Felucca);
            target.Map = Map.Felucca;
            Assert.Equal(42, target.InstanceId);
            Assert.True(target.TryChangeInstance(0));
            Assert.True(source.CanSee(target));
            Assert.True(target.CanSee(ground));
            Assert.Equal(0, carried.InstanceId);
            target.Damage(4, source);
            Assert.Equal(before - 4, target.Hits);
        }
        finally
        {
            carried.Delete();
            ground.Delete();
            source.Delete();
            target.Delete();
        }
    }

    [Fact]
    public void ClientsReceiveOnlyTheirOwnGroundItemsAndRemoveOldWorldOnTransfer()
    {
        var (nsA, a) = TestClientHarness.CreateClient(Location);
        var (nsB, b) = TestClientHarness.CreateClient(Location);
        var itemA = new Item(1);
        var itemB = new Item(2);
        try
        {
            itemA.MoveToWorld(Location, a.Map);
            Assert.True(TestClientHarness.ReceivedWorldItem(nsB, itemA));
            Assert.True(b.TryChangeInstance(42));
            Assert.True(TestClientHarness.ReceivedRemove(nsB, itemA.Serial));
            Assert.True(TestClientHarness.ReceivedRemove(nsA, b.Serial));
            itemB.MoveToWorld(Location, b.Map);
            Assert.True(TestClientHarness.ReceivedWorldItem(nsB, itemB));
            Assert.False(TestClientHarness.ReceivedWorldItem(nsA, itemB));
            Assert.True(b.TryChangeInstance(0));
            Assert.True(TestClientHarness.ReceivedRemove(nsB, itemB.Serial));
        }
        finally
        {
            itemB.Delete();
            itemA.Delete();
            TestClientHarness.DisposeClient(nsB, b);
            TestClientHarness.DisposeClient(nsA, a);
        }
    }

    [Fact]
    public void SavedPlayerLogsBackIntoTheirInstance()
    {
        var source = Spawn();
        var restored = new Mobile(World.NewMobile);
        var ns = PacketTestUtilities.CreateTestNetState();
        try
        {
            source.Player = true;
            Assert.True(source.TryChangeInstance(42));
            var writer = new BufferWriter(new byte[4096], true);
            source.Serialize(writer);
            restored.Deserialize(new BufferReader(writer.Buffer));
            Assert.Same(Map.Internal, restored.Map);
            Assert.Equal(42, restored.InstanceId);
            Assert.Same(source.Map, restored.LogoutMap);
            ns.Account = new MockAccount();
            ns.Mobile = restored;
            restored.NetState = ns;
            Assert.Same(source.Map, restored.Map);
            Assert.Equal(Location, restored.Location);
        }
        finally
        {
            TestClientHarness.DisposeClient(ns, restored);
            source.Delete();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    public void GroundItemRetainsInstanceThroughSerialization(int id)
    {
        var map = Map.Trammel.GetInstance(id);
        var ground = new Item(1);
        var restored = new Item(World.NewItem);
        try
        {
            ground.MoveToWorld(Location, map);
            var buffer = new BufferWriter(new byte[256], true);
            ground.Serialize(buffer);
            restored.Deserialize(new BufferReader(buffer.Buffer));
            Assert.Same(map, restored.Map);
            Assert.Equal(Location, restored.Location);
        }
        finally
        {
            ground.Delete();
            restored.Delete();
        }
    }
}
