﻿using System;
using UnityEngine;

namespace ShipEditor.Model
{
	[Serializable]
	public struct ShipElementContainer<T> where T : class
	{
		[SerializeField] private T _shipLayout;
		[SerializeField] private T _satelliteLayoutL;
		[SerializeField] private T _satelliteLayoutR;

		public T this[ShipElementType elementType]
		{
			get
			{
				switch (elementType)
				{
					case ShipElementType.Ship: return _shipLayout;
					case ShipElementType.SatelliteL: return _satelliteLayoutL;
					case ShipElementType.SatelliteR: return _satelliteLayoutR;
					default: return null;
				}
			}
			set
			{
				switch (elementType)
				{
					case ShipElementType.Ship: _shipLayout = value; break;
					case ShipElementType.SatelliteL: _satelliteLayoutL = value; break;
					case ShipElementType.SatelliteR: _satelliteLayoutR = value; break;
				}
			}
		}

		public T this[SatelliteLocation location]
		{
			get => this[ToShipElement(location)];
			set => this[ToShipElement(location)] = value;
		}

		private static ShipElementType ToShipElement(SatelliteLocation location)
		{
			if (location == SatelliteLocation.Left)
				return ShipElementType.SatelliteL;
			else if (location == SatelliteLocation.Right)
				return ShipElementType.SatelliteR;
			else
				return ShipElementType.Ship;
		}
	}
}
