# Contributing to Project RimFactory Revived


## C# Styleguide

### General
- Use `Spaces` for the Indentation and `CRLF` as the EOL Char.
- Only the required `using` Statements should be pressent.
- Files should be Named after the contained entry (Class, Interface, ...)

### Harmony Patches
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
/// - Cold Storage
///
/// Old Note: Art & maybe other things too need a separate patch
/// </summary>
```

## XML Styleguide
TODO

## LLM Usage
Large language models are prone to generate Invalid or Inaccurate Output and will present them with 100% Confidence.
As a user of such technology you MUST be aware of those issues and be able to spot them.
You MUST review the output and verify that it is correct.
If you Submit a PR or an Issue, you are responsible for its contents.
Make sure they are accurate. 

Large language models also tend to generate a lot of output/filler, much more than any Human realistically would.
This means that one has to take much greater care when reviewing with LLM output.
Time is a valuable resource and maintainers tend to have little of it to spare.

For those Reasons, 
if your Submission is found to be substantially LLM generated and or you are unable to explain your submission with sufficient detail in your own words,
your submission MAY be rejected without detailed explanation at the full discretion of the Maintainers.

### Acceptable LLM usage
- Correcting Spelling Issues
- Correcting Grammatical Issues
- to Review your own work (for yourself)
- for Learning purposes 
- (to an extend) Single Line Completions
