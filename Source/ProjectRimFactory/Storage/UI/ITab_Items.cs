using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;
using RimWorld.Planet;
using Verse.AI.Group;

namespace ProjectRimFactory.Storage.UI
{
    // Somebody toucha my spaghet code
    public class ITab_Items : ITab
    {
        public ITab_Items()
        {
            size = new Vector2(480f, 480f);
            labelKey = "PRFItemsTab";
        }
        public Building_MassStorageUnit SelBuilding => (Building_MassStorageUnit)SelThing;
        protected override void FillTab()
        {
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            IEnumerable<Thing> selected = from Thing t in SelBuilding.StoredItems
                                          where string.IsNullOrEmpty(searchQuery) || t.Label.ToLower().Contains(searchQuery.ToLower())
                                          select t;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 25f), SelBuilding.GetITabString(Math.Min(500, selected.Count())));
            searchQuery = Widgets.TextArea(new Rect(rect.x, rect.y + 25f, rect.width, 25f), searchQuery ?? string.Empty, false);
            Rect position = new Rect(rect);
            GUI.BeginGroup(position);
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 60f, position.width, position.height - 60f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
            float curY = 0;
            foreach (Thing thing in selected.Take(500))
            {
                DrawThingRow(ref curY, viewRect.width, thing);
            }
            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = curY + 30f;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        private void DrawThingRow(ref float y, float width, Thing thing)
        {
            Rect rect = new Rect(0f, y, width, 28f);
            Widgets.InfoCardButton(rect.width - 24f, y, thing);
            rect.width -= 84f;
            if (Mouse.IsOver(rect))
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing, 1f);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            Rect rect5 = new Rect(36f, y, rect.width - 36f, rect.height);
            Text.WordWrap = false;
            Widgets.Label(rect5, thing.LabelCap.Truncate(rect5.width, null));
            Text.WordWrap = true;
            string text2 = thing.LabelCap;
            if (thing.def.useHitPoints)
            {
                text2 = string.Concat(new object[]
                {
                    thing.LabelCap,
                    "\n",
                    thing.HitPoints,
                    " / ",
                    thing.MaxHitPoints
                });
            }
            TooltipHandler.TipRegion(rect, text2);
            if (GUI.Button(rect, "", Widgets.EmptyStyle))
            {
                if (Event.current.button == 1)
                {
                    List<FloatMenuOption> opts = new List<FloatMenuOption>()
                    {
                        new FloatMenuOption("PRFMassStorageRightClickSelectPawn".Translate(), null) { Disabled = true }
                    };
                    foreach (Pawn p in from Pawn col in thing.Map.mapPawns.FreeColonists
                                       where col.IsColonistPlayerControlled && !col.Dead && col.Spawned && !col.Downed
                                       select col)
                    {
                        opts.Add(new FloatMenuOption(p.Name.ToStringShort, () =>
                        {
                            Find.WindowStack.Add(new FloatMenu(ChoicesForThing(thing, p)));
                        }));
                    }
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
        public static List<FloatMenuOption> ChoicesForThing(Thing thing, Pawn pawn)
        {
            List<FloatMenuOption> opts = new List<FloatMenuOption>();
            Thing t = thing;
            

            // Copied from FloatMenuMakerMap.AddHumanlikeOrders
            if (t.def.ingestible != null && pawn.RaceProps.CanEverEat(t) && t.IngestibleNow)
            {
                string text;
                if (t.def.ingestible.ingestCommandString.NullOrEmpty())
                {
                    text = "ConsumeThing".Translate(new NamedArgument[]
                    {
                        t.LabelShort
                    });
                    
                }
                else
                {
                    text = string.Format(t.def.ingestible.ingestCommandString, t.LabelShort);
                }
                if (!t.IsSociallyProper(pawn))
                {
                    text = text + " (" + "ReservedForPrisoners".Translate() + ")";
                }
                FloatMenuOption item7;
                if (t.def.IsNonMedicalDrug && pawn.IsTeetotaler())
                {
                    item7 = new FloatMenuOption(text + " (" + TraitDefOf.DrugDesire.DataAtDegree(-1).label + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                }
                else if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    item7 = new FloatMenuOption(text + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                }
                else
                {
                    MenuOptionPriority priority2 = (!(t is Corpse)) ? MenuOptionPriority.Default : MenuOptionPriority.Low;
                    item7 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate ()
                    {
                        t.SetForbidden(false, true);
                        Job job = new Job(JobDefOf.Ingest, t);
                        job.count = FoodUtility.WillIngestStackCountOf(pawn, t.def, t.GetStatValue(StatDefOf.Nutrition, true));
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }, priority2, null, null, 0f, null, null), pawn, t, "ReservedBy");
                }
                opts.Add(item7);
            }
            

            // Add equipment commands
            // Copied from FloatMenuMakerMap.AddHumanlikeOrders
            if (thing is ThingWithComps equipment && equipment.GetComp<CompEquippable>() != null)
            {
                string labelShort = equipment.LabelShort;
                FloatMenuOption item4;
                if (equipment.def.IsWeapon && pawn.WorkTagIsDisabled(WorkTags.Violent))
                {
                    item4 = new FloatMenuOption("CannotEquip".Translate(new NamedArgument[]
                    {
                            labelShort
                    }) + " (" + "IsIncapableOfViolenceLower".Translate(new NamedArgument[]
                    {
                            pawn.LabelShort
                    }) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                }
                else if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    item4 = new FloatMenuOption("CannotEquip".Translate(new NamedArgument[]
                    {
                            labelShort
                    }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                }
                else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    item4 = new FloatMenuOption("CannotEquip".Translate(new NamedArgument[]
                    {
                            labelShort
                    }) + " (" + "Incapable".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                }
                else
                {
                    string text5 = "Equip".Translate(new NamedArgument[]
                    {
                            labelShort
                    });
                    if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                    {
                        text5 = text5 + " " + "EquipWarningBrawler".Translate();
                    }
                    item4 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text5, delegate ()
                    {
                        equipment.SetForbidden(false, true);
                        pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.Equip, equipment), JobTag.Misc);
                        MoteMaker.MakeStaticMote(equipment.DrawPos, equipment.Map, ThingDefOf.Mote_FeedbackEquip, 1f);
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                    }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, equipment, "ReservedBy");
                }
                opts.Add(item4);
            }
            
            // Add clothing commands
            Apparel apparel = thing as Apparel;
            if (apparel != null)
            {
                FloatMenuOption item5;
                if (!pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    item5 = new FloatMenuOption("CannotWear".Translate(new NamedArgument[]
                    {
                            apparel.Label
                    }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                }
                else if (!ApparelUtility.HasPartsToWear(pawn, apparel.def))
                {
                    item5 = new FloatMenuOption("CannotWear".Translate(new NamedArgument[]
                    {
                            apparel.Label
                    }) + " (" + "CannotWearBecauseOfMissingBodyParts".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                }
                else
                {
                    item5 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("ForceWear".Translate(new NamedArgument[]
                    {
                            apparel.LabelShort
                    }), delegate ()
                    {
                        apparel.SetForbidden(false, true);
                        Job job = new Job(JobDefOf.Wear, apparel);
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, apparel, "ReservedBy");
                }
                opts.Add(item5);
            }

            // Add caravan commands

            if (pawn.IsFormingCaravan())
            {
                if (thing != null && thing.def.EverHaulable)
                {
                    Pawn packTarget = GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace(pawn) ?? pawn;
                    JobDef jobDef = (packTarget != pawn) ? JobDefOf.GiveToPackAnimal : JobDefOf.TakeInventory;
                    if (!pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                    {
                        opts.Add(new FloatMenuOption("CannotLoadIntoCaravan".Translate(new NamedArgument[]
                        {
                    thing.Label
                        }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    else if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, thing, 1))
                    {
                        opts.Add(new FloatMenuOption("CannotLoadIntoCaravan".Translate(new NamedArgument[]
                        {
                    thing.Label
                        }) + " (" + "TooHeavy".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    else
                    {
                        LordJob_FormAndSendCaravan lordJob = (LordJob_FormAndSendCaravan)pawn.GetLord().LordJob;
                        float capacityLeft = CaravanFormingUtility.CapacityLeft(lordJob);
                        if (thing.stackCount == 1)
                        {
                            float capacityLeft4 = capacityLeft - thing.GetStatValue(StatDefOf.Mass, true);
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravan".Translate(new NamedArgument[]
                            {
                        thing.Label
                            }), capacityLeft4), delegate ()
                            {
                                thing.SetForbidden(false, false);
                                Job job = new Job(jobDef, thing);
                                job.count = 1;
                                job.checkEncumbrance = (packTarget == pawn);
                                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, thing, "ReservedBy"));
                        }
                        else
                        {
                            if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, thing, thing.stackCount))
                            {
                                opts.Add(new FloatMenuOption("CannotLoadIntoCaravanAll".Translate(new NamedArgument[]
                                {
                            thing.Label
                                }) + " (" + "TooHeavy".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else
                            {
                                float capacityLeft2 = capacityLeft - (float)thing.stackCount * thing.GetStatValue(StatDefOf.Mass, true);
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravanAll".Translate(new NamedArgument[]
                                {
                            thing.Label
                                }), capacityLeft2), delegate ()
                                {
                                    thing.SetForbidden(false, false);
                                    Job job = new Job(jobDef, thing);
                                    job.count = thing.stackCount;
                                    job.checkEncumbrance = (packTarget == pawn);
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, thing, "ReservedBy"));
                            }
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("LoadIntoCaravanSome".Translate(new NamedArgument[]
                            {
                        thing.LabelNoCount
                            }), delegate ()
                            {
                                int to = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(packTarget, thing), thing.stackCount);
                                Dialog_Slider window = new Dialog_Slider(delegate (int val)
                                {
                                    float capacityLeft3 = capacityLeft - (float)val * thing.GetStatValue(StatDefOf.Mass, true);
                                    return CaravanFormingUtility.AppendOverweightInfo(string.Format("LoadIntoCaravanCount".Translate(new NamedArgument[]
                                    {
                                thing.LabelNoCount
                                    }), val), capacityLeft3);
                                }, 1, to, delegate (int count)
                                {
                                    thing.SetForbidden(false, false);
                                    Job job = new Job(jobDef, thing);
                                    job.count = count;
                                    job.checkEncumbrance = (packTarget == pawn);
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }, int.MinValue);
                                Find.WindowStack.Add(window);
                            }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, thing, "ReservedBy"));
                        }
                    }
                }
            }

            if (opts.Count == 0)
            {
                opts.Add(new FloatMenuOption("NoneBrackets".Translate(), null) { Disabled = true });
            }
            return opts;
        }
        Vector2 scrollPos;
        float scrollViewHeight;
        string searchQuery;
    }
}
