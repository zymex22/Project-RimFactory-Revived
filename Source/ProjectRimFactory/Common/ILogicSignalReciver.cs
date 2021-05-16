using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectRimFactory.Common
{

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





    }


 
}
