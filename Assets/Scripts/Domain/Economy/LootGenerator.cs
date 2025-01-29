using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Constructor;
using Economy;
using Economy.ItemType;
using Economy.Products;
using GameServices.Random;
using Constructor.Ships;
using Game;
using Game.Exploration;
using GameDatabase;
using GameDatabase.DataModel;
using GameDatabase.Enums;
using GameDatabase.Extensions;
using GameDatabase.Model;
using GameDatabase.Query;
using GameModel;
using GameServices.Player;
using Zenject;
using Component = GameDatabase.DataModel.Component;
using Constructor.Extensions;

namespace GameServices.Economy
{
    public class LootGenerator
    {
        [Inject] private readonly ItemTypeFactory _factory;
        [Inject] private readonly Research.Research _research;
        [Inject] private readonly IRandom _random;
        [Inject] private readonly IDatabase _database;
        [Inject] private readonly HolidayManager _holidayManager;
        [Inject] private readonly PlayerSkills _playerSkills;

        public ItemTypeFactory Factory { get { return _factory; } }

        public IEnumerable<IProduct> GetCommonReward(IEnumerable<IShip> ships, int distance, Faction faction, int seed)
        {
            var random = _random.CreateRandom(seed);

            var scraps = 0;
            var money = 0;

            var moduleLevel = Maths.Distance.ComponentLevel(distance);

            foreach (var ship in ships)
            {
                scraps += ship.Scraps();
                money += ship.Price()/20;

                if (ship.Model.ShipType == ShipType.Flagship)
                {
                    var bossFaction = ship.Model.Faction;
                    var threatLevel = (int)ship.ExtraThreatLevel;
                    yield return CommonProduct.Create(_factory.CreateResearchItem(bossFaction), random.Next(3 + threatLevel * 1, 6 + threatLevel * 2));

                    foreach (var item in RandomComponents(moduleLevel + 35, random.Next(1, 2), bossFaction, random, false))
                        yield return CommonProduct.Create(item);

                    if (ship.ExtraThreatLevel >= DifficultyClass.Class2)
                    {
                        yield return Price.Premium(8).GetProduct(_factory);
                        foreach (var item in RandomComponents(moduleLevel + 75, random.Next(1, 3), bossFaction, random, false))
                            yield return CommonProduct.Create(item);
                    } else
                    {
                        yield return Price.Premium(16).GetProduct(_factory);
                    }
                }
                else
                {
                    if (random.Percentage(20))
                    {
                        yield return CommonProduct.Create(_factory.CreateResearchItem(faction));
                    }
                    foreach (var item in RandomComponents(moduleLevel, random.Next(-10, 2), faction, random, false))
                        yield return CommonProduct.Create(item);
                }
            }

            if (money > 0)
                yield return Price.Common(scaleFromSkill(money)).GetProduct(_factory);

            var toxicWaste = random.Next2(scraps/2);
            if (toxicWaste > 0)
                yield return CommonProduct.Create(CreateArtifact(CommodityType.ToxicWaste), scaleFromSkill(toxicWaste));

            scraps -= toxicWaste;
            if (scraps > 0)
                yield return CommonProduct.Create(CreateArtifact(CommodityType.Scraps), scaleFromSkill(scraps));

            foreach (var item in GetHolidayLoot(random))
                yield return item;
        }

        public IEnumerable<IProduct> GetSocialShareReward()
        {
            yield return Price.Premium(100).GetProduct(_factory);
        }

        public IEnumerable<IProduct> GetAdReward()
        {
            yield return Price.Premium(100).GetProduct(_factory);
        }

        public IEnumerable<IProduct> GetHolidayLoot(System.Random random)
        {
            if (_holidayManager.IsChristmas)
            {
                yield return CommonProduct.Create(_factory.CreateCurrencyItem(Currency.Snowflakes), scaleFromSkill(11 + random.Next(22)));
            }
        }

        public IEnumerable<IProduct> GetMeteoriteLoot(Faction faction, int level, int seed)
        {
            var random = new System.Random(seed);
            var quality = Mathf.RoundToInt(_playerSkills.PlanetaryScanner*100);

            yield return CommonProduct.Create(CreateArtifact(CommodityType.Minerals), 1 + random.Next2(20*quality/100));
            if (random.Percentage(5))
                yield return CommonProduct.Create(CreateArtifact(CommodityType.Gems), 1 + random.Next2(5 * quality / 100));
            if (random.Percentage(5))
                yield return CommonProduct.Create(CreateArtifact(CommodityType.PreciousMetals), 1 + random.Next2(5 * quality / 100));
        }

        private int scaleFromSkill(int n) {
            return Mathf.RoundToInt(n * _playerSkills.PlanetaryScanner);
        }

        public IEnumerable<IProduct> GetOutpostLoot(Faction faction, int level, int seed)
        {
            var random = new System.Random(seed);
            var quality = Mathf.RoundToInt(_playerSkills.PlanetaryScanner * 100);
            var researchItemNeed = true;

            yield return CommonProduct.Create(CreateArtifact(CommodityType.Scraps), 10 + random.Next2(120 * quality / 100));

            if (random.Percentage(quality/2))
            {
                var tech = _research.GetAvailableTechs(faction).Where(item => item.Hidden || item.Price <= 35).RandomElement(random);
                if (tech != null)
                    yield return CommonProduct.Create(_factory.CreateBlueprintItem(tech));
                else
                {
                    researchItemNeed = false;
                    yield return CommonProduct.Create(_factory.CreateResearchItem(faction), scaleFromSkill(5 + random.Next(30)));
                }
            }

            for (var i = 0; i < random.Next(quality / 40); ++i)
                if (TryCreateRandomComponent(level, faction, random, true, ComponentQuality.P3, out var itemType))
                    yield return CommonProduct.Create(itemType);

            if (researchItemNeed)
                yield return CommonProduct.Create(_factory.CreateResearchItem(faction), scaleFromSkill(1 + random.Next(5)));

            yield return Price.Premium(10 + random.Next(2 + quality * 100 + level) / 100).GetProduct(_factory);
        }

        public IEnumerable<IProduct> GetHiveLoot(int level, int seed)
        {
            var random = new System.Random(seed);
            var quality = Mathf.RoundToInt(_playerSkills.PlanetaryScanner * 100);

            yield return CommonProduct.Create(CreateArtifact(CommodityType.Artifacts), 1 + random.Next2(5 * quality / 100));

            for (int i = 0; i < 3; ++i)
               if (random.Percentage(20 + quality / 10))
                    if (TryCreateRandomComponent(level, _database.ExplorationSettings.InfectedPlanetFaction, random, true, ComponentQuality.P3, out var itemType))
                        yield return CommonProduct.Create(itemType);

            if (random.Percentage(quality/5))
                yield return CommonProduct.Create(RandomFactionShip(level, _database.ExplorationSettings.InfectedPlanetFaction, random));

            if (random.Percentage(quality/2))
            {
                var tech = _research.GetAvailableTechs((_database.ExplorationSettings.InfectedPlanetFaction)).Where(item => item.Hidden || item.Price <= 35).RandomElement(random);
                if (tech != null)
                    yield return CommonProduct.Create(_factory.CreateBlueprintItem(tech));
            }

            yield return Price.Premium(10 + random.Next(2 + quality + level) / 100).GetProduct(_factory);
        }

        public IEnumerable<IProduct> GetPlanetResources(PlanetType planetType, Faction faction, int level, int seed)
        {
            var random = new System.Random(seed);
            var quality = Mathf.RoundToInt(_playerSkills.PlanetaryScanner * 100);

            if (planetType == PlanetType.Gas)
            {
                yield return CommonProduct.Create(CreateArtifact(CommodityType.ToxicWaste), 40 + random.Next2(200 * quality / 100));
                if (random.Percentage(30))
                    yield return CommonProduct.Create(_factory.CreateFuelItem(), 20 + random.Next2(40 * quality / 100));
            }
            else
            {
                yield return CommonProduct.Create(CreateArtifact(CommodityType.Minerals), 40 + random.Next2(200 * quality / 100));
                if (random.Percentage(scaleFromSkill(5)))
                    yield return CommonProduct.Create(CreateArtifact(CommodityType.Gems), 3 + random.Next2(12 * quality / 100));
                if (random.Percentage(scaleFromSkill(5)))
                    yield return CommonProduct.Create(CreateArtifact(CommodityType.PreciousMetals), 20 + random.Next2(20 * quality / 100));
            }
        }

        public IEnumerable<IProduct> GetPlanetRareResources(PlanetType planetType, Faction faction, int level, int seed)
        {
            return GetPlanetResources(planetType, faction, level, seed);
        }

        public IEnumerable<IProduct> GetContainerLoot(Faction faction, int level, int seed)
        {
            var random = new System.Random(seed);
            var ratio = _playerSkills.PlanetaryScanner;
            var quality = Mathf.RoundToInt(_playerSkills.PlanetaryScanner * 100);
            var additionP3Modification = quality >= 1.0 ? 1 : 0;

            yield return CommonProduct.Create(_factory.CreateCurrencyItem(Currency.Credits), scaleFromSkill(random.Next(1500 + level * 150, 10000 + level * 1000)));
            yield return Price.Premium(scaleFromSkill(random.Next(4, 20 + level / 5))).GetProduct(_factory);
            yield return CommonProduct.Create(_factory.CreateResearchItem(faction), scaleFromSkill(random.Next(2, 5 + level / 35)));

            if (random.Percentage(scaleFromSkill(30)))
                yield return CommonProduct.Create(CreateArtifact(CommodityType.Alloys), 20 + random.Next2(100 * quality / 100));
            if (random.Percentage(scaleFromSkill(30)))
                yield return CommonProduct.Create(CreateArtifact(CommodityType.Polymers), 20 + random.Next2(100 * quality / 100));
            if (random.Percentage(scaleFromSkill(10)))
                yield return CommonProduct.Create(CreateArtifact(CommodityType.Artifacts), 20 + random.Next2(60 * quality / 100));

            for (var i = 0; i < random.Next(2, quality / 50); ++i)
                if (TryCreateRandomComponent(level, faction, random, true, ComponentQuality.P3, out var itemType))
                    yield return CommonProduct.Create(itemType);

            for (var i = 0; i < random.Next(1, quality / 75) + additionP3Modification; ++i)
                if (TryGetRandomComponent(seed + 77394 + i, true, null, ModificationQuality.P3, out var component))
                    yield return component;
        }

        public IEnumerable<IProduct> GetShipWreckLoot(Faction faction, int level, int seed)
        {
            var random = new System.Random(seed);
            var quality = Mathf.RoundToInt(_playerSkills.PlanetaryScanner * 100);

            yield return CommonProduct.Create(CreateArtifact(CommodityType.Scraps), 1 + random.Next2(50*quality/100));

            if (random.Percentage(scaleFromSkill(30)))
                yield return CommonProduct.Create(CreateArtifact(CommodityType.Alloys), 10 + random.Next2(80 * quality / 100));
            if (random.Percentage(scaleFromSkill(30)))
                yield return CommonProduct.Create(CreateArtifact(CommodityType.Polymers), 10 + random.Next2(80 * quality / 100));
            if (random.Percentage(scaleFromSkill(20)))
                yield return CommonProduct.Create(_factory.CreateFuelItem(), 10 + random.Next2(60 * quality / 100));

            yield return CommonProduct.Create(_factory.CreateResearchItem(faction), scaleFromSkill(random.Next(2, 10 + level / 15)));

            for (var i = 0; i < random.Next(1, quality / 50); ++i)
                if (TryCreateRandomComponent(level, faction, random, true, ComponentQuality.P3, out var itemType))
                    yield return CommonProduct.Create(itemType);
        }

        public IEnumerable<IProduct> GetStarBaseSpecialReward(Region region)
        {
            yield return CommonProduct.Create(_factory.CreateResearchItem(region.Faction), Mathf.FloorToInt(3f + region.BaseDefensePower / 100f) + region.HomeStarLevel / 60);

            if (region.IsPirateBase)
            {
                var random = _random.CreateRandom(region.Id);

                yield return Price.Premium(Mathf.Min(200, 5 + region.HomeStarLevel / 30)).GetProduct(_factory);
                foreach (var faction in _database.FactionsWithEmpty.ValidForMerchants().RandomUniqueElements(4, random))
                    yield return CommonProduct.Create(_factory.CreateResearchItem(faction), Mathf.Min(80, 1 + region.HomeStarLevel / 60));

                if (random.Percentage(30))
                {
                    var tech = _research.GetAvailableTechs(region.Faction).Where(item => item.Hidden || item.Price <= 50).RandomElement(random);
                    if (tech != null)
                        yield return CommonProduct.Create(_factory.CreateBlueprintItem(tech));
                }
            }
        }

        //public IEnumerable<IProduct> GetCommonPlanetReward(Faction faction, int level, System.Random random, float successChances)
        //{
        //    if (random.NextFloat() < successChances * successChances && random.Percentage(7))
        //        yield return Price.Premium(1).GetProduct(_factory);

        //    if (random.NextFloat() < successChances * successChances && random.Percentage(2))
        //    {
        //        var tech = _research.GetAvailableTechs(faction).Where(item => item.Hidden || item.Price <= 10).RandomElement(random);
        //        if (tech != null)
        //            yield return new Product(_factory.CreateBlueprintItem(tech));
        //    }

        //    if (System.DateTime.UtcNow.IsEaster())
        //        if (random.NextFloat() < successChances * successChances && random.Percentage(2))
        //            yield return new Product(_factory.CreateShipItem(new CommonShip(_database.GetShipBuild(LegacyShipBuildNames.GetId("fns3_mk2"))).Unlocked()));
        //}

        public IEnumerable<IProduct> GetSpaceWormLoot(int level, int seed)
        {
            var random = _random.CreateRandom(seed);
            yield return CommonProduct.Create(CreateArtifact(CommodityType.Artifacts), 10 + random.Next2(level));
            yield return Price.Premium(25 + random.Next2(level / 20)).GetProduct(_factory);

            if (random.Percentage(30))
            {
                var tech = _research.GetAvailableTechs().Where(item => item.Price <= 50).RandomElement(random);
                if (tech != null)
                    yield return CommonProduct.Create(_factory.CreateBlueprintItem(tech));
            }
        }

        public IEnumerable<IProduct> GetRuinsRewards(int level, int seed)
        {
            var random = _random.CreateRandom(seed);

            yield return Price.Common(500 * Maths.Distance.Credits(level)).GetProduct(_factory);
            yield return CommonProduct.Create(_factory.CreateFuelItem(), random.Next(5,15));

            if (random.Next(3) == 0)
            {
                var itemLevel = Mathf.Max(6, level / 2);
                var companions = _database.SatelliteList.Where(item => item.Layout.CellCount <= itemLevel && item.SizeClass != SizeClass.Titan);
                foreach (var item in companions.Where(item => item.SizeClass != SizeClass.Titan).RandomUniqueElements(1, random))
                    yield return CommonProduct.Create(_factory.CreateSatelliteItem(item));
            }

            foreach (var item in RandomComponents(Maths.Distance.ComponentLevel(level) + 35, random.Next(1, 3), null, random, false))
                yield return CommonProduct.Create(item);

            var quantity = 25 + random.Next(30) + level / 20;
            if (quantity > 0)
                yield return Price.Premium(quantity).GetProduct(_factory);

            yield return CommonProduct.Create(_factory.CreateResearchItem(_database.GalaxySettings.AbandonedStarbaseFaction));
        }

        public IEnumerable<IProduct> GetXmasRewards(int level, int seed)
        {
            var random = _random.CreateRandom(seed);

            yield return new Price(random.Range(level/5 + 15, level/5 + 30), Currency.Snowflakes).GetProduct(_factory);

            var items = _database.ComponentList.CommonAndRare().LevelLessOrEqual(level + 50)
                .RandomElements(random.Range(5, 10), random).Select(item =>
                    ComponentInfo.CreateRandomModification(item, random, ModificationQuality.P2));

            if (random.Percentage(10))
                yield return CommonProduct.Create(_factory.CreateComponentItem(new ComponentInfo(_database.GetComponent(new ItemId<Component>(96))))); // xmas bomb
            if (random.Percentage(5) && level > 50)
                yield return CommonProduct.Create(_factory.CreateComponentItem(new ComponentInfo(_database.GetComponent(new ItemId<Component>(215))))); // drone bay
            if (random.Percentage(5) && level > 50)
                yield return CommonProduct.Create(_factory.CreateComponentItem(new ComponentInfo(_database.GetComponent(new ItemId<Component>(220))))); // drone bay
            if (random.Percentage(5) && level > 50)
                yield return CommonProduct.Create(_factory.CreateComponentItem(new ComponentInfo(_database.GetComponent(new ItemId<Component>(219))))); // drone bay
            if (random.Percentage(5) && level > 100)
                yield return CommonProduct.Create(_factory.CreateComponentItem(new ComponentInfo(_database.GetComponent(new ItemId<Component>(213))))); // holy cannon

            foreach (var item in items)
                yield return CommonProduct.Create(_factory.CreateComponentItem(item));
        }

        public IEnumerable<IProduct> GetDailyReward(int day, int level, int seed)
        {
            if (day <= 0)
                yield break;

            yield return new Price(Mathf.Min(day*100, 1000), Currency.Credits).GetProduct(_factory);

            if (day % 2 == 0)
                yield return CommonProduct.Create(_factory.CreateFuelItem(), Mathf.Min(30, 10*day/2));
            else if (day % 3 == 0)
                yield return CommonProduct.Create(_factory.CreateResearchItem(_database.FactionsWithEmpty.CanGiveTechPoints(level).RandomElement(new System.Random(seed))), Mathf.Min(5,day/3));
            else if (day % 5 == 0)
                yield return Price.Premium(Mathf.Min(5,day/5)).GetProduct(_factory);

            if (day > 3)
            {
                var quality = (ComponentQuality)Mathf.Min(day/3, (int)ComponentQuality.P3);
                if (ComponentInfo.TryCreateRandomComponent(_database, level, null, _random.CreateRandom(seed), false, quality, out var componentInfo))
                    yield return CommonProduct.Create(_factory.CreateComponentItem(componentInfo));
            }
        }

        public bool TryGetRandomComponent(int seed, bool allowRare, Faction faction, ModificationQuality modificationQuality, out IProduct product)
        {
            var random = _random.CreateRandom(seed);
            if (!ComponentInfo.TryCreateRandomComponent(_database, faction, random, allowRare, modificationQuality, out var componentInfo))
            {
                product = null;
                return false;
            }

            product = CommonProduct.Create(_factory.CreateComponentItem(componentInfo));
            return true;
        }

        public bool TryGetRandomComponent(int distance, int seed, bool allowRare, out IProduct product)
        {
            var random = _random.CreateRandom(seed);
            if (TryCreateRandomComponent(distance, null, random, allowRare, ComponentQuality.P3, out var item))
            {
                product = CommonProduct.Create(item);
                return true;
            }

            product = null;
            return false;
        }

        public IEnumerable<IItemType> GetRandomComponents(int distance, int count, Faction faction, int seed, bool allowRare, ComponentQuality maxQuality = ComponentQuality.P3)
        {
            var random = _random.CreateRandom(seed);
            return RandomComponents(distance, count, faction, random, allowRare, maxQuality);
        }

        public IEnumerable<IItemType> GetRandomComponents(int distance, int count, int seed, bool allowRare, ComponentQuality maxQuality = ComponentQuality.P3)
        {
            var random = _random.CreateRandom(seed);
            return RandomComponents(distance, count, null, random, allowRare, maxQuality);
        }

        public IItemType GetRandomFactionShip(int distance, Faction faction, int seed)
        {
            var random = _random.CreateRandom(seed);
            return RandomFactionShip(distance, faction, random);
        }

        public DamagedShipItem GetRandomDamagedShip(int distance, int seed)
        {
            var random = _random.CreateRandom(seed);

            var value = random.Next(distance);
            var ships = value > 20 ? ShipBuildQuery.PlayerShips(_database).CommonAndRare() : ShipBuildQuery.PlayerShips(_database).Common();
            var ship = ships.FilterByStarDistance(distance/2, ShipBuildQuery.FilterMode.SizeAndFaction).Random(random);

            return (DamagedShipItem)_factory.CreateDamagedShipItem(ship, random.Next());
        }

        private IItemType RandomFactionShip(int distance, Faction faction, System.Random random)
        {
            var ship = ShipBuildQuery.PlayerShips(_database).Common().BelongToFaction(faction).
				FilterByStarDistance(distance, ShipBuildQuery.FilterMode.Size).Random(random);
            return ship != null ? _factory.CreateMarketShipItem(new CommonShip(ship, _database)) : null;
        }

        private IEnumerable<IItemType> RandomComponents(int distance, int count, Faction faction, System.Random random, bool allowRare, ComponentQuality maxQuality = ComponentQuality.P3)
        {
            for (var i = 0; i < count; ++i)
                if (ComponentInfo.TryCreateRandomComponent(_database, distance, faction, random, allowRare, maxQuality, out var componentInfo))
                    yield return _factory.CreateComponentItem(componentInfo);
        }

        private bool TryCreateRandomComponent(int distance, Faction faction, System.Random random, bool allowRare, ComponentQuality maxQuality, out IItemType itemType)
        {
            if (!ComponentInfo.TryCreateRandomComponent(_database, distance, faction, random, allowRare, maxQuality, out var componentInfo))
            {
                itemType = null;
                return false;
            }

            itemType = _factory.CreateComponentItem(componentInfo);
            return true;
        }

        private IItemType CreateArtifact(CommodityType commodityType)
        {
            var artifact = _database.GetQuestItem(new ItemId<QuestItem>((int)commodityType));
            return _factory.CreateArtifactItem(artifact);
        }
    }

    public static class ProductListExtensions
    {
        public static Domain.Quests.ILoot ToLoot(this IEnumerable<IProduct> products)
        {
            var loot = new Loot();
            foreach (var item in products)
                loot.Add(item.Type, item.Quantity);

            return loot;
        }

        private class Loot : Domain.Quests.ILoot
        {
            private List<Domain.Quests.LootItem> _items = new();

            public void Add(IItemType item, int quantity)
            {
                _items.Add(new Domain.Quests.LootItem(item, quantity));
            }

            public IEnumerable<Domain.Quests.LootItem> Items => _items;
            public bool CanBeRemoved => false;
        }
    }
}
