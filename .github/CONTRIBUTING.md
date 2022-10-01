# C# Styleguide

## General
- Use `Spaces` for the Indentation and `CRLF` as the EOL Char.
- Only the required `using` Statements should be pressent.
- Files should be Named after the contained entry (Class, Interface, ...)

## Harmony Patches
Should be named in the Following Format: `Patch_<Type>_<Method>`
- `<Type>` Refers to the Type that is Patched
- `<Method>` The Method that is being Patched

_Example:_
```Csharp
[HarmonyPatch(typeof(TradeUtility), "AllLaunchableThingsForTrade")]
class Patch_TradeUtility_AllLaunchableThingsForTrade
```

Furthermore Patches should include a `summary` Documentation line.
This line should include the following details:
- Why is this Patch required?
- What does this Patch do?
- _(Optional) Where is this used?_
- _(Optional) Additional Comments_
 
_Example:_
```Csharp
/// <summary>
/// This Patch Counts additional Items for the Do until X Type Bills
/// Currently adds Items from:
/// - AssemblerQueue
/// - Cold STorage
///
/// Old Note: Art & maybe other things too need a separate patch
/// </summary>
```

# XML Styleguide
TODO