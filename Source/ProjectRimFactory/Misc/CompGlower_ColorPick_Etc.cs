using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Reflection.Emit;

/***************************************************
 * A comp glower (and corresponding compproperty)  * IMPORTANT TODO:
 * that can switch between a set of colors.        * Replace all
 * Colors available ar defined here in the C# code * color names
 * (see lines 45-50)                               * and labels
 * Harcoded to accept basic stats from PRF lamp    * with translation
 * (see lines 35,36)                               * keys.
 * Any questions, blame LWM                        *
 **************************************************/

namespace ProjectRimFactory.Misc {
    public class CompProperties_Glower_ColorPick : CompProperties_Glower {
        public CompProperties_Glower_ColorPick()
          {
            this.compClass = typeof(CompGlower_ColorPick);
          }
    }

    [StaticConstructorOnStartup] // need to build available colors
    public class CompGlower_ColorPick : CompGlower {
        static Dictionary<string, CompProperties_Glower> availableColors=null; //fill once defs are ready
        //static public void InitOnDefsLoaded() { // this needs to be calld by a StaticConstructorOnStartup
        static CompGlower_ColorPick() {
            CompProperties_Glower_ColorPick white=DefDatabase<ThingDef>.GetNamed("PRF_IndustrialLamp", false)?.
                GetCompProperties<CompProperties_Glower_ColorPick>();
            if (white==null) {
                white = new CompProperties_Glower_ColorPick();
                white.glowRadius=20;
                white.glowColor=new ColorInt(255,255,255);
            }
            availableColors=new Dictionary<string, CompProperties_Glower>();
            // "white" has important things like radius, etc, so use it as a basis for other colors:
            availableColors.Add("white (default)", white);
            AddColor("red", new ColorInt(255,0,0), white);
            AddColor("blue", new ColorInt(0,0,255), white);
            AddColor("green", new ColorInt(0,255,0), white);
            AddColor("cyan", new ColorInt(0,255,255), white);
            AddColor("yellow", new ColorInt(255,255,0), white);
            AddColor("magenta", new ColorInt(255,0,255), white);
        }
        public CompGlower_ColorPick() : base() {  // constructor
            // If several item use the comp glower, get them all on the same page to start:
            this.props=availableColors.ElementAt(0).Value;
        }
        static void AddColor(string name, ColorInt color, CompProperties_Glower_ColorPick basis) {
            var cp=new CompProperties_Glower_ColorPick();
            cp.glowColor=color;
            cp.glowRadius=basis.glowRadius;
            cp.overlightRadius=basis.overlightRadius;
            availableColors.Add(name, cp);
        }
        //color changer gizmo
        public override IEnumerable<Gizmo> CompGetGizmosExtra() {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;
            if (availableColors==null) yield break;
            Color tmpColor=Props.glowColor.ToColor; // current color
            // don't blind anyone with bright icon:
            tmpColor.a=0.75f; // lowering "a" lowers how much color shows up

            yield return new Command_Action {
                // don't display default color:
                defaultLabel=((Props==availableColors.ElementAt(0).Value)?"Change Color?":("("+colorLabel+")\nChange Color?")),
                defaultIconColor=tmpColor,
                groupKey=711712,
                icon=Texture2D.whiteTexture, // nice bright white background
                action=delegate() {
                    List<FloatMenuOption> mlist = new List<FloatMenuOption>();
                    foreach (var kv in availableColors) {
                        mlist.Add(new FloatMenuOption(kv.Key,
                            delegate() {
                              this.props=kv.Value;
                              colorLabel=kv.Key;
                              if (parent.Spawned) {
                                  parent.Map.glowGrid.DeRegisterGlower(this);
                                  parent.Map.glowGrid.RegisterGlower(this);
                                  Log.Message(""+parent+" changing color to "+kv.Value.glowColor.ToColor);
                              }
                            }));
                    }
                    Find.WindowStack.Add(new FloatMenu(mlist));
                }
            };
        }
        string colorLabel="";
    }
}
