using UnityEngine;
using Verse;

namespace ProjectRimFactory
{
    [StaticConstructorOnStartup]
    public static class RS
    {
        static RS()
        {
            PregnantIcon = ContentFinder<Texture2D>.Get("UI/Icons/Animal/Pregnant");
            BondIcon = ContentFinder<Texture2D>.Get("UI/Icons/Animal/Bond");
            MaleIcon = Gender.Male.GetIcon();
            FemaleIcon = Gender.Female.GetIcon();
            SlaughterIcon = ContentFinder<Texture2D>.Get("UI/Icons/Animal/Slaughter");
            TrainedIcon = ContentFinder<Texture2D>.Get("UI/Icons/Trainables/Obedience");
            YoungIcon = ContentFinder<Texture2D>.Get("UI/Icons/LifeStage/Young");
            AdultIcon = ContentFinder<Texture2D>.Get("UI/Icons/LifeStage/Adult");

            ForbidOn = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOn");
            ForbidOff = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOff");


            OutputDirectionIcon = ContentFinder<Texture2D>.Get("PRFUi/OutputDirection");
            PlayIcon = ContentFinder<Texture2D>.Get("PRFUi/Play");

            SplitterDisabled = ContentFinder<Texture2D>.Get("PRFUi/ForbidIcon");
            SplitterArrowUp = ContentFinder<Texture2D>.Get("PRFUi/UpArrow");
            SplitterArrowRight = ContentFinder<Texture2D>.Get("PRFUi/RightArrow");
            SplitterArrowLeft = ContentFinder<Texture2D>.Get("PRFUi/LeftArrow");
            SplitterArrowDown = ContentFinder<Texture2D>.Get("PRFUi/DownArrow");
            
            Arrow = ContentFinder<Texture2D>.Get("UI/Overlays/Arrow");
            // Initialize graphics for SpecialSculptures:
            foreach (var s in Common.ProjectRimFactory_ModComponent.availableSpecialSculptures)
                s.Init();
        }

        public static readonly Texture2D SplitterArrowUp;
        public static readonly Texture2D SplitterArrowRight;
        public static readonly Texture2D SplitterArrowLeft;
        public static readonly Texture2D SplitterArrowDown;
        public static readonly Texture2D SplitterDisabled;

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
        public static readonly Texture2D PlayIcon;
        
        public static readonly Texture2D Arrow;
    }
}
