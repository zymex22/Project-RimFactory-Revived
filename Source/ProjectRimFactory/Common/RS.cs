using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace ProjectRimFactory
{
    [StaticConstructorOnStartup]
    public static class RS
    {
        static RS()
        {
            PregnantIcon = ContentFinder<Texture2D>.Get("UI/Icons/Animal/Pregnant", true);
            BondIcon = ContentFinder<Texture2D>.Get("UI/Icons/Animal/Bond", true);
            MaleIcon = GenderUtility.GetIcon(Gender.Male);
            FemaleIcon = GenderUtility.GetIcon(Gender.Female);
            SlaughterIcon = ContentFinder<Texture2D>.Get("UI/Icons/Animal/Slaughter", true);
            TrainedIcon = ContentFinder<Texture2D>.Get("UI/Icons/Trainables/Obedience", true);
            YoungIcon = ContentFinder<Texture2D>.Get("UI/Icons/LifeStage/Young", true);
            AdultIcon = ContentFinder<Texture2D>.Get("UI/Icons/LifeStage/Adult", true);

            ForbidOn = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOn", true);
            ForbidOff = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOff", true);


            OutputDirectionIcon = ContentFinder<Texture2D>.Get("PRFUi/OutputDirection", true);
            ForbidIcon = ContentFinder<Texture2D>.Get("PRFUi/Forbid", true);
            PlayIcon = ContentFinder<Texture2D>.Get("PRFUi/Play", true);

            SplitterDisabeld = ContentFinder<Texture2D>.Get("PRFUi/splitter/disabled", true);
            SplitterArrow_N = ContentFinder<Texture2D>.Get("PRFUi/splitter/arrow_N", true);
            SplitterArrow_E = ContentFinder<Texture2D>.Get("PRFUi/splitter/arrow_E", true);
            SplitterArrow_W = ContentFinder<Texture2D>.Get("PRFUi/splitter/arrow_W", true);
            SplitterArrow_S = ContentFinder<Texture2D>.Get("PRFUi/splitter/arrow_S", true);

            SplitterArrow_in_N = ContentFinder<Texture2D>.Get("PRFUi/splitter/arrow_in_N", true);
            SplitterArrow_in_E = ContentFinder<Texture2D>.Get("PRFUi/splitter/arrow_in_E", true);
            SplitterArrow_in_W = ContentFinder<Texture2D>.Get("PRFUi/splitter/arrow_in_W", true);
            SplitterArrow_in_S = ContentFinder<Texture2D>.Get("PRFUi/splitter/arrow_in_S", true);

            SplitterArrowSel_N = ContentFinder<Texture2D>.Get("PRFUi/splitter/arrow_selected_N", true);
            SplitterArrowSel_E = ContentFinder<Texture2D>.Get("PRFUi/splitter/arrow_selected_E", true);
            SplitterArrowSel_W = ContentFinder<Texture2D>.Get("PRFUi/splitter/arrow_selected_W", true);
            SplitterArrowSel_S = ContentFinder<Texture2D>.Get("PRFUi/splitter/arrow_selected_S", true);


            DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);

            Arrow = ContentFinder<Texture2D>.Get("UI/Overlays/Arrow", true);
            // Initialize graphics for SpecialSculptures:
            foreach (var s in ProjectRimFactory.Common.
                     ProjectRimFactory_ModComponent.availableSpecialSculptures)
                s.Init();
        }

        public static readonly Texture2D SplitterArrow_N;
        public static readonly Texture2D SplitterArrow_E;
        public static readonly Texture2D SplitterArrow_W;
        public static readonly Texture2D SplitterArrow_S;
        
        public static readonly Texture2D SplitterArrow_in_N;
        public static readonly Texture2D SplitterArrow_in_E;
        public static readonly Texture2D SplitterArrow_in_W;
        public static readonly Texture2D SplitterArrow_in_S;

        public static readonly Texture2D SplitterArrowSel_N;
        public static readonly Texture2D SplitterArrowSel_E;
        public static readonly Texture2D SplitterArrowSel_W;
        public static readonly Texture2D SplitterArrowSel_S;
        public static readonly Texture2D SplitterDisabeld;

        public static readonly Texture2D ForbidOn;
        public static readonly Texture2D ForbidOff;

        public static readonly Texture2D PregnantIcon;
        public static readonly Texture2D BondIcon;
        public static readonly Texture2D MaleIcon;
        public static readonly Texture2D FemaleIcon;

        public static readonly Texture2D SlaughterIcon;
        public static readonly Texture2D TrainedIcon;
        public static readonly Texture2D YoungIcon;
        public static readonly Texture2D AdultIcon;

        public static readonly Texture2D OutputDirectionIcon;
        public static readonly Texture2D ForbidIcon;
        public static readonly Texture2D PlayIcon;

        public static readonly Texture2D DeleteX;

        public static readonly Texture2D Arrow;
    }
}
