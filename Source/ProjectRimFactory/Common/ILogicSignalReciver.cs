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
        /// True If The Logic Signal shall be Used
        /// If False it shall be ignored
        /// </summary>
        bool LogicSignalUsed { get; set; }
        
        /// <summary>
        /// The Signal In Question
        /// </summary>
        Industry.LogicSignal RefrerenceSignal { get; set; }





    }


 
}
