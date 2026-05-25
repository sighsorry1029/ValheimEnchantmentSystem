using ItemDataManager;
using BepInEx.Configuration;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using Object = UnityEngine.Object;

namespace kg.ValheimEnchantmentSystem;

[VES_Autoload(VES_Autoload.Priority.Normal, "OnInit", typeof(SyncedData))]
public static class Enchantment_VFX 
{ 
    private static readonly int TintColor = Shader.PropertyToID("_TintColor");
    private const int MainVfxParticleBrightnessOrder = 98;
    private const int MainVfxLightIntensityOrder = 99;
    private const int MainVfxTintIntensityOrder = 100;
    private const int ArmorVfxTintIntensityOrder = 101;
    private const int EnableWeaponVfxOrder = 102;
    private const int EnableArmorVfxOrder = 103;
    private const string ManagedLightName = "VES_Light";
    private const string MeshParticleEffectPrefix = "VES_MPE_";
    private static readonly MaterialPropertyBlock PropertyBlock = new();

    private static GameObject VES_MPE;
    private static readonly float[] VariantTintMultipliers = { 1f, 0.65f, 1.8f, 0.35f };

    public static readonly List<Material> VFXs = new List<Material>();

    public static ConfigEntry<bool> _enableWeaponVFX;
    public static ConfigEntry<bool> _enableArmorVFX;
    public static ConfigEntry<float> _mainVfxTintIntensity;
    public static ConfigEntry<float> _mainVfxLightIntensity;
    public static ConfigEntry<float> _mainVfxParticleBrightness;
    public static ConfigEntry<float> _armorVfxTintIntensity;
    private static int _visualConfigRevision;
    private static bool _sceneVisualRefreshScheduled;

    private sealed class ManagedMeshEffectState : MonoBehaviour
    {
        public RendererEntry[] Entries = Array.Empty<RendererEntry>();
        public Light? ManagedLight;
        public Color LastColor = Color.clear;
        public int LastVariant = -1;
        public int LastConfigRevision = -1;
        public bool LastEnabled;
        public bool LastIsArmor = true;
    }

    private sealed class RendererEntry
    {
        public Renderer? Renderer;
        public int RendererInstanceId;
        public int ManagedMaterialIndex = -1;
        public GameObject? ParticleRoot;
        public ParticleSystem[] ParticleSystems = Array.Empty<ParticleSystem>();
        public float[] BaseStartSizeMultipliers = Array.Empty<float>();

        public RendererEntry(Renderer renderer)
        {
            Renderer = renderer;
            RendererInstanceId = renderer.GetInstanceID();
        }
    }

    private static bool IsManagedVfxMaterial(Material? material)
    {
        return material != null && (VFXs.Contains(material) || material.name.IndexOf("Enchantment_VFX_Mat", StringComparison.Ordinal) >= 0);
    }

    private static int FindManagedMaterialIndex(Material[] materials)
    {
        for (int i = 0; i < materials.Length; ++i)
        {
            if (IsManagedVfxMaterial(materials[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static int EnsureRendererVfxMaterial(RendererEntry entry, int variant)
    {
        if (entry.Renderer == null)
        {
            entry.ManagedMaterialIndex = -1;
            return -1;
        }

        Material[] sharedMaterials = entry.Renderer.sharedMaterials ?? Array.Empty<Material>();
        int existingIndex = entry.ManagedMaterialIndex;
        if (existingIndex < 0 || existingIndex >= sharedMaterials.Length || !IsManagedVfxMaterial(sharedMaterials[existingIndex]))
        {
            existingIndex = FindManagedMaterialIndex(sharedMaterials);
        }

        if (existingIndex >= 0)
        {
            if (sharedMaterials[existingIndex] != VFXs[variant])
            {
                Material[] updatedMaterials = (Material[])sharedMaterials.Clone();
                updatedMaterials[existingIndex] = VFXs[variant];
                entry.Renderer.sharedMaterials = updatedMaterials;
            }

            entry.ManagedMaterialIndex = existingIndex;
            return existingIndex;
        }

        Material[] expandedMaterials = new Material[sharedMaterials.Length + 1];
        if (sharedMaterials.Length > 0)
        {
            Array.Copy(sharedMaterials, expandedMaterials, sharedMaterials.Length);
        }

        expandedMaterials[sharedMaterials.Length] = VFXs[variant];
        entry.Renderer.sharedMaterials = expandedMaterials;
        entry.ManagedMaterialIndex = sharedMaterials.Length;
        return entry.ManagedMaterialIndex;
    }

    private static void RemoveRendererVfxMaterial(RendererEntry entry)
    {
        if (entry.Renderer == null)
        {
            entry.ManagedMaterialIndex = -1;
            return;
        }

        Material[] sharedMaterials = entry.Renderer.sharedMaterials ?? Array.Empty<Material>();
        if (sharedMaterials.Length == 0)
        {
            entry.ManagedMaterialIndex = -1;
            return;
        }

        int existingIndex = entry.ManagedMaterialIndex;
        if (existingIndex < 0 || existingIndex >= sharedMaterials.Length || !IsManagedVfxMaterial(sharedMaterials[existingIndex]))
        {
            existingIndex = FindManagedMaterialIndex(sharedMaterials);
        }

        if (existingIndex < 0)
        {
            entry.ManagedMaterialIndex = -1;
            return;
        }

        if (sharedMaterials.Length == 1)
        {
            entry.Renderer.sharedMaterials = Array.Empty<Material>();
        }
        else
        {
            Material[] filteredMaterials = new Material[sharedMaterials.Length - 1];
            if (existingIndex > 0)
            {
                Array.Copy(sharedMaterials, 0, filteredMaterials, 0, existingIndex);
            }

            if (existingIndex < sharedMaterials.Length - 1)
            {
                Array.Copy(sharedMaterials, existingIndex + 1, filteredMaterials, existingIndex, sharedMaterials.Length - existingIndex - 1);
            }

            entry.Renderer.sharedMaterials = filteredMaterials;
        }

        PropertyBlock.Clear();
        entry.Renderer.SetPropertyBlock(PropertyBlock);
        entry.ManagedMaterialIndex = -1;
    }

    private static void SetRendererTint(Renderer renderer, Color tint)
    {
        PropertyBlock.Clear();
        renderer.GetPropertyBlock(PropertyBlock);
        PropertyBlock.SetColor(TintColor, tint);
        renderer.SetPropertyBlock(PropertyBlock);
    }

    private static GameObject GetOrCreateMeshParticleEffect(GameObject item, Renderer renderer)
    {
        string childName = MeshParticleEffectPrefix + renderer.GetInstanceID();
        Transform existing = item.transform.Find(childName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject vfx = Object.Instantiate(VES_MPE, item.transform);
        vfx.name = childName;
        vfx.transform.localPosition = Vector3.zero;
        vfx.transform.localRotation = Quaternion.identity;
        vfx.transform.localScale = Vector3.one;
        return vfx;
    }

    private static Light GetOrCreateManagedLight(GameObject item)
    {
        Transform existing = item.transform.Find(ManagedLightName);
        GameObject lightObject;
        if (existing != null)
        {
            lightObject = existing.gameObject;
        }
        else
        {
            lightObject = new GameObject(ManagedLightName);
            lightObject.transform.SetParent(item.transform, false);
        }

        return lightObject.GetComponent<Light>() ?? lightObject.AddComponent<Light>();
    }

    private static IEnumerable<Renderer> EnumerateEffectRenderers(GameObject item)
    {
        return item.GetComponentsInChildren<SkinnedMeshRenderer>(true).Cast<Renderer>()
            .Concat(item.GetComponentsInChildren<MeshRenderer>(true));
    }

    private static ManagedMeshEffectState GetOrCreateManagedMeshEffectState(GameObject item)
    {
        ManagedMeshEffectState state = item.GetComponent<ManagedMeshEffectState>();
        return state != null ? state : item.AddComponent<ManagedMeshEffectState>();
    }

    private static void EnsureParticleSystems(GameObject item, RendererEntry entry, Renderer renderer)
    {
        if (entry.ParticleRoot == null)
        {
            entry.ParticleRoot = GetOrCreateMeshParticleEffect(item, renderer);
        }

        if (entry.ParticleSystems.Length > 0 && entry.ParticleSystems.All(ps => ps != null))
        {
            return;
        }

        entry.ParticleSystems = entry.ParticleRoot.GetComponentsInChildren<ParticleSystem>(true);
        entry.BaseStartSizeMultipliers = new float[entry.ParticleSystems.Length];
        for (int i = 0; i < entry.ParticleSystems.Length; ++i)
        {
            entry.BaseStartSizeMultipliers[i] = entry.ParticleSystems[i].main.startSizeMultiplier;
        }
    }

    private static void SetParticleRootActive(RendererEntry entry, bool active)
    {
        if (entry.ParticleRoot != null && entry.ParticleRoot.activeSelf != active)
        {
            entry.ParticleRoot.SetActive(active);
        }
    }

    private static bool TryConfigureParticleEffects(GameObject item, RendererEntry entry, Renderer renderer, Color color)
    {
        bool isSkinned = renderer is SkinnedMeshRenderer;
        if (isSkinned)
        {
            SkinnedMeshRenderer? skinnedRenderer = renderer as SkinnedMeshRenderer;
            if (skinnedRenderer?.sharedMesh == null || !skinnedRenderer.sharedMesh.isReadable)
            {
                SetParticleRootActive(entry, false);
                return false;
            }
        }
        else
        {
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (!meshFilter || meshFilter.sharedMesh == null || !meshFilter.sharedMesh.isReadable)
            {
                SetParticleRootActive(entry, false);
                return false;
            }
        }

        EnsureParticleSystems(item, entry, renderer);
        SetParticleRootActive(entry, true);

        float boundsMagnitude = renderer.bounds.size.magnitude;
        float lossyMagnitude = item.transform.lossyScale.magnitude;
        if (lossyMagnitude <= 0f)
        {
            lossyMagnitude = 1f;
        }

        float sizeMultiplier = boundsMagnitude / lossyMagnitude;
        Color particleColor = GetParticleColor(color);

        for (int i = 0; i < entry.ParticleSystems.Length; ++i)
        {
            ParticleSystem particleSystem = entry.ParticleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particleSystem.main;
            main.startColor = particleColor;
            float baseStartSize = i < entry.BaseStartSizeMultipliers.Length ? entry.BaseStartSizeMultipliers[i] : main.startSizeMultiplier;
            main.startSizeMultiplier = baseStartSize * sizeMultiplier;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.shapeType = isSkinned ? ParticleSystemShapeType.SkinnedMeshRenderer : ParticleSystemShapeType.MeshRenderer;
            if (isSkinned)
            {
                shape.skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
                shape.meshRenderer = null;
            }
            else
            {
                shape.skinnedMeshRenderer = null;
                shape.meshRenderer = renderer as MeshRenderer;
            }

            particleSystem.gameObject.SetActive(true);
        }

        return true;
    }

    private static void DestroyRendererEntry(RendererEntry entry)
    {
        if (entry.ParticleRoot != null)
        {
            Object.Destroy(entry.ParticleRoot);
        }
    }

    private static void RebuildRendererEntries(ManagedMeshEffectState state, bool includeParticles)
    {
        GameObject item = state.gameObject;
        Dictionary<int, RendererEntry> existingEntries = new();
        foreach (RendererEntry entry in state.Entries)
        {
            if (entry.Renderer != null)
            {
                existingEntries[entry.RendererInstanceId] = entry;
            }
            else
            {
                DestroyRendererEntry(entry);
            }
        }

        Renderer[] renderers = EnumerateEffectRenderers(item).Where(renderer => renderer != null).ToArray();
        RendererEntry[] updatedEntries = new RendererEntry[renderers.Length];

        for (int i = 0; i < renderers.Length; ++i)
        {
            Renderer renderer = renderers[i];
            int rendererId = renderer.GetInstanceID();
            if (!existingEntries.TryGetValue(rendererId, out RendererEntry? entry))
            {
                entry = new RendererEntry(renderer);
            }
            else
            {
                existingEntries.Remove(rendererId);
                entry.Renderer = renderer;
            }

            entry.ManagedMaterialIndex = -1;
            if (!includeParticles)
            {
                SetParticleRootActive(entry, false);
            }

            updatedEntries[i] = entry;
        }

        foreach (RendererEntry leftoverEntry in existingEntries.Values)
        {
            DestroyRendererEntry(leftoverEntry);
        }

        state.Entries = updatedEntries;
    }

    private static void DisableManagedMeshEffect(ManagedMeshEffectState state)
    {
        if (state.ManagedLight != null)
        {
            state.ManagedLight.enabled = false;
            state.ManagedLight.gameObject.SetActive(false);
        }

        foreach (RendererEntry entry in state.Entries)
        {
            RemoveRendererVfxMaterial(entry);
            SetParticleRootActive(entry, false);
        }
    }

    private static void ApplyManagedMeshEffect(ManagedMeshEffectState state, Color color, int variant, bool isArmor)
    {
        if (state.Entries.Length == 0 || state.LastIsArmor != isArmor || state.Entries.Any(entry => entry.Renderer == null))
        {
            RebuildRendererEntries(state, includeParticles: !isArmor);
        }

        if (!isArmor)
        {
            Light light = state.ManagedLight != null ? state.ManagedLight : GetOrCreateManagedLight(state.gameObject);
            state.ManagedLight = light;
            light.gameObject.SetActive(true);
            light.enabled = true;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = _mainVfxLightIntensity.Value * color.a;
            light.range = 9f;
        }
        else if (state.ManagedLight != null)
        {
            state.ManagedLight.enabled = false;
            state.ManagedLight.gameObject.SetActive(false);
        }

        Color tint = isArmor ? color * GetArmorTintIntensity(variant) : color * GetTintIntensity(variant);
        foreach (RendererEntry entry in state.Entries)
        {
            Renderer? renderer = entry.Renderer;
            if (renderer == null)
            {
                continue;
            }

            EnsureRendererVfxMaterial(entry, variant);
            SetRendererTint(renderer, tint);
            if (isArmor)
            {
                SetParticleRootActive(entry, false);
                continue;
            }

            TryConfigureParticleEffects(state.gameObject, entry, renderer, color);
        }
    }

    private static void SetMeshEffectState(GameObject item, bool enabled, Color color, int variant, bool isArmor)
    {
        if (!item)
        {
            return;
        }

        ManagedMeshEffectState? state = item.GetComponent<ManagedMeshEffectState>();
        bool shouldEnable = enabled && VFXs.Count > 0 && IsEffectEnabledForClass(isArmor);
        if (!shouldEnable)
        {
            if (state == null)
            {
                return;
            }

            if (state.LastEnabled || state.LastConfigRevision != _visualConfigRevision)
            {
                DisableManagedMeshEffect(state);
            }

            state.LastEnabled = false;
            state.LastColor = color;
            state.LastVariant = variant;
            state.LastIsArmor = isArmor;
            state.LastConfigRevision = _visualConfigRevision;
            return;
        }

        state ??= GetOrCreateManagedMeshEffectState(item);
        variant = Mathf.Clamp(variant, 0, VFXs.Count - 1);
        if (state.LastEnabled &&
            state.LastVariant == variant &&
            state.LastIsArmor == isArmor &&
            state.LastConfigRevision == _visualConfigRevision &&
            state.LastColor.Equals(color))
        {
            return;
        }

        ApplyManagedMeshEffect(state, color, variant, isArmor);
        state.LastEnabled = true;
        state.LastColor = color;
        state.LastVariant = variant;
        state.LastIsArmor = isArmor;
        state.LastConfigRevision = _visualConfigRevision;
    }

    internal static void RemoveMeshEffects(GameObject item)
    {
        if (!item)
        {
            return;
        }

        if (item.GetComponent<ManagedMeshEffectState>() is { } state)
        {
            DisableManagedMeshEffect(state);
            foreach (RendererEntry entry in state.Entries)
            {
                DestroyRendererEntry(entry);
            }

            if (state.ManagedLight != null)
            {
                Object.Destroy(state.ManagedLight.gameObject);
            }

            Object.Destroy(state);
        }

        foreach (Transform child in item.transform.Cast<Transform>()
                     .Where(child => child.name == ManagedLightName || child.name.StartsWith(MeshParticleEffectPrefix, StringComparison.Ordinal))
                     .ToArray())
        {
            Object.Destroy(child.gameObject);
        }

        foreach (Renderer renderer in EnumerateEffectRenderers(item))
        {
            Material[] sharedMaterials = renderer.sharedMaterials ?? Array.Empty<Material>();
            int managedMaterialIndex = FindManagedMaterialIndex(sharedMaterials);
            if (managedMaterialIndex < 0)
            {
                continue;
            }

            Material[] filteredMaterials = new Material[sharedMaterials.Length - 1];
            if (managedMaterialIndex > 0)
            {
                Array.Copy(sharedMaterials, 0, filteredMaterials, 0, managedMaterialIndex);
            }

            if (managedMaterialIndex < sharedMaterials.Length - 1)
            {
                Array.Copy(sharedMaterials, managedMaterialIndex + 1, filteredMaterials, managedMaterialIndex, sharedMaterials.Length - managedMaterialIndex - 1);
            }

            renderer.sharedMaterials = filteredMaterials;
            PropertyBlock.Clear();
            renderer.SetPropertyBlock(PropertyBlock);
        }
    }

    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order;
    }

    private static ConfigDescription OrderedDescription(string description, int order) => new(
        description,
        null,
        new ConfigurationManagerAttributes { Order = order });

    private static ConfigDescription OrderedRangeDescription(string description, int order, float min, float max) => new(
        description,
        new AcceptableValueRange<float>(min, max),
        new ConfigurationManagerAttributes { Order = order });
    
    [UsedImplicitly]
    private static void OnInit()
    {
        if (ValheimEnchantmentSystem.NoGraphics) return;
        VFXs.Add(ValheimEnchantmentSystem._asset.LoadAsset<Material>("Enchantment_VFX_Mat1"));
        VFXs.Add(ValheimEnchantmentSystem._asset.LoadAsset<Material>("Enchantment_VFX_Mat2"));
        VFXs.Add(ValheimEnchantmentSystem._asset.LoadAsset<Material>("Enchantment_VFX_Mat3")); 
        VFXs.Add(ValheimEnchantmentSystem._asset.LoadAsset<Material>("Enchantment_VFX_Mat4"));
        VES_MPE = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("VES_MPE");
        _mainVfxParticleBrightness = ValheimEnchantmentSystem.ClientConfig(
            "",
            "MainVFXParticleBrightness",
            1.5f,
            OrderedRangeDescription("Brightness of weapon/world/stand particle VFX.", MainVfxParticleBrightnessOrder, 0f, 5f));
        _mainVfxLightIntensity = ValheimEnchantmentSystem.ClientConfig(
            "",
            "MainVFXLightIntensity",
            1f,
            OrderedRangeDescription("Light intensity added by weapon/world/stand VFX.", MainVfxLightIntensityOrder, 0f, 5f));
        _mainVfxTintIntensity = ValheimEnchantmentSystem.ClientConfig(
            "",
            "MainVFXTintIntensity",
            8f,
            OrderedRangeDescription("Emission intensity of weapon/world/stand VFX materials.", MainVfxTintIntensityOrder, 0f, 50f));
        _armorVfxTintIntensity = ValheimEnchantmentSystem.ClientConfig(
            "",
            "ArmorVFXTintIntensity",
            2f,
            OrderedRangeDescription("Emission intensity of armor/cape/utility VFX materials.", ArmorVfxTintIntensityOrder, 0f, 20f));
        _enableWeaponVFX = ValheimEnchantmentSystem.ClientConfig("", "EnableWeaponVFX", true, OrderedDescription("Enable enchantment VFX for held items like weapons, shields, tools, torches, and their stands.", EnableWeaponVfxOrder));
        _enableArmorVFX = ValheimEnchantmentSystem.ClientConfig("", "EnableArmorVFX", true, OrderedDescription("Enable enchantment VFX for worn items like armor, capes, utility items, and their stands.", EnableArmorVfxOrder));
        _enableWeaponVFX.SettingChanged += (_, _) => OnEquipmentVisualSettingChanged();
        _enableArmorVFX.SettingChanged += (_, _) => OnEquipmentVisualSettingChanged();
        _mainVfxParticleBrightness.SettingChanged += (_, _) => OnEquipmentVisualSettingChanged();
        _mainVfxLightIntensity.SettingChanged += (_, _) => OnEquipmentVisualSettingChanged();
        _mainVfxTintIntensity.SettingChanged += (_, _) => OnEquipmentVisualSettingChanged();
        _armorVfxTintIntensity.SettingChanged += (_, _) => OnEquipmentVisualSettingChanged();
    }

    private static void OnEquipmentVisualSettingChanged()
    {
        unchecked
        {
            ++_visualConfigRevision;
        }

        RequestSceneVisualRefresh();
    }

    internal static void RequestSceneVisualRefresh()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        if (ValheimEnchantmentSystem._thistype == null)
        {
            RefreshSceneVisuals();
            return;
        }

        if (_sceneVisualRefreshScheduled)
        {
            return;
        }

        _sceneVisualRefreshScheduled = true;
        ValheimEnchantmentSystem._thistype.DelayedInvoke(() =>
        {
            _sceneVisualRefreshScheduled = false;
            RefreshSceneVisuals();
        }, 1);
    }

    internal static bool IsWeaponVfxEnabled()
    {
        return _enableWeaponVFX.Value;
    }

    internal static bool IsArmorVfxEnabled()
    {
        return _enableArmorVFX.Value;
    }

    internal static bool IsEffectEnabledForClass(bool isArmor)
    {
        return isArmor ? IsArmorVfxEnabled() : IsWeaponVfxEnabled();
    }

    internal static void InsertColor(ZDO zdo, string key, string color, int variant)
    {
        if (zdo == null)
        {
            return;
        }

        color ??= string.Empty;
        variant = Mathf.Max(0, variant);

        if (!string.Equals(zdo.GetString(key), color, StringComparison.Ordinal))
        {
            zdo.Set(key, color);
        }

        string variantKey = key + "_variant";
        if (zdo.GetInt(variantKey) != variant)
        {
            zdo.Set(variantKey, variant);
        }
    }

    internal static string GetItemEnchantmentColor(ItemDrop.ItemData item, out int variant, bool trimAlpha = false)
    {
        variant = 0;
        if (item?.Data().Get<Enchantment_Core.Enchanted>() is { level: > 0 } en)
        {
            return SyncedData.GetColor(en, out variant, trimAlpha);
        }

        return string.Empty;
    }

    internal static bool TryGetVisSlotColorKey(VisSlot slot, out string colorKey, out bool isArmor)
    {
        switch (slot)
        {
            case VisSlot.HandLeft:
                colorKey = "VES_leftitemColor";
                isArmor = false;
                return true;
            case VisSlot.HandRight:
                colorKey = "VES_rightitemColor";
                isArmor = false;
                return true;
            case VisSlot.BackLeft:
                colorKey = "VES_leftbackitemColor";
                isArmor = false;
                return true;
            case VisSlot.BackRight:
                colorKey = "VES_rightbackitemColor";
                isArmor = false;
                return true;
            case VisSlot.Chest:
                colorKey = "VES_chestitemColor";
                isArmor = true;
                return true;
            case VisSlot.Legs:
                colorKey = "VES_legsitemColor";
                isArmor = true;
                return true;
            case VisSlot.Helmet:
                colorKey = "VES_helmetitemColor";
                isArmor = true;
                return true;
            case VisSlot.Shoulder:
                colorKey = "VES_shoulderitemColor";
                isArmor = true;
                return true;
            default:
                colorKey = string.Empty;
                isArmor = true;
                return false;
        }
    }

    internal static bool IsArmorVisual(ItemDrop.ItemData itemData)
    {
        if (itemData == null)
        {
            return true;
        }

        return itemData.m_shared.m_itemType switch
        {
            ItemDrop.ItemData.ItemType.Tool => false,
            ItemDrop.ItemData.ItemType.Torch => false,
            ItemDrop.ItemData.ItemType.OneHandedWeapon => false,
            ItemDrop.ItemData.ItemType.Shield => false,
            ItemDrop.ItemData.ItemType.Bow => false,
            ItemDrop.ItemData.ItemType.TwoHandedWeapon => false,
            ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft => false,
            ItemDrop.ItemData.ItemType.Helmet => true,
            ItemDrop.ItemData.ItemType.Chest => true,
            ItemDrop.ItemData.ItemType.Legs => true,
            ItemDrop.ItemData.ItemType.Shoulder => true,
            ItemDrop.ItemData.ItemType.Utility => true,
            _ => !itemData.IsWeapon()
        };
    }

    private static float GetTintIntensity(int variant)
    {
        variant = Mathf.Clamp(variant, 0, VariantTintMultipliers.Length - 1);
        return _mainVfxTintIntensity.Value * VariantTintMultipliers[variant];
    }

    private static float GetArmorTintIntensity(int variant)
    {
        variant = Mathf.Clamp(variant, 0, VariantTintMultipliers.Length - 1);
        return _armorVfxTintIntensity.Value * VariantTintMultipliers[variant];
    }

    private static Color GetParticleColor(Color color)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        float brightness = Mathf.Clamp01(v * _mainVfxParticleBrightness.Value);
        Color particleColor = Color.HSVToRGB(h, s, brightness, false);
        particleColor.a = color.a;
        return particleColor;
    }

    public static void RefreshEquipmentVisuals()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        ArmorStandVfx.RefreshAllArmorStandVisuals();
        EquipmentWorldVfx.RefreshEquipmentVisuals();
    }

    public static void RefreshArmorStandVisuals(ArmorStand armorStand)
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        ArmorStandVfx.RefreshArmorStandVisuals(armorStand);
    }

    public static void RefreshWorldItemVisuals()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        EquipmentWorldVfx.RefreshWorldItemVisuals();
    }

    public static void RefreshSceneVisuals()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        RefreshEquipmentVisuals();
        RefreshWorldItemVisuals();
    }

    public static void RefreshLiveVisuals()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        UpdateGrid();
        RefreshSceneVisuals();
    }

    public static void AttachMeshEffect(GameObject item, Color c, int variant, bool isArmor = false)
    {
        SetMeshEffectState(item, enabled: true, c, variant, isArmor);
    }

    internal static void DisableMeshEffect(GameObject item, bool isArmor = false)
    {
        SetMeshEffectState(item, enabled: false, Color.clear, 0, isArmor);
    }

    public static void UpdateGrid()
    {
        InventoryOverlayVfx.UpdateGrid();
    }
}
