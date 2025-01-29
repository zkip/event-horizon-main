using Combat.Component.Unit.Classification;
using Combat.Domain;
using Economy;
using Economy.ItemType;
using Economy.Products;
using GameDatabase;
using GameDatabase.Enums;
using GameServices.Economy;
using GameServices.Player;
using GameServices.Random;
using GameStateMachine.States;
using Session;
using System.Collections.Generic;
using Zenject;
using static Combat.Domain.CombatModel;

namespace Galaxy.StarContent
{
    public class Survival
    {
        [Inject] private readonly ISessionData _session;
        [Inject] private readonly IRandom _random;
        [Inject] private readonly PlayerFleet _playerFleet;
        [Inject] private readonly StarData _starData;
        [Inject] private readonly StarContentChangedSignal.Trigger _starContentChangedTrigger;
        [Inject] private readonly StartBattleSignal.Trigger _startBattleTrigger;
        [Inject] private readonly IDatabase _database;
        [Inject] private readonly CombatModelBuilder.Factory _combatModelBuilderFactory;
        [Inject] private readonly LootGenerator _lootGenerator;
        [Inject] private readonly ItemTypeFactory _itemTypeFactory;
        [Inject] private readonly PlayerSkills _playerSkills;

        public int GetCurrentLevel(int starId) { return _session.CommonObjects.GetIntValue(starId); }

        public long GetLastAttackTime(int starId)
        {
            return _session.CommonObjects.GetUseTime(starId);
        }

        public long CooldownTime { get { return System.TimeSpan.TicksPerHour*4; } }

        public Model.Military.IFleet CreateFleet(int starId)
        {
            return Model.Factories.Fleet.Survival(_starData.GetLevel(starId), 
                _starData.GetRegion(starId).Faction, starId + _random.Seed, _database);
        }

        public void Attack(int starId)
        {
            if (System.DateTime.UtcNow.Ticks < GetLastAttackTime(starId) + CooldownTime)
                throw new System.InvalidOperationException();

            var level = _starData.GetLevel(starId);
            var firstFleet = new Model.Military.PlayerFleet(_database, _playerFleet);
            var secondFleet = CreateFleet(starId);

            var builder = _combatModelBuilderFactory.Create();
            builder.PlayerFleet = firstFleet;
            builder.EnemyFleet = secondFleet;
            builder.Rules = _database.GalaxySettings.SurvivalCombatRules ?? _database.CombatSettings.DefaultCombatRules;
            builder.StarLevel = level;
            builder.AddSpecialReward(GetReward(starId));

            if (_playerSkills.Experience.Level < level)
            {
                var step = GetCurrentLevel(starId);
                builder.AddPlayerSkillLevelReward(3);
            }

            var combatModel = (CombatModel)builder.Build();
            combatModel.SetRewardsCondition((CombatModel combatModel, AvailableExtraLootType availableExtraType) => combatModel.IsVictory());

            _startBattleTrigger.Fire(combatModel, result => OnCombatCompleted(starId));

            _session.CommonObjects.SetUseTime(starId, System.DateTime.UtcNow.Ticks);
        }

        private IEnumerable<IProduct> GetReward(int starId)
        {
            var level = _starData.GetLevel(starId);

            var random = _random.CreateRandom(starId + 73413);
            var componentSeed = starId + 3456;

            var rewardLevel = level / 25;

            for (var i = 0; i < 6 + rewardLevel; i++)
            {
                if (_lootGenerator.TryGetRandomComponent(componentSeed + i, true, null, ModificationQuality.P3, out var otherFactionExtraProduct))
                    yield return otherFactionExtraProduct;
            }

            yield return Price.Premium(100 + random.Next(10, 100) * rewardLevel).GetProduct(_itemTypeFactory);
        }

        private void OnCombatCompleted(int starId)
        {
            _starContentChangedTrigger.Fire(starId);
        }

        public struct Facade
        {
            public Facade(Survival survival, int starId)
            {
                _survival = survival;
                _starId = starId;
            }

            public Model.Military.IFleet CreateFleet() { return _survival.CreateFleet(_starId); }
            public long LastAttackTime { get { return _survival.GetLastAttackTime(_starId); } }
            public long CooldownTime { get { return _survival.CooldownTime; } }
            public void Attack() { _survival.Attack(_starId); }

            private readonly Survival _survival;
            private readonly int _starId;
        }
    }
}
