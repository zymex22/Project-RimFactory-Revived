using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;


namespace ProjectRimFactory.Common
{

    public class LSR_Entry : IExposable
    {
        public Industry.LogicSignal RefrerenceSignal = null;
        public ThingFilter Filter;
        public int ThingCount = 1;
        public int RetriggerDelay = 100;

        public int RemainingItems = 0;
        private int LastTrigger = -10000;
        private bool DelayActive = true;

        public bool TriggerValid()
        {
            int tick = Find.TickManager.TicksGame;
            if (DelayActive && tick > LastTrigger + RetriggerDelay)
            {
                DelayActive = false;
                RemainingItems = ThingCount;
            }
            if (!DelayActive && RemainingItems > 0)
            {
                return true;
            }else if (!DelayActive)
            {
                DelayActive = true;
                LastTrigger = tick;
            }
            return false;
            
        }

        public void MoveItems(int cnt)
        {
            if (cnt > ThingCount)
            {
                Log.Error("PRF Error in LSR_Entry::MoveItems cnt is greater then Limit");
            }
            RemainingItems -= cnt;
        }



        public LSR_Entry(Industry.LogicSignal signal)
        {
            RefrerenceSignal = signal;
            Filter = new ThingFilter();
        }

        public LSR_Entry()
        {
            //for ExposeData() Only
        }






        public void ExposeData()
        {
            Scribe_Values.Look(ref ThingCount, "ThingCount");
            Scribe_Values.Look(ref RetriggerDelay, "RetriggerDelay");
            Scribe_Deep.Look(ref Filter, "Filter");
            Scribe_References.Look(ref RefrerenceSignal, "RefrerenceSignal");
        }
    }

    
    class Window_ConditionalLSREditor : Window
    {
        public override Vector2 InitialSize => new Vector2(800f, 450f);

        private Vector2 scrolPos = new Vector2();
        private Vector2 scrollPos_itemFilter = new Vector2();
        private int SelectedIndex = -1;

        private static GUIStyle richTextStyle
        {
            get
            {
                GUIStyle gtter_richTextStyle = new GUIStyle();
                gtter_richTextStyle.richText = true;
                gtter_richTextStyle.normal.textColor = Color.white;
                return gtter_richTextStyle;
            }
        }

        private int LSR_Item(LSR_Entry entry, Rect rect, int i, bool selected)
        {


            bool clicked = Widgets.ButtonInvisible(rect);
            string vrString = "";

            if (selected) vrString += "<color=red>";

            vrString += "(" + (i + 1) + ")\t" + entry.RefrerenceSignal.Name;

            if (selected) vrString += "</color>";

            CommonGUIFunctions.Label(rect, vrString, richTextStyle);



            return clicked ? i : -1;
        }


        public override void DoWindowContents(Rect inRect)
        {
            
            float margin = 5;
            float listboxHight = 400;
            float listBoxWidth = 250;
            Rect listBoxRect = new Rect(margin, margin, listBoxWidth, listboxHight);
            float buttonHight = 20;
            float buttonWidth = 100;
            float buttonWidth2 = 150;

            float ThingFilter_Width = 200;
            float ThingFilter_Hight = 400;

            CommonGUIFunctions.ListBox(listBoxRect, ref scrolPos, ref SelectedIndex, SelectedIndex, parrent.LSR_Advanced, delegate (LSR_Entry r, Rect c, int e, bool f) { return LSR_Item(r, c, e, f); },null);

            Rect InteractButton_Rect = new Rect(margin + listBoxWidth + margin, margin, buttonWidth, buttonHight);

            List<FloatMenuOption> floatMenuOptionsAdd = pRFGameComp.LoigSignalRegestry.Where(e => e.Key.AvailableCrossMap || e.Value == map).Select(e => e.Key)
                                                    .Select(g => new FloatMenuOption(g.Name, () => { parrent.LSR_Advanced.Add(new LSR_Entry(g)); }))
                                                    .ToList();

            if (Widgets.ButtonText(InteractButton_Rect, "Add"))
            {
                Find.WindowStack.Add(new FloatMenu(floatMenuOptionsAdd));
            }
            InteractButton_Rect.y = margin + (listboxHight / 2) - (margin / 2);
            
            if (Widgets.ButtonText(InteractButton_Rect, "Move Up"))
            {
                if (SelectedIndex > 0)
                {
                    LSR_Entry entry = parrent.LSR_Advanced[SelectedIndex];
                    parrent.LSR_Advanced.Remove(entry);
                    SelectedIndex--;
                    parrent.LSR_Advanced.Insert(SelectedIndex, entry);

                }
            }

            InteractButton_Rect.y = margin + (listboxHight / 2) + (margin / 2) + buttonHight;
            if (Widgets.ButtonText(InteractButton_Rect, "Move Down"))
            {
                if (SelectedIndex != -1 && SelectedIndex < parrent.LSR_Advanced.Count - 1)
                {
                    LSR_Entry entry = parrent.LSR_Advanced[SelectedIndex];
                    parrent.LSR_Advanced.Remove(entry);
                    SelectedIndex++;
                    parrent.LSR_Advanced.Insert(SelectedIndex, entry);
                }
            }

            InteractButton_Rect.y = margin + listboxHight - buttonHight;
            if (Widgets.ButtonText(InteractButton_Rect, "Remove"))
            {
                if (SelectedIndex != -1)
                {
                    parrent.LSR_Advanced.RemoveAt(SelectedIndex);
                    SelectedIndex = -1;
                }
            }



            //Devidor Line
            Widgets.DrawLineVertical(InteractButton_Rect.x + buttonWidth + (margin * 2), 0, inRect.height);

            if (SelectedIndex != -1)
            {
                LSR_Entry selectedItem = parrent.LSR_Advanced[SelectedIndex];
                
                Rect ThingFilter_Rect = new Rect(InteractButton_Rect.x + buttonWidth + (margin * 4), margin, ThingFilter_Width, ThingFilter_Hight);

                ThingFilterUI.DoThingFilterConfigWindow(ThingFilter_Rect, ref scrollPos_itemFilter, selectedItem.Filter);

                Rect InteractButtons_Rect = new Rect(ThingFilter_Rect.x + ThingFilter_Width + margin, margin, buttonWidth2, buttonHight);

                List<FloatMenuOption> floatMenuOptionsChange = pRFGameComp.LoigSignalRegestry.Where(e => e.Key.AvailableCrossMap || e.Value == map).Select(e => e.Key)
                                                    .Select(g => new FloatMenuOption(g.Name, () => { selectedItem.RefrerenceSignal = g; }))
                                                    .ToList();
                if (Widgets.ButtonText(InteractButtons_Rect, selectedItem.RefrerenceSignal.Name))
                {
                    Find.WindowStack.Add(new FloatMenu(floatMenuOptionsChange));
                }

                InteractButtons_Rect.y += buttonHight + margin;
                string strbuff = "" + selectedItem.RetriggerDelay;
                Widgets.TextFieldNumericLabeled(InteractButtons_Rect, "Delay in s", ref selectedItem.RetriggerDelay, ref strbuff, 0, 1000);
                InteractButtons_Rect.y += buttonHight + margin;
                strbuff = "" + selectedItem.ThingCount;
                Widgets.TextFieldNumericLabeled(InteractButtons_Rect, "Times", ref selectedItem.ThingCount, ref strbuff, 1);


            }
        }

 //       private string ntf_buffer1 = "";
   //     private string ntf_buffer2 = "";

        private ILogicSignalReciver parrent;
        private Map map;
        PRFGameComponent pRFGameComp;
        public Window_ConditionalLSREditor(ILogicSignalReciver logicSignalInterface, Map parrentMap , PRFGameComponent pRFGame)
        {
            parrent = logicSignalInterface;
            map = parrentMap;
            pRFGameComp = pRFGame;
            doCloseX = true;
        }
    }



    interface ILogicSignalReciver
    {
        /// <summary>
        /// True If The Logic Signal is Active
        /// </summary>
        bool LogicSignaStatus { get;}
        
        /// <summary>
        /// The Signal In Question
        /// </summary>
        Industry.LogicSignal RefrerenceSignal { get; set; }

        /// <summary>
        /// If True then show the Gui for the Advanced Configuration
        /// </summary>
        bool SupportsAdvancedReciverMode { get; }
        bool UsesAdvancedReciverMode { get; set; }
        /// <summary>
        /// List With Advanced Reciver Rules
        /// shall operate using a first match style rule
        /// </summary>
        List<LSR_Entry> LSR_Advanced { get; set; }

    }


 
}
