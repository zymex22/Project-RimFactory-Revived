using ProjectRimFactory.Common;
using ProjectRimFactory.Storage;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Industry.UI
{
    public class ITab_PaperclipDuplicator : ITab
    {
        public ITab_PaperclipDuplicator()
        {
            size = new Vector2(400f, 250f);
            labelKey = "PRFPaperclipDuplicatorTab";
        }

        private Building_PaperclipDuplicator SelBuilding => (Building_PaperclipDuplicator)SelThing;
        
        protected override void FillTab()
        {
            var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            var listing = new Listing_Standard(GameFont.Small);
            listing.Begin(rect);
            listing.Label(SelBuilding.LabelCap);
            if (listing.ButtonTextLabeled("PRFBoundStorageBuilding".Translate(), SelBuilding.BoundStorageUnit?.LabelCap ?? "NoneBrackets".Translate()))
            {
                var list = (from Building_MassStorageUnit b in Find.CurrentMap.listerBuildings.AllBuildingsColonistOfClass<Building_MassStorageUnit>()
                                              select new FloatMenuOption(b.LabelCap, () => SelBuilding.BoundStorageUnit = b)).ToList();
                if (list.Count == 0)
                {
                    list.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }
            listing.GapLine();
            listing.Label("PRFDepositWithdraw".Translate());
            if (listing.ButtonTextLabeled("PRFDepositWithdrawMode".Translate(), (deposit ? "PRFDeposit" : "PRFWithdraw").Translate()))
            {
                Find.WindowStack.Add(new FloatMenu([
                    new FloatMenuOption("PRFDeposit".Translate(), () => { deposit = true; }),
                    new FloatMenuOption("PRFWithdraw".Translate(), () => { deposit = false; })
                ]));
            }
            amountTextArea = listing.TextEntryLabeled("PRFAmount".Translate(), amountTextArea);
            if (listing.ButtonText("PRFDepositWithdraw".Translate()))
            {
                HandleDepositWithdrawRequest();
            }
            listing.End();
        }

        private void HandleDepositWithdrawRequest()
        {
            if (SelBuilding.BoundStorageUnit == null)
            {
                Messages.Message("PRFNoBoundStorageUnit".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            if (!SelBuilding.BoundStorageUnit.CanReceiveIO)
            {
                Messages.Message("PRFBoundStorageUnitNotAvailableForIO".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            if (!int.TryParse(amountTextArea, out var selectedAmount))
            {
                Messages.Message("PRFInputInvalid".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            if (deposit)
            {
                var selected = new List<ThingCount>();
                var current = 0;
                foreach (var item in SelBuilding.BoundStorageUnit.StoredItems.ToList())
                {
                    if (item.def == PRFDefOf.Paperclip)
                    {
                        var num = Math.Min(selectedAmount - current, item.stackCount);
                        selected.Add(new ThingCount(item, num));
                        current += num;
                    }

                    if (current == selectedAmount)
                    {
                        break;
                    }
                }

                if (current == selectedAmount)
                {
                    SelBuilding.DepositPaperclips(selectedAmount);
                    for (var i = 0; i < selected.Count; i++)
                    {
                        selected[i].Thing.SplitOff(selected[i].Count);
                    }

                    Messages.Message("SuccessfullyDepositedPaperclips".Translate(selectedAmount), MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message("PRFNotEnoughPaperclips".Translate(), MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                if (selectedAmount >= SelBuilding.PaperclipsActual)
                {
                    Messages.Message("PRFNotEnoughPaperclips".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }

                var output = new List<Thing>();
                var current = 0;
                while (current < selectedAmount)
                {
                    var num = Math.Min(selectedAmount - current, PRFDefOf.Paperclip.stackLimit);
                    var paperclip = ThingMaker.MakeThing(PRFDefOf.Paperclip);
                    paperclip.stackCount = num;
                    output.Add(paperclip);
                    current += num;
                }

                for (var i = 0; i < output.Count; i++)
                {
                    GenPlace.TryPlaceThing(output[i], SelBuilding.Position, SelBuilding.Map, ThingPlaceMode.Direct);
                    SelBuilding.BoundStorageUnit.Notify_ReceivedThing(output[i]);
                }

                SelBuilding.WithdrawPaperclips(selectedAmount);
                Messages.Message("SuccessfullyWithdrawnPaperclips".Translate(selectedAmount),
                    MessageTypeDefOf.PositiveEvent);
            }
        }

        private bool deposit;
        private string amountTextArea;
    }
}
