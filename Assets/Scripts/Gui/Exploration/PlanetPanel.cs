using Constructor.Ships;
using Economy;
using Game.Exploration;
using GameServices.Player;
using GameStateMachine.States;
using Gui.Combat;
using Services.Gui;
using Services.Localization;
using Session;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ViewModel.Common;
using Zenject;
using System.Linq;
using System;
using Services.Resources;

namespace Gui.Exploration
{
	public class PlanetPanel : MonoBehaviour
    {
        [Inject] private readonly IResourceLocator _resourceLocator;
        [Inject] private readonly Planet.Factory _factory;
	    [Inject] private readonly ILocalization _localization;
	    [Inject] private readonly PlayerResources _playerResources;
	    [Inject] private readonly ISessionData _session;
	    [Inject] private readonly StartExplorationSignal.Trigger _startExplorationTrigger;
	    [Inject] private readonly PlayerFleet _playerFleet;
        [Inject] private readonly MotherShip _motherShip;

	    [SerializeField] private Text _factionText;
	    [SerializeField] private Image _factionIcon;
	    [SerializeField] private Text _levelText;
	    [SerializeField] private FleetPanel _fleetPanel;
        [SerializeField] private PlanetInfo _planetInfo;
        [SerializeField] private VerticalLayoutGroup _resourceExporationPanel;
        [SerializeField] private Text _exploredText;
		[SerializeField] private Text _notExploredText;
	    [SerializeField] private PricePanel _price;
	    [SerializeField] private Text _fuelText;
	    [SerializeField] private GameObject _notEnoughFuel;
	    [SerializeField] private Button _exploreButton;

        private struct ObjectiveInfoX
        {
            public int id;
            public bool visited;
        }

        public void StartExploration()
		{
            if (_playerFleet.ExplorationShip == null) return;
		    if (!_playerResources.TryConsumeFuel(Planet.RequiredFuel)) return;
            if (!GetPrice().TryWithdraw(_playerResources)) return;
            
		    _startExplorationTrigger.Fire(_planet);
        }

	    public void Initialize(WindowArgs args)
        {
            _resourceExporation = _resourceExporationPanel.transform.Children<HorizontalLayoutGroup>().First();

            if (!args.TryGet<int>(0, out var index))
	            return;

	        _planet = _factory.Create(_motherShip.Position, index);

            _factionText.text = _localization.GetString(_planet.Faction.Name);
	        _factionIcon.color = _planet.Faction.Color;
	        _levelText.text = _planet.Level.ToString();

            _fleetPanel.Initialize();
            _planetInfo.UpdatePlanet(_planet);

	        var count = _planet.ObjectivesExplored;
	        _notExploredText.gameObject.SetActive(count == 0);
	        _exploredText.gameObject.SetActive(count > 0);
	        _exploredText.text = _localization.GetString("$ExplorationProgress", 100 * count / _planet.TotalObjectives);

            updateObjectiveTypes();
            UpdateButton();
            updatePlanetObjectivesInfo();
	    }

        private void updatePlanetObjectivesInfo()
        {
            var shownCount = 0;
            for (var i = 0; i < showPriority.Count; i++)
            {
                var objectiveType = showPriority[i];

                var infos = objectiveDict.GetValueOrDefault(objectiveType, new List<ObjectiveInfoX>());
                var count = infos.Count;
                if (count == 0) continue;

                var typePanel = activateInRange(_resourceExporationPanel, 1 + shownCount, _resourceExporation);

                var resourceTpl = typePanel.transform.Children<ObjectiveBeacom>().First();

                for (var j = 0; j < count; j++)
                {
                    var resource = activateInRange(typePanel, 1 + j, resourceTpl);
                    resource.Initialize(objectiveType, infos[j].visited,  _resourceLocator);
                }

                deactivateOutOfRange(typePanel, 1 + count);
                shownCount++;
            }

            deactivateOutOfRange(_resourceExporationPanel, 1 + shownCount);
        }

        private bool IsCompleted(int objectiveID)
        {
            return ((_session.StarMap.GetPlanetData(_planet.StarId, _planet.Index) >> objectiveID) & 1) == 1;
        }

        private Tc activateInRange<Tp, Tc>(Tp parent, int childIndex, Tc instOriginal) where Tp : Component where Tc : Component
        {
            if (childIndex < parent.transform.childCount) {
                var child = parent.transform.GetChild(childIndex).GetComponent<Tc>();
                child.gameObject.SetActive(true);
                return child;
            }

            var inst = Instantiate(instOriginal, parent.transform);
            inst.gameObject.SetActive(true);
            return inst;
        }
        private void deactivateOutOfRange<T>(T parent, int expectCount) where T : Component
        {
            var pcount = parent.transform.childCount;
            for (var i = 0; i < pcount - expectCount; i++)
            {
                parent.transform.GetChild(i + expectCount).gameObject.SetActive(false);
            }
        }


        public void OnShipSelected(IShip ship)
	    {
	        _playerFleet.ExplorationShip = ship;
            UpdateButton();
	    }

        private void UpdateButton()
        {
            var haveEnoughFuel = _playerResources.Fuel >= Planet.RequiredFuel;
            _fuelText.text = Planet.RequiredFuel.ToString();
            _notEnoughFuel.SetActive(!haveEnoughFuel);

            var price = GetPrice();
            var haveEnoughMoney = price.IsEnough(_playerResources);

            if (price.Amount == 0)
                _price.gameObject.SetActive(false);
            else
                _price.Initialize(price, haveEnoughMoney);

            _exploreButton.gameObject.SetActive(_planet.TotalObjectives > _planet.ObjectivesExplored);
            _exploreButton.interactable = haveEnoughMoney && haveEnoughFuel && _playerFleet.ExplorationShip != null;
        }

        private Price GetPrice()
	    {
            if (!_planet.WasExplored) return Price.Premium(0);

	        var price = Mathf.Min(10, 1 + _planet.Level/5);
            return Price.Premium(price);
	    }

        private Planet _planet;
        private List<ObjectiveType> showPriority = new List<ObjectiveType> {
            ObjectiveType.Outpost,
            ObjectiveType.Hive,
            ObjectiveType.Container,
            ObjectiveType.ShipWreck,
            ObjectiveType.MineralsRare,
            ObjectiveType.Minerals,
            ObjectiveType.Meteorite,
            // TODO: XmasBox,
        };
        private Dictionary<ObjectiveType, List<ObjectiveInfoX>> objectiveDict = new Dictionary<ObjectiveType, List<ObjectiveInfoX>>();
        private HorizontalLayoutGroup _resourceExporation;

        private void updateObjectiveTypes()
        {
            objectiveDict = new Dictionary<ObjectiveType, List<ObjectiveInfoX>>();
            var random = new System.Random(_planet.Seed);

            for (var i = 0; i < _planet.TotalObjectives; ++i)
            {
                var id = i;
                ObjectiveType objectiveType;
                switch (_planet.Type)
                {
                    case PlanetType.Gas: objectiveType = CreateGasPlanetObjective(random, i); break;
                    case PlanetType.Barren: objectiveType = CreateBarrenPlanetObjective(random, i); break;
                    case PlanetType.Terran: objectiveType = CreateTerranPlanetObjective(random, i, _planet); break;
                    case PlanetType.Infected: objectiveType = CreateInfectedPlanetObjective(random, i); break;
                    default: throw new System.ArgumentException("Invalid planet type: " + _planet.Type);
                }

                bool visited = IsCompleted(id);
                var infos = objectiveDict.GetValueOrDefault(objectiveType, new List<ObjectiveInfoX>());
                infos.Add(new ObjectiveInfoX{ id = id, visited = visited});
                objectiveDict[objectiveType] = infos;
            }
        }

        private ObjectiveType CreateGasPlanetObjective(System.Random random, int id)
        {
            var value = random.Next(100);
            if (value < 30) return ObjectiveType.Meteorite;
            if (value < 40) return ObjectiveType.MineralsRare;
            if (value < 50) return ObjectiveType.ShipWreck;
            if (value < 60) return ObjectiveType.Container;
            return ObjectiveType.Minerals;
        }

        private ObjectiveType CreateBarrenPlanetObjective(System.Random random, int id)
        {
            var value = random.Next(100);
            if (value < 30) return ObjectiveType.Meteorite;
            if (value < 40) return ObjectiveType.MineralsRare;
            if (value < 60) return ObjectiveType.ShipWreck;
            if (value < 70) return ObjectiveType.Container;
            if (value < 72) return ObjectiveType.Outpost;
            return ObjectiveType.Minerals;
        }

        private ObjectiveType CreateTerranPlanetObjective(System.Random random, int id, Planet planet)
        {
            var haveOutpost = planet.Level > 10;

            if (id == 0 && haveOutpost) return ObjectiveType.Outpost;

            var value = random.Next(100);
            if (value < 40) return ObjectiveType.Meteorite;
            if (value < 45) return ObjectiveType.MineralsRare;
            if (value < 60) return ObjectiveType.Minerals;
            if (value < 75) return ObjectiveType.ShipWreck;
            if (value < 95) return ObjectiveType.Container;
            return haveOutpost ? ObjectiveType.Outpost : ObjectiveType.Container;
        }

        private ObjectiveType CreateInfectedPlanetObjective(System.Random random, int id)
        {
            if (id == 0) return ObjectiveType.Hive;

            var value = random.Next(100);
            if (value < 40) return ObjectiveType.Meteorite;
            if (value < 45) return ObjectiveType.MineralsRare;
            if (value < 60) return ObjectiveType.Minerals;
            if (value < 75) return ObjectiveType.ShipWreck;
            if (value < 95) return ObjectiveType.Container;
            return ObjectiveType.Hive;
        }

    }
}
