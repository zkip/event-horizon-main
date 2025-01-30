using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Domain;
using Constructor.Ships;
using Economy;
using Economy.ItemType;
using Economy.Products;
using GameDatabase;
using GameDatabase.Enums;
using GameServices.Economy;
using GameServices.Random;
using GameStateMachine.States;
using GameDatabase.DataModel;
using GameDatabase.Query;
using GameDatabase.Model;
using Model.Factories;
using Session;
using UnityEngine;
using Zenject;
using GameServices.Player;
using static Combat.Domain.CombatModel;

namespace Galaxy.StarContent
{
    public class Challenge
    {
        [Inject] private readonly ISessionData _session;
        [Inject] private readonly IRandom _random;
        [Inject] private readonly ItemTypeFactory _itemTypeFactory;
        [Inject] private readonly StarData _starData;
        [Inject] private readonly StarContentChangedSignal.Trigger _starContentChangedTrigger;
        [Inject] private readonly StartBattleSignal.Trigger _startBattleTrigger;
        [Inject] private readonly LootGenerator _lootGenerator;
        [Inject] private readonly IDatabase _database;
        [Inject] private readonly CombatModelBuilder.Factory _combatModelBuilderFactory;
        [Inject] private readonly PlayerSkills _playerSkills;

        public bool IsCompleted(int starId) { return GetCurrentLevel(starId) >= MaxLevel; }
        public int GetCurrentLevel(int starId) { return _session.CommonObjects.GetIntValue(starId); }
        public int MaxLevel { get { return 5; } }

        public ShipBuild GetEnemyShip(int starId)
        {
            var level = _starData.GetLevel(starId);
            var ships = ShipBuildQuery.EnemyShips(_database).
                CommonAndRare().
				FilterByStarDistance(level, ShipBuildQuery.FilterMode.Size | ShipBuildQuery.FilterMode.Faction).
                WithDifficulty(DifficultyClass.Default, DifficultyClass.Default).
				SelectUniqueRandom(MaxLevel, new System.Random(starId)).
                Take(MaxLevel).
                All.ToList();

            ships.Sort((first, second) => first.Ship.Layout.CellCount - second.Ship.Layout.CellCount);

            var stage = GetCurrentLevel(starId);
            if (ships.Count <= stage)
            {
                Debug.LogException(new InvalidOperationException("Challenge: no more ships - " + stage));
                return _database.GetShipBuild(new ItemId<ShipBuild>(DefaultShipBuild));
            }

            return ships[stage];
        }

        public ShipBuild GetPlayerShip(int starId)
        {
            var level = _starData.GetLevel(starId);
            var ship = ShipBuildQuery.PlayerShips(_database).
                Common().
				FilterByStarDistance(level, ShipBuildQuery.FilterMode.Faction).
                WithSizeClass(SizeClass.Frigate, SizeClass.Cruiser).
                Random(new System.Random(starId));

            return ship ?? _database.GalaxySettings.StartingShipBuilds.FirstOrDefault() ?? _database.GetShipBuild(new ItemId<ShipBuild>(DefaultShipBuild));
        }

        public void Attack(int starId)
        {
            if (IsCompleted(starId))
                throw new System.InvalidOperationException();

            var level = _starData.GetLevel(starId);

            var playerFleet = new Model.Military.SingleShip(new CommonShip(GetPlayerShip(starId), _database));

            var ailevel = 104; // TODO
            var enemyFleet = new Model.Military.SingleShip(new EnemyShip(GetEnemyShip(starId), _database), ailevel);

            var builder = _combatModelBuilderFactory.Create();
            builder.PlayerFleet = playerFleet;
            builder.EnemyFleet = enemyFleet;
            builder.Rules = _database.GalaxySettings.ChallengeCombatRules ?? _database.CombatSettings.DefaultCombatRules;
            builder.AddSpecialReward(GetReward(starId));

            if(_playerSkills.Experience.Level < level)
            {
                var step = GetCurrentLevel(starId);
                builder.AddPlayerSkillLevelReward(step + 1 < MaxLevel ? 1 : 2);
            }

            var combatModel = (CombatModel)builder.Build();
            combatModel.SetRewardsCondition((CombatModel combatModel, AvailableExtraLootType availableExtraType) => combatModel.IsVictory());

            _startBattleTrigger.Fire(combatModel, result => OnCombatCompleted(starId, result));
        }

        private IEnumerable<IProduct> GetReward(int starId)
        {
            var step = GetCurrentLevel(starId);
            var level = _starData.GetLevel(starId);
            var faction = _starData.GetRegion(starId).Faction;

            var random = _random.CreateRandom(starId + 98765);
            var componentSeed = starId + step + 3456;
            var targetFactions = new List<Faction> { faction };
            if (step + 1 < MaxLevel)
            {
                if (_lootGenerator.TryGetRandomComponent(componentSeed, targetFactions, false, ModificationQuality.P3, out var factionProduct))
                    yield return factionProduct;

                if (_lootGenerator.TryGetRandomComponent(componentSeed, targetFactions, true, ModificationQuality.P3, out var otherFactionProduct))
                    yield return otherFactionProduct;

                yield return Price.Premium(10 + random.Next(1 + step * 2, 10 + step * 5)).GetProduct(_itemTypeFactory);
            }
            else
            {
                for(var i = 0; i < 1 + level / 50; i++)
                {
                    if (_lootGenerator.TryGetRandomComponent(componentSeed, targetFactions, false, ModificationQuality.P3, out var factionProduct))
                        yield return factionProduct;

                    if (_lootGenerator.TryGetRandomComponent(componentSeed, targetFactions, true, ModificationQuality.P3, out var otherFactionProduct))
                        yield return otherFactionProduct;
                }
                yield return Price.Premium(100 + random.Next(10, 100)).GetProduct(_itemTypeFactory);
            }
        }

        private void OnCombatCompleted(int starId, ICombatModel result)
        {
            if (!result.IsVictory())
                return;

            _session.CommonObjects.SetIntValue(starId, GetCurrentLevel(starId) + 1);
            _starContentChangedTrigger.Fire(starId);
        }

        public struct Facade
        {
            public Facade(Challenge challenge, int starId)
            {
                _challenge = challenge;
                _starId = starId;
            }

            public ShipBuild GetEnemyShip() { return _challenge.GetEnemyShip(_starId); }
            public ShipBuild GetPlayerShip() { return _challenge.GetPlayerShip(_starId); }
            public bool IsCompleted { get { return _challenge.IsCompleted(_starId); } }
            public int CurrentLevel { get { return _challenge.GetCurrentLevel(_starId); } }
            public int MaxLevel { get { return _challenge.MaxLevel; } }
            public void Attack() { _challenge.Attack(_starId); }

            private readonly Challenge _challenge;
            private readonly int _starId;
        }

        private const int DefaultShipBuild = 39;
    }
}
