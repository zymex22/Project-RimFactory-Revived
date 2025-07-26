using ProjectRimFactory.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public class ITab_SmartHopper : ITab
    {
        
        private static readonly Vector2 WinSize = new(200f, 200f);

        private IPickupSettings Hopper => SelThing as IPickupSettings;


        private bool groundPickup;
        private bool stockpile;
        private bool buildingStorage;
        private bool allowForbidden;
        private bool allowBelt;

        public ITab_SmartHopper()
        {
            size = WinSize;
            labelKey = "PRF.SmartHopper.ITab.Name";
        }

        protected override void FillTab()
        {
            var list = new Listing_Standard();
            var rect = new Rect(0f, 0f, WinSize.x-20f, WinSize.y).ContractedBy(10f);
            list.Begin(rect);

            groundPickup = Hopper.AllowGroundPickup;
            stockpile = Hopper.AllowStockpilePickup;
            buildingStorage = Hopper.AllowStoragePickup;
            allowForbidden = Hopper.AllowForbiddenPickup;
            allowBelt = Hopper.AllowBeltPickup;


            list.Label("PRF.SmartHopper.ITab.Description".Translate());
            rect = list.GetRect(30);
            Widgets.CheckboxLabeled(rect, "PRF.SmartHopper.ITab.AllowGround".Translate(), ref groundPickup);
            rect = list.GetRect(30);
            Widgets.CheckboxLabeled(rect, "PRF.SmartHopper.ITab.AllowStockpile".Translate(), ref stockpile);
            rect = list.GetRect(30);
            Widgets.CheckboxLabeled(rect, "PRF.SmartHopper.ITab.AllowStorage".Translate(), ref buildingStorage);
            rect = list.GetRect(30);
            Widgets.CheckboxLabeled(rect, "PRF.SmartHopper.ITab.AllowForbidden".Translate(), ref allowForbidden);
            rect = list.GetRect(30);
            Widgets.CheckboxLabeled(rect, "PRF.SmartHopper.ITab.AllowBelt".Translate(), ref allowBelt);

            Hopper.AllowGroundPickup = groundPickup;
            Hopper.AllowStockpilePickup = stockpile;
            Hopper.AllowStoragePickup = buildingStorage;
            Hopper.AllowForbiddenPickup = allowForbidden;
            Hopper.AllowBeltPickup = allowBelt;

            list.End();
            
        }
    }
}
