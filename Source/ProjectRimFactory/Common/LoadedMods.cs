using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;


namespace ProjectRimFactory.Common
{
    [StaticConstructorOnStartup]
    class LoadedMods
    {
        public static ModMetaData Metha_sos2 = null;

        static LoadedMods()
        {
            
            Metha_sos2 = ModLister.GetActiveModWithIdentifier("kentington.saveourship2");


        }



    }
}
