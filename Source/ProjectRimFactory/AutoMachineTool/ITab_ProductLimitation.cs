using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using ProjectRimFactory.Common;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    interface IProductLimitation
    {
        int ProductLimitCount { get; set; }
        bool ProductLimitation { get; set; }
        bool CountStacks { get; set; }
        Option<SlotGroup> TargetSlotGroup { get; set; }
    }

    class ITab_ProductLimitation : ITab
    {
//        private static readonly Vector2 WinSize = new Vector2(400f, 270f);
        private Vector2 winSize = new Vector2(400f, 0f);

        public ITab_ProductLimitation()
        {
            this.labelKey = "PRF.AutoMachineTool.ProductLimitation.TabName";
        }
        
        public IProductLimitation Machine
        {
            get => this.SelThing as IProductLimitation;
        }

        private PRF_Building PRFB { get => this.SelThing as PRF_Building; }

        protected override void UpdateSize() {
            winSize.y = 0;
            if (Machine != null) winSize.y = 270f;
            if (PRFB != null) winSize.y += 100f;
            this.size = winSize;
            base.UpdateSize();
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if (Machine != null) {
                this.groups = Find.CurrentMap.haulDestinationManager.AllGroups.ToList();
                this.Machine.TargetSlotGroup = this.Machine.TargetSlotGroup.Where(s => this.groups.Contains(s));
            }
        }

        private List<SlotGroup> groups;

        protected override void FillTab()
        {
            Listing_Standard list = new Listing_Standard();
            Rect inRect = new Rect(0f, 0f, winSize.x, winSize.y).ContractedBy(10f);

            list.Begin(inRect);
            list.Gap(24);
            if (PRFB!=null) {
                var description = "PRF.Common.OutputToStockpileDesc".Translate();
                var label = "PRF.Common.OutputToStockpile".Translate();
                bool tmpB = PRFB.OutputToEntireStockpile;
                list.CheckboxLabeled(label, ref tmpB, description);
                if (tmpB != PRFB.OutputToEntireStockpile)
                    PRFB.OutputToEntireStockpile = tmpB;
                if (Machine != null) list.GapLine();
            }
            if (Machine != null) {
                var description = "PRF.AutoMachineTool.ProductLimitation.Description".Translate();
                var label = "PRF.AutoMachineTool.ProductLimitation.ValueLabel".Translate();
                var checkBoxLabel = "PRF.AutoMachineTool.ProductLimitation.CheckBoxLabel".Translate();
                var stackCountLabel = "PRF.AutoMachineTool.ProductLimitation.CountStacks".Translate();


                var rect = list.GetRect(70f);
                Widgets.Label(rect, description);
                list.Gap();

                rect = list.GetRect(30f);
                bool limitation = this.Machine.ProductLimitation;
                Widgets.CheckboxLabeled(rect, checkBoxLabel, ref limitation);
                this.Machine.ProductLimitation = limitation;
                list.Gap();

                rect = list.GetRect(30f);
                string buf = this.Machine.ProductLimitCount.ToString();
                int limit = this.Machine.ProductLimitCount;
                Widgets.Label(rect.LeftHalf(), label);
                Widgets.TextFieldNumeric<int>(rect.RightHalf(), ref limit, ref buf, 1, 1000000);
                list.Gap();

                rect = list.GetRect(30f);
                bool countStacks = this.Machine.CountStacks;
                Widgets.CheckboxLabeled(rect, stackCountLabel, ref countStacks);
                this.Machine.CountStacks = countStacks;
                list.Gap();

                rect = list.GetRect(30f);
                Widgets.Label(rect.LeftHalf(), "PRF.AutoMachineTool.CountZone".Translate());
                if (Widgets.ButtonText(rect.RightHalf(), this.Machine.TargetSlotGroup.Fold("PRF.AutoMachineTool.EntierMap".Translate())(s => s.parent.SlotYielderLabel()))) {
                    Find.WindowStack.Add(new FloatMenu(groups
                        .Select(g => new FloatMenuOption(g.parent.SlotYielderLabel(), () => this.Machine.TargetSlotGroup = Option(g)))
                        .ToList()
                        .Head(new FloatMenuOption("PRF.AutoMachineTool.EntierMap".Translate(), () => this.Machine.TargetSlotGroup = Nothing<SlotGroup>()))));
                }
                this.Machine.ProductLimitCount = limit;
            }

            list.Gap();

            list.End();

        }
    }
}
