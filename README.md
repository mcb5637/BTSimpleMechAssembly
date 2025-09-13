# BTSimpleMechAssembly
Mod for BattleTech

Download the latest release here: https://github.com/mcb5637/BTSimpleMechAssembly/releases/latest/download/BTSimpleMechAssembly.zip

Changes how mechs are assembled.
Allows Crossvariant Assembly (requires machine shop argo upgrade by default) to combine parts of different mech variants to one mech.  
You are no longer forced to assemble mechs on gaining parts. Instead you get asked if you want to assemble a mech (and which if you have CrossvariantAssembly) and if it should go into storage or to you active mechs or even get directly sold.  
Manually start mech assembly by selecting parts and use the ready mech button.  
CrossvariantAssembly can be configuried to require same speed/tonnage and matching tags.  
Optional omnimech support. On readying an omnimech you can choose which variant you want to get (provided you already have seen that variant at least once).  
Assembled mechs can be required to be readied before use, or be instantly ready.  
If you use equiped mechs game rule, you gain the mech equipment in any case (goes into storage if you assemble and put the mech into storage, gets sold with the mech if you sell, adding their vaue to that of the mech).  
More intelligent Assembly algorythm. First uses the parts of the mech you want to assemble, and fills missing parts from Crossvariants (if enabled&unlocked) trying to leave at least one part of every variant remaining.  
Modified part count in salvage/shop screen: `currentParts (crossvariantParts) / maxParts (CompleteMechs)`, to help pick the correct parts you need.  

(optional) Modified salvage generation to be based on remaining structure points. It sums up all structure points (with center torso using a factor of StructurePointBasedSalvageHighPriorityFactor, the head getting ignored,
and the rest a factor of StructurePointBasedSalvageLowPriorityFactor) and calculates a percentage of it. 100% means you get maximum parts (which is the lower one of StructurePointBasedSalvageMaxPartsFromMech and your difficulty setting),
0% means you get nothing, unless StructurePointBasedSalvageMinPartsFromMech is set >0, in which case you get always at least this much parts from a mech.  

(optional with CustomComponents present) Documentation of Customs used by SMA:
```
"AssemblyVariant": {
	"PrefabID": "Ostroc",
	"Exclude": false,
	"AssemblyAllowedWith": [ "Ostwar" ],
	"KnownOmniVariant": false,
	"Lootable": "mechdef_ostsol_OTL-4D"
},
```
Variant specification for ChassisDefs. AssemblyVariant and PrefabID are choosen to make a change to/from CustomSalvage easier.  
PrefabID should be set to the UIName of the variant you want this chassis to belong to.  
Exclude disables crossassembly for this chassis.  
AssemblyAllowedWith allow variant assembly with these without checking other conditions except Exclude (works both ways, but add it to both sides to make it more clear what happens).  
KnownOmniVariant sets this omni variants known status if no parts are preset.  
Lootable set a mechdef id to override generated salvage.  
```
"VAssemblyVariant": {
	"PrefabID": "APC",
	"Exclude": false
},
```
variant specification for VehicleChassisDefs, required for crossassembly (vehicle salvaging and assembly requires CustomUnits).  
`"Flags": { "flags" : ["no_salvage"] }` default CustomComponents Flags, no_salvage gets checked for MechComponentDefs (only StructurePointBasedSalvage)  
`"Lootable": { "ItemID": "Weapon_Autocannon_AC2-STOCK" }` CustomComponents Lootable, gets checked for MechComponentDefs (only StructurePointBasedSalvage)  
