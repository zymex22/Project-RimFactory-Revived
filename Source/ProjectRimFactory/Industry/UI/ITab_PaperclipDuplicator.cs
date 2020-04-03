using ProjectRimFactory.Common;
using ProjectRimFactory.Storage;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public Building_PaperclipDuplicator SelBuilding => (Building_PaperclipDuplicator)SelThing;
        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Listing_Standard listing = new Listing_Standard(GameFont.Small);
            listing.Begin(rect);
            listing.Label(SelBuilding.LabelCap);
            if (listing.ButtonTextLabeled("PRFBoundStorageBuilding".Translate(), SelBuilding.boundStorageUnit?.LabelCap ?? "NoneBrackets".Translate()))
            {
                List<FloatMenuOption> list = (from Building_MassStorageUnit b in Find.CurrentMap.listerBuildings.AllBuildingsColonistOfClass<Building_MassStorageUnit>()
                                              select new FloatMenuOption(b.LabelCap, () => SelBuilding.boundStorageUnit = b)).ToList();
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
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>()
                {
                    new FloatMenuOption("PRFDeposit".Translate(), () =>
                    {
                        deposit = true;
                    }),
                    new FloatMenuOption("PRFWithdraw".Translate(), () =>
                    {
                        deposit = false;
                    })
                }));
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
            if (SelBuilding.boundStorageUnit != null)
            {
                if (SelBuilding.boundStorageUnit.CanReceiveIO)
                {
                    if (int.TryParse(amountTextArea, out int result))
                    {
                        if (deposit)
                        {
                            List<ThingCount> selected = new List<ThingCount>();
                            int current = 0;
                            foreach (Thing item in SelBuilding.boundStorageUnit.StoredItems.ToList())
                            {
                                if (item.def == PRFDefOf.Paperclip)
                                {
                                    int num = Math.Min(result - current, item.stackCount);
                                    selected.Add(new ThingCount(item, num));
                                    current += num;
                                }
                                if (current == result)
                                {
                                    break;
                                }
                            }
                            if (current == result)
                            {
                                SelBuilding.DepositPaperclips(result);
                                for (int i = 0; i < selected.Count; i++)
                                {
                                    selected[i].Thing.SplitOff(selected[i].Count);
                                }
                                Messages.Message("SuccessfullyDepositedPaperclips".Translate(result), MessageTypeDefOf.PositiveEvent);
                            }
                            else
                            {
                                Messages.Message("PRFNotEnoughPaperclips".Translate(), MessageTypeDefOf.RejectInput);
                            }
                        }
                        else
                        {
                            if (result < SelBuilding.PaperclipsActual)
                            {
                                List<Thing> output = new List<Thing>();
                                int current = 0;
                                while (current < result)
                                {
                                    int num = Math.Min(result - current, PRFDefOf.Paperclip.stackLimit);
                                    Thing paperclip = ThingMaker.MakeThing(PRFDefOf.Paperclip);
                                    paperclip.stackCount = num;
                                    output.Add(paperclip);
                                    current += num;
                                }
                                for (int i = 0; i < output.Count; i++)
                                {
                                    GenPlace.TryPlaceThing(output[i], SelBuilding.Position, SelBuilding.Map, ThingPlaceMode.Direct);
                                    SelBuilding.boundStorageUnit.Notify_ReceivedThing(output[i]);
                                }
                                SelBuilding.WithdrawPaperclips(result);
                                Messages.Message("SuccessfullyWithdrawnPaperclips".Translate(result), MessageTypeDefOf.PositiveEvent);
                            }
                            else
                            {
                                Messages.Message("PRFNotEnoughPaperclips".Translate(), MessageTypeDefOf.RejectInput);
                            }
                        }
                    }
                    else
                    {
                        Messages.Message("PRFInputInvalid".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("PRFBoundStorageUnitNotAvailableForIO".Translate(), MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                Messages.Message("PRFNoBoundStorageUnit".Translate(), MessageTypeDefOf.RejectInput);
            }
        }

        bool deposit;
        string amountTextArea;
    }
}
