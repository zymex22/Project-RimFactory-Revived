using ProjectRimFactory.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Archo
{
    public class ITab_ArchoCipher : ITab
    {
        public ITab_ArchoCipher()
        {
            size = new Vector2(400f, 400f);
            labelKey = "PRF_ITab_ArchoCipher";
        }
        public override bool IsVisible => PRFDefOf.PRFOrdoDataRummaging.IsFinished;
        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);
            listing.Label("PRFArchoCipherInterpreter".Translate());
            ciphertext = listing.TextEntryLabeled("PRF_InsertCiphertext".Translate(), ciphertext);
            if (listing.ButtonText("PRF_ButtonDecipher".Translate()))
            {
                string result = ArchoCipher.Decipher(ciphertext);
                if (result != null)
                {
                    resultText = result;
                }
                else
                {
                    System.Random random = new System.Random(ciphertext.Sum(Convert.ToInt32));
                    int length = random.Next(1, 513);
                    string gibberish = "PRF_GibberishAlphabet".Translate();
                    char[] output = new char[length];
                    for (int i = 0; i < output.Length; i++)
                    {
                        output[i] = gibberish[random.Next(gibberish.Length)];
                    }
                    resultText = new string(output);
                }
            }
            if (listing.ButtonText("PRF_ButtonClear".Translate()))
            {
                resultText = null;
                ciphertext = null;
            }
            listing.Label(resultText);
            listing.End();
        }
        public string ciphertext;
        public string resultText;
    }
}
