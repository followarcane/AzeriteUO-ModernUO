using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

[Collection("Sequential UOContent Tests")]
public class WorldInstanceCreatureTests
{
    [Fact]
    public void FollowingPetTransfersButStationaryPetStaysInItsWorld()
    {
        var location = new Point3D(1800, 1800, 0);
        var (master, pet) = PetTestSetup.SpawnControlledPet(location, location);
        try
        {
            Assert.True(master.TryChangeInstance(42));
            Assert.Equal(42, pet.InstanceId);
            pet.ControlOrder = OrderType.Stay;
            Assert.True(master.TryChangeInstance(0));
            Assert.Equal(42, pet.InstanceId);
            Assert.False(master.CanSee(pet));
        }
        finally
        {
            pet.Delete();
            master.Delete();
        }
    }

    [Fact]
    public void CreatureAuraHitsOnlyPlayersInItsWorld()
    {
        var location = new Point3D(1850, 1850, 0);
        var creature = new PetTestStub();
        var local = new PlayerMobile { Player = true };
        var remote = new PlayerMobile { Player = true };
        try
        {
            creature.MoveToWorld(location, Map.Felucca);
            local.MoveToWorld(location, Map.Felucca);
            remote.MoveToWorld(location, Map.Felucca);
            Assert.True(remote.TryChangeInstance(42));
            local.Hits = local.HitsMax;
            remote.Hits = remote.HitsMax;
            creature.AuraDamage();
            Assert.True(local.Hits < local.HitsMax);
            Assert.Equal(remote.HitsMax, remote.Hits);
        }
        finally
        {
            remote.Delete();
            local.Delete();
            creature.Delete();
        }
    }
}
