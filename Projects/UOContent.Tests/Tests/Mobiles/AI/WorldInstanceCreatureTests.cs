using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

[Collection("Sequential UOContent Tests")]
public class WorldInstanceCreatureTests
{
    [Fact]
    public void DistantPetOnStayArrivesBesideOwnerOnBothTransfers()
    {
        var location = new Point3D(1800, 1800, 0);
        var (master, pet) = PetTestSetup.SpawnControlledPet(location, new Point3D(350, 400, 0));
        try
        {
            pet.ControlOrder = OrderType.Stay;
            Assert.True(master.TryChangeInstance(42));
            Assert.Equal(42, pet.InstanceId);
            Assert.Equal(location, pet.Location);
            Assert.True(master.TryChangeInstance(0));
            Assert.Same(master.Map, pet.Map);
            Assert.Equal(OrderType.Stay, pet.ControlOrder);
        }
        finally
        {
            pet.Delete();
            master.Delete();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DistantSummonKeepsItsOwnerAndExpiration(bool controlled)
    {
        using var clock = new TimerWheelScope();
        var master = new PlayerMobile();
        var summon = new PetTestStub();
        try
        {
            master.MoveToWorld(new Point3D(1800, 1800, 0), Map.Felucca);
            Assert.True(BaseCreature.Summon(summon, controlled, master, new Point3D(500, 500, 0), 0, TimeSpan.FromSeconds(5)));
            TimerWheelScope.RunFor(2000);
            Assert.True(master.TryChangeInstance(42));
            Assert.Same(master.Map, summon.Map);
            Assert.Equal(master.Location, summon.Location);
            Assert.Same(master, summon.SummonMaster);
            Assert.False(summon.Deleted);
            TimerWheelScope.RunFor(3100);
            Assert.True(summon.Deleted);
        }
        finally
        {
            summon.Delete();
            master.Delete();
        }
    }

    [Fact]
    public void StabledPetOnlyEntersWorldWhenClaimedFromLocalTrainer()
    {
        var location = new Point3D(1800, 1800, 0);
        var (master, pet) = PetTestSetup.SpawnControlledPet(location, location);
        var trainer = new AnimalTrainer();
        try
        {
            trainer.MoveToWorld(location, master.Map);
            master.AddItem(new Backpack());
            master.Backpack.DropItem(new Gold(30));
            trainer.EndStable(master, pet);
            Assert.True(pet.IsStabled);
            Assert.True(master.TryChangeInstance(42));
            Assert.Same(Map.Internal, pet.Map);
            trainer.Claim(master);
            Assert.True(pet.IsStabled);
            Assert.True(trainer.TryChangeInstance(42));
            trainer.Claim(master);
            Assert.False(pet.IsStabled);
            Assert.Same(master.Map, pet.Map);
            Assert.Equal(master.Location, pet.Location);
            Assert.Same(master, pet.ControlMaster);
        }
        finally
        {
            pet.Delete();
            trainer.Delete();
            master.Delete();
        }
    }

    [Fact]
    public void RiderDismountsWithEquipmentAndNestedInventoryInDestination()
    {
        var rider = new PlayerMobile();
        var horse = new Horse();
        var bag = new Bag();
        var coins = new Gold(3);
        var held = new Item(1);
        try
        {
            rider.MoveToWorld(new Point3D(1800, 1800, 0), Map.Felucca);
            horse.MoveToWorld(rider.Location, rider.Map);
            horse.Rider = rider;
            rider.AddItem(new Backpack());
            rider.Backpack.DropItem(bag);
            bag.DropItem(coins);
            held.Internalize();
            rider.Holding = held;
            Assert.True(rider.TryChangeInstance(42));
            Assert.Null(rider.Holding);
            Assert.Same(rider.Backpack, held.Parent);
            Assert.Equal(42, held.InstanceId);
            Assert.Same(bag, coins.Parent);
            Assert.Equal(42, coins.InstanceId);
            Assert.Same(horse, rider.Mount);
            Assert.Equal(42, rider.FindItemOnLayer(Layer.Mount).InstanceId);
            horse.Rider = null;
            Assert.Same(rider.Map, horse.Map);
            Assert.Equal(rider.Location, horse.Location);
        }
        finally
        {
            held.Delete();
            horse.Delete();
            rider.Delete();
        }
    }

    [Fact]
    public void PetsOnOtherFacetsAndAnotherPlayersPetsStayPut()
    {
        var location = new Point3D(1800, 1800, 0);
        var (master, pet) = PetTestSetup.SpawnControlledPet(location, location);
        var (other, otherPet) = PetTestSetup.SpawnControlledPet(location, location);
        try
        {
            pet.MoveToWorld(location, Map.Trammel);
            Assert.True(master.TryChangeInstance(42));
            Assert.Same(Map.Trammel, pet.Map);
            Assert.Same(Map.Felucca, otherPet.Map);
            Assert.Same(other, otherPet.ControlMaster);
        }
        finally
        {
            otherPet.Delete();
            other.Delete();
            pet.Delete();
            master.Delete();
        }
    }

    [Fact]
    public void FailedCursorStowLeavesOwnerItemAndFollowersTogether()
    {
        var location = new Point3D(1800, 1800, 0);
        var (master, pet) = PetTestSetup.SpawnControlledPet(location, location);
        var held = new Item(1);
        try
        {
            Assert.Null(master.Backpack);
            held.Internalize();
            master.Holding = held;
            Assert.False(master.TryChangeInstance(42));
            Assert.Same(held, master.Holding);
            Assert.Same(Map.Felucca, master.Map);
            Assert.Same(master.Map, pet.Map);
        }
        finally
        {
            master.Holding = null;
            held.Delete();
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
