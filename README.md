# BTSimpleMechAssembly
Mod for BattleTech

Changes how mechs are assembled.
Allows Crossvariant Assembly (requires machine shop argo upgrade by default) to combine parts of different mech variants to one mech.
You are no longer forced to assemble mechs on gaining parts. Instead you get asked if you want to assemble a mech (and which if you have CrossvariantAssembly) and if it should go into storage or to you active mechs.
Manually start mech assembly by selecting parts and use the ready mech button.
CrossvariantAssembly can be configuried to require same speed/tonnage and matching tags.
Optional omnimech support. On readying an omnimech you can choose which variant you want to get (provided you already have seen that variant at least once).
Assembled mechs can be required to be readied before use, or be instantly ready.
If you use equiped mechs game rule, you gain the mech equipment in any case (goes into storage if you assemble and put the mech into storage).
More intelligent Assembly algorythm. First uses the parts of the mech you want to assemble, and fills missing parts from Crossvariants (if enabled&unlocked) trying to leave at least one part of every variant remaining.

(optional) Modified salvage generation to be based on remaining structure points. It sums up all structure points (with head and center torso using a factor of StructurePointBasedSalvageHighPriorityFactor,
and the rest a factor of StructurePointBasedSalvageLowPriorityFactor) and calculates a percentage of it. 100% means you get maximum parts (which is the lower one of StructurePointBasedSalvageMaxPartsFromMech and your difficulty setting),
0% means you get nothing, unless StructurePointBasedSalvageMinPartsFromMech is set >0, in which case you get always at least this much parts from a mech.
Mechcomponents (upgrades&weapons) are generated as usual, but you cannot get randomly upgraded Mechcomponents from destroying a mech with basic Mechcomponents.
