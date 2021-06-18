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
                    //Dummy Values UPDATE before Merge!
					case "PRF_BeltsI":
						return "PRF_Research_BeltsI";
                    case "PRF_BeltsII":
                        return "PRF_Research_BeltsII";
                    case "PRF_BeltsIII":
                        return "PRF_Research_BeltsIII";

                }
			}
            return null;
		}

        public override Type GetBackCompatibleType(Type baseType, string providedClassName, XmlNode node)
        {
            return null;
        }

        public override void PostExposeData(object obj)
        {
            
        }
    }
}
