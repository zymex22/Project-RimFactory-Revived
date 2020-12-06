using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProjectRimFactory.Storage.UI
{
    // Somebody toucha my spaghet code
    public class ITab_Items : ITab
    {
        private Vector2 scrollPos;
        private float scrollViewHeight;
        private string searchQuery;

        public ITab_Items()
        {
            size = new Vector2(480f, 480f);
            labelKey = "PRFItemsTab";
        }

        public Building_MassStorageUnit SelectedMassStorageUnit =>
            IOPortSelected ? SelectedIOPort.BoundStorageUnit : (Building_MassStorageUnit) SelThing;

        public override bool IsVisible =>
            IOPortSelected ? SelectedIOPort.BoundStorageUnit?.CanReceiveIO ?? false : true;

        protected bool IOPortSelected => SelThing is Building_StorageUnitIOPort;

        protected Building_StorageUnitIOPort SelectedIOPort => SelThing as Building_StorageUnitIOPort;

        protected override void FillTab()
        {
            Text.Font = GameFont.Small;
            var curY = 0f;
            var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Widgets.ListSeparator(ref curY, rect.width, "Content");
            curY += 5f;
            var selected = from Thing t in SelectedMassStorageUnit.StoredItems
                where string.IsNullOrEmpty(searchQuery) || t.Label.ToLower().Contains(searchQuery.ToLower())
                select t;
            var text = SelectedMassStorageUnit.GetITabString(Math.Min(500, selected.Count()));
            var MainTabText = new Rect(8f, curY, rect.width - 16f, Text.CalcHeight(text, rect.width - 16f));
            Widgets.Label(MainTabText, text);
            curY += MainTabText.height;
            searchQuery = Widgets.TextArea(new Rect(rect.x, curY, rect.width, 25f),
                searchQuery ?? string.Empty);
            
            rect.y += MainTabText.height;
            var position = new Rect(rect);
            GUI.BeginGroup(position);
            GUI.color = Color.white;
            var outRect = new Rect(0f, 60f, position.width, position.height - 60f);
            var viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
            curY = 0f;
            foreach (var thing in selected.Take(500)) DrawThingRow(ref curY, viewRect.width, thing);
            if (Event.current.type == EventType.Layout) scrollViewHeight = curY + 30f;
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        // Attempt at mimicking LWM Deep Storage
        // Credits to LWM Deep Storage :)
        private void DrawThingRow(ref float y, float width, Thing thing)
        {
            width -= 24f;
            // row to hold the item in the GUI
            Widgets.InfoCardButton(width, y, thing);
            // rect.width -= 84f;
            width -= 24f;
            
            var checkmarkRect = new Rect(width, y, 24f, 24f);
            var isItemForbidden = !thing.IsForbidden(Faction.OfPlayer);
            var forbidRowItem = isItemForbidden;
            if (isItemForbidden)
            {
                TooltipHandler.TipRegion(checkmarkRect, "CommandNotForbiddenDesc".Translate());
            }
            else
            {
                TooltipHandler.TipRegion(checkmarkRect, "CommandForbiddenDesc".Translate());
            }
            Widgets.Checkbox(checkmarkRect.x, checkmarkRect.y, ref isItemForbidden);
            if (isItemForbidden != forbidRowItem) thing.SetForbidden(!isItemForbidden, false);
            var thingRow = new Rect(0f, y, width, 28f);
            // Highlights the row upon mousing over
            if (Mouse.IsOver(thingRow))
            {
                GUI.color = ITab_Pawn_Gear.HighlightColor;
                GUI.DrawTexture(thingRow, TexUI.HighlightTex);
            }
            // Draws the icon of the thingDef in the row
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing);
            // Draws the item name + info
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ITab_Pawn_Gear.ThingLabelColor;
            var itemName = new Rect(36f, y, thingRow.width - 36f, thingRow.height);
            Text.WordWrap = false;
            // LabelCap is interesting to me(rider)
            // Really useful I would think
            // LabelCap == "Wort x75"
            Widgets.Label(itemName, thing.LabelCap.Truncate(itemName.width));
            Text.WordWrap = true;
            
            // For the toolpit
            var text2 = thing.LabelCap;
            
            // if uses hitpoints draw it
            if (thing.def.useHitPoints)
                text2 = string.Concat(thing.LabelCap, "\n", thing.HitPoints, " / ", thing.MaxHitPoints);
            
            // Custom rightclick menu
            TooltipHandler.TipRegion(thingRow, text2);
            if (GUI.Button(thingRow, "", Widgets.EmptyStyle))
            {
                // if right click
                if (Event.current.button == 1)
                {
                    // create a menu with all of the pawn names
                    var opts = new List<FloatMenuOption>();
                    foreach (var p in from Pawn col in thing.Map.mapPawns.FreeColonists
                        where col.IsColonistPlayerControlled && !col.Dead && col.Spawned && !col.Downed
                        select col)
                    {
                        var choices = ChoicesForThing(thing, p);
                        if (ChoicesForThing(thing, p).Count > 0)
                            opts.Add(new FloatMenuOption(p.Name.ToStringFull,
                                () => { Find.WindowStack.Add(new FloatMenu(choices)); }));   
                    }

                    // Output to IOPort, otherwise drop to ground
                    if (IOPortSelected)
                        opts.Add(new FloatMenuOption("PRFOutputItems".Translate(), () => SelectedMassStorageUnit
                            .StoredItems.Where(i => i == thing)
                            .ToList().ForEach(t => SelectedIOPort.OutputItem(t))));
                    else
                        opts.Add( new FloatMenuOption("PRFOutputItems".Translate(), () => SelectedMassStorageUnit
                            .StoredItems.Where(i => i == thing)
                            .ToList().ForEach(t => SelectedMassStorageUnit.OutputItem(t))));
                    
                    // No idea how to stretch the options window so we're just not going to add a title to the menu
                    // Find.WindowStack.Add(new FloatMenu(opts, "PRFMassStorageRightClickSelectPawn".Translate()));
                    Find.WindowStack.Add(new FloatMenu(opts));
                }
                else
                {
                    Find.Selector.ClearSelection();
                    Find.Selector.Select(thing);
                }
            }

            y += 28f;
        }

        // Decompiled code is painful to read... Continue at your own risk
        // TODO: Replace this with a cleaner solution
        // Maybe explore FloatMenuMakerMap
        public static List<FloatMenuOption> ChoicesForThing(Thing thing, Pawn pawn)
        {
            var opts = new List<FloatMenuOption>();
            var t = thing;


            // Copied from FloatMenuMakerMap.AddHumanlikeOrders
            if (t.def.ingestible != null && pawn.RaceProps.CanEverEat(t) && t.IngestibleNow)
            {
                string text;
                if (t.def.ingestible.ingestCommandString.NullOrEmpty())
                    text = "ConsumeThing".Translate(t.LabelShort, t);
                else
                    text = string.Format(t.def.ingestible.ingestCommandString, t.LabelShort);
                if (!t.IsSociallyProper(pawn)) text = text + " (" + "ReservedForPrisoners".Translate() + ")";
                FloatMenuOption item7;
                if (t.def.IsNonMedicalDrug && pawn.IsTeetotaler())
                {
                    item7 = new FloatMenuOption(text + " (" + TraitDefOf.DrugDesire.DataAtDegree(-1).label + ")", null);
                }
                else if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.Deadly))
                {
                    item7 = new FloatMenuOption(text + " (" + "NoPath".Translate() + ")", null);
                }
                else
                {
                    var priority2 = !(t is Corpse) ? MenuOptionPriority.Default : MenuOptionPriority.Low;
                    item7 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
                    {
                        t.SetForbidden(false);
                        var job = new Job(JobDefOf.Ingest, t);
                        job.count = FoodUtility.WillIngestStackCountOf(pawn, t.def,
                            t.GetStatValue(StatDefOf.Nutrition));
                        pawn.jobs.TryTakeOrderedJob(job);
                    }, priority2), pawn, t);
                }

                opts.Add(item7);
            }


            // Add equipment commands
            // Copied from FloatMenuMakerMap.AddHumanlikeOrders
            if (thing is ThingWithComps equipment && equipment.GetComp<CompEquippable>() != null)
            {
                var labelShort = equipment.LabelShort;
                FloatMenuOption item4;
                if (equipment.def.IsWeapon && pawn.WorkTagIsDisabled(WorkTags.Violent))
                {
                    item4 = new FloatMenuOption(
                        "CannotEquip".Translate(labelShort) + " (" +
                        "IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn) + ")", null);
                }
                else if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    item4 = new FloatMenuOption("CannotEquip".Translate(labelShort) + " (" + "NoPath".Translate() + ")",
                        null);
                }
                else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    item4 = new FloatMenuOption(
                        "CannotEquip".Translate(labelShort) + " (" + "Incapable".Translate() + ")", null);
                }
                else
                {
                    string text5 = "Equip".Translate(labelShort);
                    if (equipment.def.IsRangedWeapon && pawn.story != null &&
                        pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                        text5 = text5 + " " + "EquipWarningBrawler".Translate();
                    item4 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text5, delegate
                    {
                        equipment.SetForbidden(false);
                        pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.Equip, equipment));
                        MoteMaker.MakeStaticMote(equipment.DrawPos, equipment.Map, ThingDefOf.Mote_FeedbackEquip);
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons,
                            KnowledgeAmount.Total);
                    }, MenuOptionPriority.High), pawn, equipment);
                }

                opts.Add(item4);
            }

            // Add clothing commands
            var apparel = thing as Apparel;
            if (apparel != null)
            {
                FloatMenuOption item5;
                if (!pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly))
                    item5 = new FloatMenuOption(
                        "CannotWear".Translate(apparel.Label, apparel) + " (" + "NoPath".Translate() + ")", null);
                else if (!ApparelUtility.HasPartsToWear(pawn, apparel.def))
                    item5 = new FloatMenuOption(
                        "CannotWear".Translate(apparel.Label, apparel) + " (" +
                        "CannotWearBecauseOfMissingBodyParts".Translate() + ")", null);
                else
                    item5 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                        "ForceWear".Translate(apparel.LabelShort, apparel), delegate
                        {
                            apparel.SetForbidden(false);
                            var job = new Job(JobDefOf.Wear, apparel);
                            pawn.jobs.TryTakeOrderedJob(job);
                        }, MenuOptionPriority.High), pawn, apparel);
                opts.Add(item5);
            }

            // Add caravan commands

            if (pawn.IsFormingCaravan())
                if (thing != null && thing.def.EverHaulable)
                {
                    var packTarget = GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace(pawn) ?? pawn;
                    var jobDef = packTarget != pawn ? JobDefOf.GiveToPackAnimal : JobDefOf.TakeInventory;
                    if (!pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        opts.Add(new FloatMenuOption(
                            "CannotLoadIntoCaravan".Translate(thing.Label, thing) + " (" + "NoPath".Translate() + ")",
                            null));
                    }
                    else if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, thing, 1))
                    {
                        opts.Add(new FloatMenuOption(
                            "CannotLoadIntoCaravan".Translate(thing.Label, thing) + " (" + "TooHeavy".Translate() + ")",
                            null));
                    }
                    else
                    {
                        var lordJob = (LordJob_FormAndSendCaravan) pawn.GetLord().LordJob;
                        var capacityLeft = CaravanFormingUtility.CapacityLeft(lordJob);
                        if (thing.stackCount == 1)
                        {
                            var capacityLeft4 = capacityLeft - thing.GetStatValue(StatDefOf.Mass);
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                                CaravanFormingUtility.AppendOverweightInfo(
                                    "LoadIntoCaravan".Translate(thing.Label, thing), capacityLeft4), delegate
                                {
                                    thing.SetForbidden(false, false);
                                    var job = new Job(jobDef, thing);
                                    job.count = 1;
                                    job.checkEncumbrance = packTarget == pawn;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                }, MenuOptionPriority.High), pawn, thing));
                        }
                        else
                        {
                            if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, thing, thing.stackCount))
                            {
                                opts.Add(new FloatMenuOption(
                                    "CannotLoadIntoCaravanAll".Translate(thing.Label, thing) + " (" +
                                    "TooHeavy".Translate() + ")", null));
                            }
                            else
                            {
                                var capacityLeft2 =
                                    capacityLeft - thing.stackCount * thing.GetStatValue(StatDefOf.Mass);
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                                    CaravanFormingUtility.AppendOverweightInfo(
                                        "LoadIntoCaravanAll".Translate(thing.Label, thing), capacityLeft2), delegate
                                    {
                                        thing.SetForbidden(false, false);
                                        var job = new Job(jobDef, thing);
                                        job.count = thing.stackCount;
                                        job.checkEncumbrance = packTarget == pawn;
                                        pawn.jobs.TryTakeOrderedJob(job);
                                    }, MenuOptionPriority.High), pawn, thing));
                            }

                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                                "LoadIntoCaravanSome".Translate(thing.LabelNoCount, thing), delegate
                                {
                                    var to = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(packTarget, thing),
                                        thing.stackCount);
                                    var window = new Dialog_Slider(delegate(int val)
                                    {
                                        var capacityLeft3 = capacityLeft - val * thing.GetStatValue(StatDefOf.Mass);
                                        return CaravanFormingUtility.AppendOverweightInfo(
                                            string.Format("LoadIntoCaravanCount".Translate(thing.LabelNoCount, thing),
                                                val), capacityLeft3);
                                    }, 1, to, delegate(int count)
                                    {
                                        thing.SetForbidden(false, false);
                                        var job = new Job(jobDef, thing);
                                        job.count = count;
                                        job.checkEncumbrance = packTarget == pawn;
                                        pawn.jobs.TryTakeOrderedJob(job);
                                    });
                                    Find.WindowStack.Add(window);
                                }, MenuOptionPriority.High), pawn, thing));
                        }
                    }
                }
            return opts;
        }
    }
}