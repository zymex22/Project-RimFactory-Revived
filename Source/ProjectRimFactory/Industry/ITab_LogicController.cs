using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;



namespace ProjectRimFactory.Industry
{
    class ITab_LogicController : ITab
    {

        private LogicController this_Controller { get => this.SelThing as LogicController; }

        public override bool IsVisible => base.IsVisible;

        protected override void FillTab()
        {
            //this_Controller.LogicSignals


            //Somhow need to add a Edit UI Here
            //Welp


            throw new NotImplementedException();
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();



        }

        public ITab_LogicController()
        {
            this.labelKey = "Logic_GUI";
        }

    }
}
