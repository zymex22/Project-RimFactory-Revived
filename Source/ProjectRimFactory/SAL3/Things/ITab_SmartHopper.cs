using ProjectRimFactory.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Things
{
    public class ITab_SmartHopper : ITab
    {
        
        private static readonly Vector2 WinSize = new Vector2(200f, 200f);

        private IPickupSettings hopper => this.SelThing as IPickupSettings;


        private bool GroundPickup = false;
        private bool Stockpile = false;
        private bool BuildingStorage = false;
        private bool Allowforbidden = false;
        private bool AllowBelt = false;

        public ITab_SmartHopper()
        {
            this.size = WinSize;
            this.labelKey = "PRF.SmartHopper.ITab.Name";
        }

        protected override void FillTab()
        {
            Listing_Standard list = new Listing_Standard();
            Rect rect = new Rect(0f, 0f, WinSize.x-20f, WinSize.y).ContractedBy(10f);
            list.Begin(rect);

            GroundPickup = hopper.AllowGroundPickup;
            Stockpile = hopper.AllowStockpilePickup;
            BuildingStorage = hopper.AllowStoragePickup;
            Allowforbidden = hopper.AllowForbiddenPickup;
            AllowBelt = hopper.AllowBeltPickup;


            list.Label("PRF.SmartHopper.ITab.Description".Translate());
            rect = list.GetRect(30);
            Widgets.CheckboxLabeled(rect, "PRF.SmartHopper.ITab.AllowGround".Translate(), ref GroundPickup);
            rect = list.GetRect(30);
            Widgets.CheckboxLabeled(rect, "PRF.SmartHopper.ITab.AllowStockpile".Translate(), ref Stockpile);
            rect = list.GetRect(30);
            Widgets.CheckboxLabeled(rect, "PRF.SmartHopper.ITab.AllowStorage".Translate(), ref BuildingStorage);
            rect = list.GetRect(30);
            Widgets.CheckboxLabeled(rect, "PRF.SmartHopper.ITab.AllowForbidden".Translate(), ref Allowforbidden);
            rect = list.GetRect(30);
            Widgets.CheckboxLabeled(rect, "PRF.SmartHopper.ITab.AllowBelt".Translate(), ref AllowBelt);

            hopper.AllowGroundPickup = GroundPickup;
            hopper.AllowStockpilePickup = Stockpile;
            hopper.AllowStoragePickup = BuildingStorage;
            hopper.AllowForbiddenPickup = Allowforbidden;
            hopper.AllowBeltPickup = AllowBelt;

            list.End();
            
        }
    }
}
