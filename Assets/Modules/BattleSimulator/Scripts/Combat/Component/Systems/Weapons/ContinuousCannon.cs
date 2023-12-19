﻿using Combat.Component.Bullet;
using Combat.Component.Platform;
using Combat.Component.Triggers;
using Combat.Unit;
using GameDatabase.DataModel;
using UnityEngine;

namespace Combat.Component.Systems.Weapons
{
    public class ContinuousCannon : SystemBase, IWeapon
    {
        public ContinuousCannon(IWeaponPlatform platform, WeaponStats weaponStats, Factory.IBulletFactory bulletFactory, int keyBinding)
            : base(keyBinding, weaponStats.ControlButtonIcon)
        {
            MaxCooldown = weaponStats.FireRate > 0 ? 1f / weaponStats.FireRate : 0f;

            _bulletFactory = bulletFactory;
            _platform = platform;
            _energyConsumption = bulletFactory.Stats.EnergyCost;
			_activationCost = bulletFactory.Stats.ActivationCost;
            _spread = weaponStats.Spread;

            Info = new WeaponInfo(WeaponType.Continuous, _spread, bulletFactory, platform);
        }

		public override bool CanBeActivated => base.CanBeActivated && (HasActiveBullet || _platform.IsReady);
		public override float Cooldown => Mathf.Max(base.Cooldown, _platform.Cooldown);

		public WeaponInfo Info { get; private set; }
		public IWeaponPlatform Platform { get { return _platform; } }
		public float PowerLevel { get { return 1.0f; } }
		public IBullet ActiveBullet { get { return HasActiveBullet ? _activeBullet : null; } }

		protected override void OnUpdateView(float elapsedTime) {}

        protected override void OnUpdatePhysics(float elapsedTime)
        {
            if (HasActiveBullet)
            {
                _platform.Aim(Info.BulletSpeed, Info.Range, Info.IsRelativeVelocity);

                if (Active && _platform.EnergyPoints.TryGet(_energyConsumption * elapsedTime))
                {
                    _activeBullet.Lifetime.Restore();
                    InvokeTriggers(ConditionType.OnRemainActive);
                }
                else
                {
                    TimeFromLastUse = 0;
                    InvokeTriggers(ConditionType.OnDeactivate);
                }
            }
            else if (Active && CanBeActivated && _platform.EnergyPoints.TryGet(_activationCost))
            {
                Shot();
                InvokeTriggers(ConditionType.OnActivate);
            }
        }

        protected override void OnDispose() 
		{
			if (_bulletFactory.Stats.IsBoundToCannon)
				_activeBullet?.Vanish();
		}

        private void Shot()
        {
            _platform.Aim(Info.BulletSpeed, Info.Range, Info.IsRelativeVelocity);

            _activeBullet = _bulletFactory.Create(_platform, _spread, 0, 0);
            _activeBullet.Lifetime.Restore();
        }

		private bool HasActiveBullet => _activeBullet.IsActive();

		private IBullet _activeBullet;
        private readonly float _spread;
        private readonly float _energyConsumption;
		private readonly float _activationCost;
        private readonly IWeaponPlatform _platform;
        private readonly Factory.IBulletFactory _bulletFactory;
    }
}
