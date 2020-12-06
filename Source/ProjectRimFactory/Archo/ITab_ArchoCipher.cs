using System;
using System.Linq;
using ProjectRimFactory.Common;
using RimWorld;
using UnityEngine;
using Verse;
using Random = System.Random;

namespace ProjectRimFactory.Archo
{
    public class ITab_ArchoCipher : ITab
    {
        public string ciphertext;
        public string resultText;

        public ITab_ArchoCipher()
        {
            size = new Vector2(400f, 400f);
            labelKey = "PRF_ITab_ArchoCipher";
        }

        public override bool IsVisible => PRFDefOf.PRFOrdoDataRummaging.IsFinished;

        protected override void FillTab()
        {
            var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            var listing = new Listing_Standard();
            listing.Begin(rect);
            listing.Label("PRFArchoCipherInterpreter".Translate());
            ciphertext = listing.TextEntryLabeled("PRF_InsertCiphertext".Translate(), ciphertext);
            if (listing.ButtonText("PRF_ButtonDecipher".Translate()))
            {
                var result = ArchoCipher.Decipher(ciphertext);
                if (result != null)
                {
                    resultText = result;
                }
                else
                {
                    var random = new Random(ciphertext.Sum(Convert.ToInt32));
                    var length = random.Next(1, 513);
                    string gibberish = "PRF_GibberishAlphabet".Translate();
                    var output = new char[length];
                    for (var i = 0; i < output.Length; i++) output[i] = gibberish[random.Next(gibberish.Length)];
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
    }
}