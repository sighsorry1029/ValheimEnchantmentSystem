# Patch layout

This directory contains no C# patch implementations. It is not a standalone plugin project and produces no ArcaneWard or Blueprint patch DLLs or mirrored configuration files.

Valheim Enchantment System compiles its mod source into the main `kg.ValheimEnchantmentSystem` assembly. `Startup/PatchRegistry.cs` discovers the attributed Harmony patch classes alongside their owning features. The item data, localization, skill, and piece managers also register Harmony patches directly during static initialization.

Optional mod integrations live in `Integrations/`. The ArcaneWard and Blueprint soft dependency declarations in the main plugin do not implement the standalone Settings UI patches previously described here.

See [BUILDING.md](../BUILDING.md) for the build and packaging procedure.
