using System;
using Verse;
namespace ProjectRimFactory
{
    /// <summary>
    /// A set of Debug trace functions for help in tracking weird problems.
    /// NOTE: These are only compiled in the Debug build.  As far as a Release
    ///   build is concerned, these methods *do not exist*.
    /// </summary>
    /// <use>
    /// Make sure there is an entry in the Flag enum.
    /// Set activeFlags to include that Flag (TODO: include this in mod setings)
    /// Call via:  Debug.Message(Debug.Flag.Conveyors, "Message blah blah blah");
    /// Note: since these methods only exist in Debug builds, they have no
    ///     performance impact in Release builds.
    /// Incorrect usage:
    ///     string debugString = "really complicated"+(string)ExpensiveMethod();
    ///     Debug.Message(flag, debugString);
    /// Correct:
    ///     Debug.Message(flag, "really complicated"+(string)ThatOnlyHappensInDebug());
    /// Remember: you may also use #if DEBUG #endif to do any other debug-related stuff
    /// If you have a debugger you might prefer that approach? Whatever makes
    /// you happy ^.^
    /// </use>
    /// <onlyAppliesTo>
    /// The Log messages only appear in the debug build.
    /// </onlyAppliesTo>
    public static class Debug
    {
        [Flags]
        public enum Flag
        {
            PlaceThing = 0x1,
            Conveyors = 0x2,
            ConveyorGraphics = 0x4,
            Benchmark = 0x8, // performance measurement
            ExtModifyProduct = 0x10,
            AssemblerQueue = 0x20,
            // NextFlag=0x4,
            // 0x8
            // 0x10
            // 0x20
            // 0x40, etc, as powers of two
        }
        public static Flag activeFlags = (Flag)0; //Flag.PlaceThing | Flag.Conveyors...

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Message(Debug.Flag flag, string text, bool ignoreStopLoggingLimit = false)
        {
            if ((activeFlags & flag) > 0) Log.Message(text);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Warning(Debug.Flag flag, string text, bool ignoreStopLoggingLimit = false)
        {
            if ((activeFlags & flag) > 0) Log.Warning(text);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Error(Debug.Flag flag, string text, bool ignoreStopLoggingLimit = false)
        {
            if ((activeFlags & flag) > 0) Log.Error(text);
        }
        // NOTE: Additional options are possible:
        // https://github.com/zymex22/Project-RimFactory-Revived/issues/95
        // For example, allowing a debug trace call to specify the calling method, which
        //   might be useful for tracing callbacks (or maybe not, who knows).
        // If you think it might be useful, feel free to add another parameter `string methodName=null`
        // as described in Issue 95
    }
}
