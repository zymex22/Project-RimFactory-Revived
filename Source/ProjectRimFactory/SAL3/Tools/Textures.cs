using UnityEngine;
using Verse;

namespace ProjectRimFactory.SAL3.Tools
{
    [StaticConstructorOnStartup]
    public static class Textures
    {
        public static readonly Texture2D Paste = ContentFinder<Texture2D>.Get("UI/Buttons/Paste");
    }
}
