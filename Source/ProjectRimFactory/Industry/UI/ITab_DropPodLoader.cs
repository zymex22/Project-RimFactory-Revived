using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;
using ProjectRimFactory.Common;

namespace ProjectRimFactory.Industry.UI
{
    class ITab_DropPodLoader : ITab
    {

        private Building_DropPodLoader Machine => (this.SelThing as Building_DropPodLoader);
        private static readonly Vector2 WinSize = new Vector2(600f, 250f);

        public ITab_DropPodLoader()
        {
            this.size = WinSize;
            this.labelKey = "DropPodSettings";

        }

        

        protected override void FillTab()
        {
            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(10f);

            list.Begin(inRect);

            list.Gap();

            if (list.ButtonText("configure Target"))
            {
                Machine.Map.GetComponent<PRFMapComponent>().RegisterCompLaunchableSelectTarget(Machine.TransportPodPosActual, Machine);
                Machine.ChoosingDestination();
                
            }
            list.Gap();
            list.Label("Target Tile: " + Machine.DestinationTile);
            list.Gap();
            list.Label("Target Cell: " + Machine.DestinationCell);


            list.End();

        }
    }
}
