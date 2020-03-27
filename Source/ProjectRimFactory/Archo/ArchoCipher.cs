using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Verse;

namespace ProjectRimFactory.Archo
{
    public static class ArchoCipher
    {
        public static string Decipher(string str)
        {
            if (Regex.IsMatch(str, "[\"\']\\)?;"))
            {
                return "PRF_ArchoCipher_InternalServerError".Translate();
            }
            string key = "PRF_ArchoCipherKey_" + str.Replace(' ', '_');
            if (Translator.TryTranslate(key, out TaggedString result))
            {
                return result;
            }
            return null;
        }
    }
}
