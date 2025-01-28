﻿using System.Collections.Generic;
using System.Linq;
using Combat.Component.Unit.Classification;
using Combat.Scene;
using Economy.Products;
using GameDatabase;
using GameDatabase.DataModel;
using GameServices.Player;
using Model.Military;
using Zenject;

namespace Combat.Domain
{
    public interface ICombatModelBuilder
    {
        ICombatModel Build(IEnumerable<IProduct> specialLoot = null);
        IFleet PlayerFleet { get; }
        IFleet EnemyFleet { get; }
    }

    public class CombatModelBuilder : ICombatModelBuilder
    {
        public CombatModelBuilder(IDatabase database, PlayerSkills playerSkills, ShipDestroyedSignal shipDestroyedSignal)
        {
            _database = database;
            _playerSkills = playerSkills;
            _shipDestroyedSignal = shipDestroyedSignal;

            Rules = database.CombatSettings.DefaultCombatRules;
        }

        public IFleet EnemyFleet { get; set; }
        public IFleet PlayerFleet { get; set; }

        public CombatRules Rules { get; set; }
        public int StarLevel { get; set; }

        public void AddSpecialReward(IProduct item)
        {
            _specialReward.Add(item);
        }

        public void AddSpecialReward(IEnumerable<IProduct> items)
        {
            _specialReward.AddRange(items);
        }

        public void AddPlayerSkillLevelReward(int level)
        {
            _extraExperiences.Add(_playerSkills.Experience.ExpForFurtherLevel(level));
        }

        public ICombatModel Build(IEnumerable<IProduct> specialLoot = null)
        {
            var playerFleet = PlayerFleet ?? Model.Factories.Fleet.Empty;
            var enemyFleet = EnemyFleet ?? Model.Factories.Fleet.Empty;
            var useBonuses = !Rules.DisableSkillBonuses;

            var model = new CombatModel(
                new FleetModel(playerFleet.Ships, UnitSide.Player, _database, playerFleet.AiLevel, useBonuses ? _playerSkills : null),
                new FleetModel(enemyFleet.Ships, UnitSide.Enemy, _database, enemyFleet.AiLevel), _shipDestroyedSignal);

			var rules = Rules.Create(StarLevel, _playerSkills.HasRescueUnit);

			model.SpecialRewards = specialLoot != null ? _specialReward.Concat(specialLoot) : _specialReward;
            model.ExtraExperiences = _extraExperiences;
            model.Rules = rules;

            return model;
        }

        private readonly IDatabase _database;
        private readonly List<IProduct> _specialReward = new List<IProduct>();
        private readonly List<long> _extraExperiences = new List<long>();
        private readonly ShipDestroyedSignal _shipDestroyedSignal;
        private readonly PlayerSkills _playerSkills;

        public class Factory : Factory<CombatModelBuilder> { }
    }
}
