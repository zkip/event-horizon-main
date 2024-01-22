﻿using System.Collections.Generic;
using Combat.Collision;
using Combat.Component.Stats;
using Combat.Component.Unit;
using GameDatabase.DataModel;
using GameDatabase.Model;

namespace Combat.Component.Systems.Devices
{
    public class WormTailDevice : SystemBase, IDevice, IStatsModification
    {
        public WormTailDevice(DeviceStats deviceSpec, IEnumerable<WormSegment> wormTail)
            : base(-1, SpriteId.Empty)
        {
			DeviceClass = deviceSpec.DeviceClass;
            _units = new List<WormSegment>(wormTail);
            _size = _units.Count;
        }

		public GameDatabase.Enums.DeviceClass DeviceClass { get; }
		public override float ActivationCost { get { return 0f; } }
        public override bool CanBeActivated { get { return false; } }

        public override IStatsModification StatsModification { get { return this; } }
        public bool TryApplyModification(ref Resistance data)
        {
            var power = _units.Count / (float)_size;

            data.Heat = power + (1f - power) * data.Heat;
            data.Energy = power + (1f - power) * data.Energy;
            data.Kinetic = power + (1f - power) * data.Kinetic;

            return true;
        }

        public void Deactivate() {}

        protected override void OnUpdatePhysics(float elapsedTime)
        {
            _updateCooldown -= elapsedTime;
            if (_updateCooldown > 0)
                return;

            _updateCooldown = 0.5f;
            _units.RemoveAll(item => !item.Enabled);
        }

        protected override void OnUpdateView(float elapsedTime)
        {
        }

        protected override void OnDispose() { }

        private float _updateCooldown;
        private readonly int _size;
        private readonly List<WormSegment> _units;
    }
}
