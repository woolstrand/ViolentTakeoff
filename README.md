# ViolentTakeoff
RimWorld mod which makes gravship launch a tiny bit more spectacular

## Building

`dotnet build -c Release` in `Source/ViolentTakeoff` builds both game versions in one go:

- `1.6/Assemblies/ViolentTakeoff.dll` (net472, RimWorld 1.6)
- `1.7/Assemblies/ViolentTakeoff.dll` (net10.0, RimWorld 1.7)

Game reference DLLs are not committed. Put them into:

- `References/1.6/`: `Assembly-CSharp.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.IMGUIModule.dll`, `UnityEngine.TextRenderingModule.dll` from a 1.6 install, `0Harmony.dll` from Harmony's `1.6` folder
- `References/1.7/`: `Assembly-CSharp.dll`, `UnityStubs.dll`, `Waylith.dll` from a 1.7 install, `0Harmony.dll` from Harmony's `1.7` folder

or pass `-p:RefDir16=...` / `-p:RefDir17=...`.
