using System;
using System.Xml;
using Verse;

namespace ProjectRimFactory.Common.BackCompatibility
{
    public class PRF_BackCompatibilityConverter : BackCompatibilityConverter
    {
        public override bool AppliesToVersion(int majorVer, int minorVer)
        {
            return true;
        }

        public PRF_BackCompatibilityConverter() { }

        public override string BackCompatibleDefName(Type defType, string defName, bool forDefInjections = false, XmlNode node = null)
        {

            if (defType == typeof(ResearchProjectDef))
            {
                switch (defName)
                {
                    //PRF Materials Rename
                    case "PRF_StainlessSteelAlloying":
                        return "PRF_Research_StainlessSteelAlloying";

                    case "PRF_Carbon":
                        return "PRF_Research_Carbon";
                    case "PRF_CarbonNanotubes":
                        return "PRF_Research_CarbonNanotubes";
                    case "PRF_CarbonFiber":
                        return "PRF_Research_CarbonFiber";
                    case "PRF_CarbonKevlar":
                        return "PRF_Research_CarbonKevlar";
                    case "PRF_CarbonPlating":
                        return "PRF_Research_CarbonPlating";
                    case "PRF_SyntheticDiamond":
                        return "PRF_Research_SyntheticDiamond";

                    case "PRF_AdvancedFlooring":
                        return "PRF_Research_AdvancedFlooring";

                    case "PRF_CokeProduction":
                        return "PRF_Research_CokeProduction";
                    case "PRF_Brick":
                        return "PRF_Research_Brick";
                    case "PRF_BasicGlass":
                        return "PRF_Research_BasicGlass";

                    case "PRF_ReinforcedGlass":
                        return "PRF_Research_ReinforcedGlass";
                    case "PRF_Cement":
                        return "PRF_Research_Cement";
                    case "PRF_BasicConcrete":
                        return "PRF_Research_BasicConcrete";

                    case "PRF_BulkConcrete":
                        return "PRF_Research_BulkConcrete";

                    case "PRF_AdvancedConcrete":
                        return "PRF_Research_AdvancedConcrete";

                    case "PRF_BasicPlastics":
                        return "PRF_Research_BasicPlastics";
                    case "PRF_AdvPlastics":
                        return "PRF_Research_AdvPlastics";
                    case "PRF_BasicPlasticTextiles":
                        return "PRF_Research_BasicPlasticTextiles";
                    case "PRF_AdvPlasticTextiles":
                        return "PRF_Research_AdvPlasticTextiles";
                    case "PRF_GlitterPlasticTextiles":
                        return "PRF_Research_GlitterPlasticTextiles";


                }
            }
            else if (defType == typeof(RecipeDef))
            {
                switch (defName)
                {
                    case "K_DarkAndesite":
                        return "PRF_Excavate_DarkAndesite";
                    case "K_Anorthosite":
                        return "PRF_Excavate_Anorthosite";
                    case "K_Basalt":
                        return "PRF_Excavate_Basalt";
                    case "K_Blueschist":
                        return "PRF_Excavate_Blueschist";
                    case "K_Chalk":
                        return "PRF_Excavate_Chalk";
                    case "K_Charnockite":
                        return "PRF_Excavate_Charnockite";
                    case "K_CreoleMarble":
                        return "PRF_Excavate_CreoleMarble";
                    case "K_Dacite":
                        return "PRF_Excavate_Dacite";
                    case "K_VibrantDunite":
                        return "PRF_Excavate_VibrantDunite";
                    case "K_Emperadordark":
                        return "PRF_Excavate_Emperadordark";
                    case "K_EtowahMarble":
                        return "PRF_Excavate_EtowahMarble";
                    case "K_GreenGabbro":
                        return "PRF_Excavate_GreenGabbro";
                    case "K_GreenSchist":
                        return "PRF_Excavate_GreenSchist";
                    case "K_Jaspillite":
                        return "PRF_Excavate_Jaspillite";
                    case "K_Lepidolite":
                        return "PRF_Excavate_Lepidolite";
                    case "K_Lherzolite":
                        return "PRF_Excavate_Lherzolite";
                    case "K_Lignite":
                        return "PRF_Excavate_Lignite";
                    case "K_Migmatite":
                        return "PRF_Excavate_Migmatite";
                    case "K_Monzonite":
                        return "PRF_Excavate_Monzonite";
                    case "K_Obsidian":
                        return "PRF_Excavate_Obsidian";
                    case "K_Rhyolite":
                        return "PRF_Excavate_Rhyolite";
                    case "K_Scoria":
                        return "PRF_Excavate_Scoria";
                    case "K_Serpentinite":
                        return "PRF_Excavate_Serpentinite";
                    case "K_Siltstone":
                        return "PRF_Excavate_Siltstone";
                    case "K_Sovite":
                        return "PRF_Excavate_Sovite";
                    case "K_Thometzekite":
                        return "PRF_Excavate_Thometzekite";
                }
            }

            return null;
        }

        public override Type GetBackCompatibleType(Type baseType, string providedClassName, XmlNode node)
        {
            switch (providedClassName)
            {
                case "ProjectRimFactory.AutoMachineTool.PRF_SAL_Trarget+Bill_ProductionWithUftPawnForbidded":
                    return typeof(AutoMachineTool.SAL_TargetWorktable.Bill_ProductionWithUftPawnForbidded);
                case "ProjectRimFactory.AutoMachineTool.PRF_SAL_Trarget+Bill_ProductionPawnForbidded":
                    return typeof(AutoMachineTool.SAL_TargetWorktable.Bill_ProductionPawnForbidded);
            }

            return null;
        }

        public override void PostExposeData(object obj)
        {

        }
    }
}
