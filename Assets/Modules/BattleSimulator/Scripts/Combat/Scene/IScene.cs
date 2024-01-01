﻿using Combat.Component.Ship;
using Combat.Component.Unit;
using Combat.Component.Unit.Classification;
using UnityEngine;
using CommonComponents.Signals;

namespace Combat.Scene
{
    public interface IScene
    {
        void AddUnit(IUnit unit);

        IUnitList<IShip> Ships { get; }
        IUnitList<IUnit> Units { get; }

        IShip PlayerShip { get; }
        IShip EnemyShip { get; }

        Vector2 FindFreePlace(float minDistance, UnitSide unitSide);
        void Shake(float amplitude);

        Vector2 ViewPoint { get; }
        Rect ViewRect { get; }

        SceneSettings Settings { get; }
	
		void Clear();
	}

	public struct SceneSettings
    {
        public float AreaWidth;
        public float AreaHeight;
        public bool PlayerAlwaysInCenter;
    }

    public class ShipDestroyedSignal : SmartWeakSignal<ShipDestroyedSignal, IShip> {}
    public class ShipCreatedSignal : SmartWeakSignal<ShipCreatedSignal, IShip> {}
}
