using ProjectRimFactory.SAL3.Things.Assemblers;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Industry
{
    public class Building_PaperclipFactory : Building_SimpleAssembler
    {
        private const float PaperclipsPerKilogramModifier = 0.25f;

        protected override void PostProcessRecipeProduct(Thing thing)
        {
            var limit = thing.def.stackLimit;
            var paperclips = Mathf.RoundToInt(CurrentBillReport.selected.Sum(t => t.PaperclipAmount() * PaperclipsPerKilogramModifier));
            if (paperclips <= limit)
            {
                thing.stackCount = paperclips;
            }
            else
            {
                thing.stackCount = limit;
                paperclips -= limit;
                while (paperclips > 0)
                {
                    var count = Math.Min(paperclips, limit);
                    var newThing = ThingMaker.MakeThing(thing.def);
                    newThing.stackCount = count;
                    ThingQueue.Add(newThing);
                    paperclips -= count;
                }
            }
        }
    }
}
