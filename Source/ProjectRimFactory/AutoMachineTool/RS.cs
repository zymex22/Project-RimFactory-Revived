using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace ProjectRimFactory.AutoMachineTool
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


            OutputDirectionIcon = ContentFinder<Texture2D>.Get("UI/OutputDirection", true);
            PlayIcon = ContentFinder<Texture2D>.Get("UI/Play", true);

            DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);

            Arrow = ContentFinder<Texture2D>.Get("UI/Overlays/Arrow", true);
        }

        public static readonly Texture2D PregnantIcon;
        public static readonly Texture2D BondIcon;
        public static readonly Texture2D MaleIcon;
        public static readonly Texture2D FemaleIcon;

        public static readonly Texture2D SlaughterIcon;
        public static readonly Texture2D TrainedIcon;
        public static readonly Texture2D YoungIcon;
        public static readonly Texture2D AdultIcon;

        public static readonly Texture2D OutputDirectionIcon;
        public static readonly Texture2D PlayIcon;

        public static readonly Texture2D DeleteX;

        public static readonly Texture2D Arrow;
    }
}
