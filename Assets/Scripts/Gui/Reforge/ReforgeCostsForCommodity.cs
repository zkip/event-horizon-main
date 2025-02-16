using Economy;
using Economy.ItemType;
using Economy.Products;
using GameDatabase;
using GameDatabase.DataModel;
using GameDatabase.Enums;
using GameDatabase.Model;
using GameModel.Quests;
using Services.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace Gui.Reforge
{
    public interface IReforgeCosts<T>
    {
        public IEnumerable<IProduct> GetCosts(ReforgeStyle reforgeStyle, T context);
    }

    public struct ReforgeContext
    {
        public Faction faction;
        public IItemType target;
        public int spaceLevel;
        public int ignoreLevel;
        public float discount;
        public bool notRequiredStars;
    }

    class ReforgeCostsForCommodity: IReforgeCosts<ReforgeContext>
    {
        private enum ReforgeModification
        {
            P3 = 6, P2 = 5, P1 = 4, O = 3, N1 = 2, N2 = 1, N3 = 0
        }
        private Dictionary<ModificationQuality, ReforgeModification> _modQualityMap = new Dictionary<ModificationQuality, ReforgeModification>
        {
            {ModificationQuality.P3, ReforgeModification.P3 },
            {ModificationQuality.P2, ReforgeModification.P2 },
            {ModificationQuality.P1, ReforgeModification.P1 },
            {ModificationQuality.N1, ReforgeModification.N1 },
            {ModificationQuality.N2, ReforgeModification.N2 },
            {ModificationQuality.N3, ReforgeModification.N3 },
        };

        public ReforgeCostsForCommodity(IDatabase database, ItemTypeFactory factory)
        {
            _database = database;
            _factory = factory;
        }

        public IEnumerable<IProduct> GetCosts(ReforgeStyle reforgeStyle, ReforgeContext context) {
            switch (reforgeStyle)
            {
                case ReforgeStyle.RandomAll: return getRandomAllCost(context);
                case ReforgeStyle.RandomModification: return getRandomModificationCost(context);
                case ReforgeStyle.UpgradeThenRandom: return getUpgradeCost(context);
                default: throw new ArgumentException();
            }
        }

        private float max(float a,float b) { return Mathf.Max(a, b); }
        private float min(float a, float b) { return Mathf.Min(a, b); }
        private float lerp(float a, float b, float t) { return Mathf.Lerp(a, b, t); }

        private ReforgeModification getComponentModQuality(ComponentItem componentItem) {
            if (componentItem.Component.ModificationType == ComponentMod.Empty && componentItem.Component.ModificationQuality == ModificationQuality.N3)
                return ReforgeModification.O;
            return _modQualityMap[componentItem.Component.ModificationQuality];
        }

        private float getBaseQuantity(ReforgeContext context, bool isApplySmallHighXLDiscount = false, bool isUpgrade = false, bool isReverse = false)
        {
            var componentItem = (ComponentItem)context.target;
            var data = componentItem.Component.Data;

            var P = (float)data.PossibleModifications.Count();
            var L = (float)data.Level;
            var S = (float)data.Layout.Size;
            var Q = max((float)getComponentModQuality(componentItem) + (isUpgrade ? 1f : 0f), 0f);
            var U = data.Restrictions.MaxComponentAmount > 0;
            var X = (float)context.spaceLevel;

            if (isReverse)
            {
                P = max(8f - P, 0f);
                L = max(300f - L, 0f);
                Q = max(6f - Q, 0f);
            }

            var spaceDisCount = min(X / 1000f, 0.3f);
            spaceDisCount += min(max((X - 300f)/1500f, 0f), 0.2f);

            var smallHighXLevelDiscount = isApplySmallHighXLDiscount && isUpgrade && X >= 150 && S <= 9 ? lerp(0.7f, 0.1f, S / 9f) : 0f;

            var scale = 1f;
            scale = U ? scale * 2f : scale;

            var PP = 12f;
            var LL = 0.4f;
            var SS = 4f;
            var QQ = 15f;
            var SS2 = 8f;
            var wholeScale = 0.15f;

            return (
                    max(8f - P, 1f) * PP + L * LL + S * SS + Q * QQ
                        + max(S - 16f, 0f) * SS2
                ) * wholeScale * scale * (1f - spaceDisCount) * (1f - smallHighXLevelDiscount) * (1f - context.discount);
        }

        private IProduct createReforgeProduct(CommodityType commodityType, float quantity)
        {
            var costQuantity = quantity;
            switch (commodityType)
            {
                case CommodityType.Gems:
                case CommodityType.Artifacts:
                case CommodityType.PreciousMetals: costQuantity *= 1f; break;

                case CommodityType.Polymers:
                case CommodityType.Alloys: costQuantity *= 5f; break;

                case CommodityType.Scraps: costQuantity *= 10f; break;

                case CommodityType.Minerals:
                case CommodityType.ToxicWaste: costQuantity *= 25f; break;

                default: throw new ArgumentException();
            }

            return CommonProduct.Create(CreateArtifact(commodityType), Mathf.RoundToInt(costQuantity));
        }

        private IProduct createCreditProduct(ComponentItem componentItem, float basePrice, bool isReverse = false)
        {
            var data = componentItem.Component.Data;

            var P = (float)data.PossibleModifications.Count();
            var L = (float)data.Level;
            var S = (float)data.Layout.Size;
            var Q = (float)getComponentModQuality(componentItem);
            var U = (float)data.Restrictions.MaxComponentAmount > 0;

            if (isReverse)
            {
                P = max(8f - P, 0f);
                L = max(300f - L, 0f);
                Q = max(6f - Q, 0f);
            }

            return Price.Common(Mathf.RoundToInt(basePrice * max(P, 1f) * max(S, 1f) * max(Q, 1) * (L / 50 + 1) * (U ? 5 : 1))).GetProduct(_factory);
        }
        private IProduct createStarProduct(ComponentItem componentItem, bool isReverse = false)
        {
            var data = componentItem.Component.Data;

            var P = (float)data.PossibleModifications.Count();
            var S = (float)data.Layout.Size;
            var Q = (float)getComponentModQuality(componentItem);

            if (isReverse)
            {
                P = max(8f - P, 0f);
                Q = max(6f - Q, 0f);
            }

            return Price.Premium(Mathf.RoundToInt((P + S / 2) * max(1, Q - 4))).GetProduct(_factory);
        }

        private IEnumerable<IProduct> getRandomAllCost(ReforgeContext context)
        {
            if (context.target == null || !(context.target is ComponentItem)) yield break;

            var componentItem = (ComponentItem)context.target;

            if(!context.notRequiredStars) yield return createStarProduct(componentItem, true);
            yield return createCreditProduct(componentItem, 30f * (1f - context.discount), true);

            var quality = getComponentModQuality(componentItem);
            var quantity = getBaseQuantity(context, false, false, true) * 1f;

            float scale;
            switch (quality)
            {
                case ReforgeModification.P3:
                    yield return createReforgeProduct(CommodityType.Scraps, quantity);
                    break;

                case ReforgeModification.P1:
                case ReforgeModification.P2:
                    scale = quality == ReforgeModification.P1 ? 1.25f : 1f;
                    yield return createReforgeProduct(CommodityType.Scraps, quantity * scale);
                    yield return createReforgeProduct(CommodityType.Minerals, quantity * scale);
                    break;

                case ReforgeModification.O:
                    yield return createReforgeProduct(CommodityType.Scraps, quantity);
                    yield return createReforgeProduct(CommodityType.Minerals, quantity);
                    yield return createReforgeProduct(CommodityType.Alloys, quantity);
                    break;

                case ReforgeModification.N1:
                case ReforgeModification.N2:
                case ReforgeModification.N3:
                    scale = 1 + (2f - (float)quality) * 0.15f;
                    yield return createReforgeProduct(CommodityType.Scraps, quantity * scale);
                    yield return createReforgeProduct(CommodityType.Alloys, quantity * scale);
                    yield return createReforgeProduct(CommodityType.Polymers, quantity * scale);
                    break;
            }
        }
        private IEnumerable<IProduct> getRandomModificationCost(ReforgeContext context)
        {
            if (context.target == null || !(context.target is ComponentItem)) yield break;

            var componentItem = (ComponentItem)context.target;

            if (!context.notRequiredStars) yield return createStarProduct(componentItem);
            yield return createCreditProduct(componentItem, 150f * (1f - context.discount));

            var quality = getComponentModQuality(componentItem);
            var quantity = getBaseQuantity(context) * 2f;

            var data = componentItem.Component.Data;
            var L = (float)data.Level;
            var S = (float)data.Layout.Size;
            var Q = (float)quality;
            var X = context.spaceLevel;

            var requiredRareMaterial = ( L > 100 && Q > 4) && (X < 150 || S > 9);

            switch (quality)
            {
                case ReforgeModification.P3:
                    if (requiredRareMaterial) yield return createReforgeProduct(CommodityType.Gems, quantity);
                    if (requiredRareMaterial) yield return createReforgeProduct(CommodityType.Artifacts, quantity * 0.25f);
                    if (!requiredRareMaterial) yield return createReforgeProduct(CommodityType.Polymers, quantity * 1.25f);
                    yield return createReforgeProduct(CommodityType.Alloys, quantity);
                    yield return createReforgeProduct(CommodityType.ToxicWaste, quantity);
                    break;

                case ReforgeModification.P2:
                    yield return createReforgeProduct(CommodityType.Polymers, quantity);
                    yield return createReforgeProduct(CommodityType.Alloys, quantity);
                    yield return createReforgeProduct(CommodityType.ToxicWaste, quantity);
                    break;

                case ReforgeModification.P1:
                    yield return createReforgeProduct(CommodityType.Minerals, quantity);
                    yield return createReforgeProduct(CommodityType.Alloys, quantity);
                    break;
                case ReforgeModification.O:
                    yield return createReforgeProduct(CommodityType.Minerals, quantity * 0.75f);
                    yield return createReforgeProduct(CommodityType.Alloys, quantity * 0.75f);
                    break;

                case ReforgeModification.N1:
                case ReforgeModification.N2:
                case ReforgeModification.N3:
                    yield return createReforgeProduct(CommodityType.Minerals, quantity * (0.5f + (float)quality * 0.15f));
                    yield return createReforgeProduct(CommodityType.ToxicWaste, quantity * (0.75f + (float)quality * 0.15f));
                    break;
            }
        }
        private IEnumerable<IProduct> getUpgradeCost(ReforgeContext context)
        {
            if (context.target == null || !(context.target is ComponentItem)) yield break;

            var componentItem = (ComponentItem)context.target;

            if (!context.notRequiredStars) yield return createStarProduct(componentItem);
            yield return createCreditProduct(componentItem, 300f * (1f - context.discount));

            var quality = getComponentModQuality(componentItem);
            var quantity = getBaseQuantity(context, false, true) * 5f;
            var rareQuantity = getBaseQuantity(context, true, true) * 5f;

            var isAlloyNeed = componentItem.Component.Data.Level > 100;

            switch (quality)
            {
                case ReforgeModification.P3:
                    break;

                case ReforgeModification.P2:
                    yield return createReforgeProduct(CommodityType.Artifacts, rareQuantity * 0.75f);
                    yield return createReforgeProduct(CommodityType.PreciousMetals, rareQuantity * 0.25f);
                    yield return createReforgeProduct(CommodityType.Polymers, quantity * 0.75f);
                    yield return createReforgeProduct(CommodityType.Minerals, quantity * 0.5f);
                    yield return createReforgeProduct(CommodityType.Scraps, quantity * 0.5f);
                    break;

                case ReforgeModification.P1:
                    yield return createReforgeProduct(CommodityType.PreciousMetals, rareQuantity * 0.5f);
                    yield return createReforgeProduct(CommodityType.Polymers, quantity);
                    yield return createReforgeProduct(CommodityType.Minerals, quantity * 0.75f);
                    yield return createReforgeProduct(CommodityType.Scraps, quantity * 0.75f);
                    break;
                case ReforgeModification.O:
                    yield return createReforgeProduct(CommodityType.Alloys, rareQuantity * 0.75f);
                    yield return createReforgeProduct(CommodityType.Polymers, quantity * 0.75f);
                    yield return createReforgeProduct(CommodityType.Minerals, quantity * 0.5f);
                    yield return createReforgeProduct(CommodityType.Scraps, quantity * 0.5f);
                    break;

                case ReforgeModification.N1:
                    yield return createReforgeProduct(CommodityType.Alloys, rareQuantity * 0.5f);
                    yield return createReforgeProduct(CommodityType.Polymers, quantity * 0.5f);
                    yield return createReforgeProduct(CommodityType.Minerals, quantity * 0.25f);
                    yield return createReforgeProduct(CommodityType.Scraps, quantity * 0.25f);
                    break;
                case ReforgeModification.N2:
                case ReforgeModification.N3:
                    if (isAlloyNeed) yield return createReforgeProduct(CommodityType.Alloys, quantity * 0.25f);
                    yield return createReforgeProduct(CommodityType.Minerals, quantity * (isAlloyNeed ? 1f : 1.25f));
                    yield return createReforgeProduct(CommodityType.Scraps, quantity * (isAlloyNeed ? 0.5f : 0.75f));
                    break;
            }
        }

        private IItemType CreateArtifact(CommodityType commodityType)
        {
            var artifact = _database.GetQuestItem(new ItemId<QuestItem>((int)commodityType));
            return _factory.CreateArtifactItem(artifact);
        }

        private IDatabase _database;
        private ItemTypeFactory _factory;
    }
}
