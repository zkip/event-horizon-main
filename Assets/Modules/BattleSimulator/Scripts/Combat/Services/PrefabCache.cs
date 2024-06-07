using System;
using UnityEngine;
using System.Collections.Generic;
using Combat.Component.Helpers;
using GameDatabase.DataModel;
using GameDatabase.Enums;
using GameDatabase.Model;
using Services.Resources;
using GameDatabase;
using Zenject;
using Combat.Services;

public class PrefabCache : MonoBehaviour 
{
    [Inject] 
    private void Initialize(IResourceLocator resourceLocator, IDatabase database)
    {
        _database = database;
        _resourceLocator = resourceLocator;
        _database.DatabaseLoaded += OnDatabaseLoaded;
        _customPrefabLoader = new Combat.Services.CustomPrefabLoader(this, resourceLocator);
    }

    private void OnDestroy()
    {
        _database.DatabaseLoaded -= OnDatabaseLoaded;
    }

    public GameObject LoadResourcePrefab(string path, bool noExceptions = false)
    {
        GameObject prefab;
        if (!_prefabs.TryGetValue(path, out prefab))
        {
            prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                if (!noExceptions) Debug.LogException(new ArgumentException("prefab not found: " + path));
                return null;
            }

            _prefabs[path] = prefab;
        }

        return prefab;
    }

    public GameObject LoadPrefab(string path)
	{
		GameObject prefab;
		if (!_prefabs.TryGetValue(path, out prefab))
		{
			prefab = Resources.Load<GameObject>("Prefabs/" + path);
		    if (prefab == null)
		    {
		        UnityEngine.Debug.Log("prefab not found: " + path);
		        return null;
		    }

		    _prefabs[path] = prefab;
		}

		return prefab;
	}

    public GameObject LoadPrefab(PrefabId prefabId)
    {
        var path = prefabId.ToString();
        GameObject prefab;
        if (!_prefabs.TryGetValue(path, out prefab))
        {
            prefab = Resources.Load<GameObject>(path);
            _prefabs[path] = prefab;
        }

        return prefab;
    }

    [Obsolete]
    public GameObject LoadBulletPrefabObsolete(PrefabId prefabId, string defaultPrefab, IGameServicesProvider servicesProvider)
    {
        var path = prefabId.ToString();
        GameObject prefab;
        if (_prefabs.TryGetValue(path, out prefab)) return prefab;

        prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            if (_prefabs.TryGetValue(defaultPrefab, out prefab))
                return prefab;

            prefab = Resources.Load<GameObject>(defaultPrefab);
            if (prefab == null)
                throw new InvalidOperationException();
        }

        _prefabs[path] = prefab;
        return prefab;
    }

    public GameObject LoadPrefab(GameObjectPrefab data)
    {
        if (data == null) return null;
        GameObject prefab;
        if (_customPrefabs.TryGetValue(data.Id.Value, out prefab))
            return prefab;

        prefab = data.Create(_customPrefabLoader);
        prefab.SetActive(false);
        prefab.transform.parent = transform;
        _customPrefabs.Add(data.Id.Value, prefab);
        return prefab;
    }

    public GameObject GetBulletPrefab(BulletPrefab data)
    {
        var id = data != null ? data.Id.Value : 0;
        GameObject prefab;
        if (_bulletPrefabs.TryGetValue(id, out prefab))
            return prefab;

        GameObject commonPrefab;
        if (data == null)
            commonPrefab = LoadPrefab(new PrefabId("AreaOfEffect", PrefabId.Type.Bullet));
        else
            switch (data.Shape)
            {
                case BulletShape.Projectile: commonPrefab = LoadPrefab(new PrefabId("CommonProjectile", PrefabId.Type.Bullet)); break;
                case BulletShape.Rocket: commonPrefab = LoadPrefab(new PrefabId("CommonRocket", PrefabId.Type.Bullet)); break;
                case BulletShape.LaserBeam: commonPrefab = LoadPrefab(new PrefabId("Laser", PrefabId.Type.Bullet)); break;
                case BulletShape.LightningBolt: commonPrefab = LoadPrefab(new PrefabId("Lightning", PrefabId.Type.Bullet)); break;
                case BulletShape.EnergyBeam: commonPrefab = LoadPrefab(new PrefabId("EnergyBeam", PrefabId.Type.Bullet)); break;
                case BulletShape.PiercingLaser: commonPrefab = LoadPrefab(new PrefabId("PiercingLaserBeam", PrefabId.Type.Bullet)); break;
                case BulletShape.Spark: commonPrefab = LoadPrefab(new PrefabId("Spark", PrefabId.Type.Bullet)); break;
                case BulletShape.Mine: commonPrefab = LoadPrefab(new PrefabId("Mine", PrefabId.Type.Bullet)); break;
                case BulletShape.Wave: commonPrefab = LoadPrefab(new PrefabId("Arc", PrefabId.Type.Bullet)); break;
                case BulletShape.BlackHole: commonPrefab = LoadPrefab(new PrefabId("WormHole", PrefabId.Type.Bullet)); break;
                case BulletShape.Harpoon: commonPrefab = LoadPrefab(new PrefabId("Hook", PrefabId.Type.Bullet)); break;
                case BulletShape.CircularSaw: commonPrefab = LoadPrefab(new PrefabId("CircularSaw", PrefabId.Type.Bullet)); break;
                default: return null;
            }

        prefab = Instantiate(commonPrefab);
        prefab.SetActive(false);
        prefab.transform.parent = transform;
        prefab.GetComponent<IBulletPrefabInitializer>().Initialize(data, _resourceLocator);

        _bulletPrefabs.Add(id, prefab);
        return prefab;
    }

    public GameObject GetEffectPrefab(VisualEffectElement data)
    {
        var id = (int)data.Type + data.Image.Id;
        GameObject prefab;
        if (_effectPrefabs.TryGetValue(id, out prefab))
            return prefab;

        GameObject commonPrefab;
        switch (data.Type)
        {
            case VisualEffectType.Flash: 
                commonPrefab = LoadPrefab(new PrefabId("Flash", PrefabId.Type.Effect)); 
                break;
            case VisualEffectType.FlashAdditive: 
                commonPrefab = LoadPrefab(new PrefabId("FlashAdditive", PrefabId.Type.Effect)); 
                break;
            case VisualEffectType.Shockwave: 
                commonPrefab = LoadPrefab(new PrefabId("Wave", PrefabId.Type.Effect)); 
                break;
            case VisualEffectType.Smoke: 
                commonPrefab = LoadPrefab(new PrefabId("SmokeSimple", PrefabId.Type.Effect)); 
                break;
            case VisualEffectType.SmokeAdditive: 
                commonPrefab = LoadPrefab(new PrefabId("SmokeSimpleAdditive", PrefabId.Type.Effect)); 
                break;
            case VisualEffectType.Spark:
                commonPrefab = LoadPrefab(new PrefabId("Spark", PrefabId.Type.Effect));
                break;
            case VisualEffectType.Sprite:
                commonPrefab = LoadPrefab(new PrefabId("Sprite", PrefabId.Type.Effect));
                break;
            case VisualEffectType.Lightning:
            case VisualEffectType.LightningStrike:
                commonPrefab = LoadPrefab(new PrefabId("LightningStrike", PrefabId.Type.Effect)); 
                break;
            default:
                return null;
        }

        prefab = Instantiate(commonPrefab);
        prefab.SetActive(false);
        prefab.transform.parent = transform;
        prefab.GetComponent<IEffectPrefabInitializer>().Initialize(data, _resourceLocator);

        _effectPrefabs.Add(id, prefab);
        return prefab;
    }

    private void OnDatabaseLoaded()
    {
        _bulletPrefabs.Clear();
        _effectPrefabs.Clear();
        _customPrefabs.Clear();
    }

    private IDatabase _database;
    private IResourceLocator _resourceLocator;
    private Combat.Services.CustomPrefabLoader _customPrefabLoader;
    private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> _effectPrefabs = new Dictionary<string, GameObject>();
    private readonly Dictionary<int, GameObject> _bulletPrefabs = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, GameObject> _customPrefabs = new Dictionary<int, GameObject>();
}
