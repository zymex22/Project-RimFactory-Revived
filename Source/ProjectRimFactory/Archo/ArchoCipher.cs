﻿using System.Text.RegularExpressions;
using Verse;

namespace ProjectRimFactory.Archo
{
    public static class ArchoCipher
    {
        public static string Decipher(string str)
        {
            if (Regex.IsMatch(str, "[\"\']\\)?;")) return "PRF_ArchoCipher_InternalServerError".Translate();
            var key = "PRF_ArchoCipherKey_" + str.Replace(' ', '_');
            if (key.TryTranslate(out var result)) return result;
            return null;
        }
    }
}