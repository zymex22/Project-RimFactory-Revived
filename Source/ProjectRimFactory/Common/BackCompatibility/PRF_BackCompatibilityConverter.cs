using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            return null;
		}

        public override Type GetBackCompatibleType(Type baseType, string providedClassName, XmlNode node)
        {
            switch (providedClassName)
            {
                case "ProjectRimFactory.AutoMachineTool.PRF_SAL_Trarget+Bill_ProductionWithUftPawnForbidded":
                    return typeof(ProjectRimFactory.AutoMachineTool.SAL_TargetWorktable.Bill_ProductionWithUftPawnForbidded);
                case "ProjectRimFactory.AutoMachineTool.PRF_SAL_Trarget+Bill_ProductionPawnForbidded":
                    return typeof(ProjectRimFactory.AutoMachineTool.SAL_TargetWorktable.Bill_ProductionPawnForbidded);
            }

            return null;
        }

        public override void PostExposeData(object obj)
        {
            
        }
    }
}
