# Standalone Mod Patches

This directory contains standalone BepInEx plugins that fix Settings UI conflicts with other mods.

## 📦 Patches Included

### 1. **ArcaneWardPatch.cs**
- **Purpose**: Prevents ArcaneWard mod from using Valheim's Settings UI (which causes crashes)
- **Function**:
  - Disables ArcaneWard's Settings tab integration
  - Mirrors all ArcaneWard settings to its own config file
  - Keeps settings synchronized in both directions

### 2. **BlueprintPatch.cs**
- **Purpose**: Prevents Blueprint mod from using Valheim's Settings UI (which causes crashes)
- **Function**:
  - Disables Blueprint's Settings tab integration
  - Mirrors all Blueprint settings to its own config file
  - Keeps settings synchronized in both directions

## 🚀 How It Works

These patches run as **separate BepInEx plugins** that:

1. **Auto-detect** if ArcaneWard or Blueprint mods are installed
2. **Remove** their Settings UI patches that cause `SettingsBase` errors
3. **Create** mirrored configs in their own config files
4. **Sync** changes bidirectionally between the original mod and the patch

## 📝 Config Files Generated

When active, these patches create:

- `BepInEx/config/kg.ArcaneWardPatch.cfg` - All ArcaneWard settings
- `BepInEx/config/kg.BlueprintPatch.cfg` - All Blueprint settings

Use **BepInEx Configuration Manager** (F1) to edit these settings.

## ✅ Benefits

- ✅ No more `SettingsBase` crashes
- ✅ Settings still fully functional via BepInEx Config Manager
- ✅ Automatic bidirectional sync
- ✅ Works as standalone patches (can be distributed separately)
- ✅ Soft dependencies - won't error if target mods aren't installed

## 🔧 Usage

These patches are automatically compiled as part of the main mod. They run independently and will only activate if the target mods are present.

## 📜 License

Part of the Valheim Enchantment System mod.
