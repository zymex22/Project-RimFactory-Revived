﻿using Verse;

namespace ProjectRimFactory.Storage.Editables
{
    public class DefModExtension_Crate : DefModExtension
    {
        public bool destroyContainsItems = false;
        public bool forbidPawnAccess = false;
        public bool hideItems = false;
        public bool hideRightClickMenus = false;
        public int limit = 10;
    }
}