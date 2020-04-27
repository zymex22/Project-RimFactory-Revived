using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ProjectRimFactory.Storage.Editables
{
    public class DefModExtension_Crate : DefModExtension
    {
        public int limit = 10;
        public bool destroyContainsItems = false;
        public bool hideItems = false;
        public bool forbidPawnAccess = false;
    }
}
