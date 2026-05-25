namespace kg.ValheimEnchantmentSystem.Items_Structures;

internal static class ScrollRecipeCatalog
{
    internal readonly struct RecipeData
    {
        public readonly int Amount;
        public readonly string[] Requirements;

        public RecipeData(int amount, params string[] requirements)
        {
            Amount = amount;
            Requirements = requirements;
        }
    }

    private static readonly Dictionary<char, RecipeData> DefaultRecipesWeapon = new()
    {
        { 'F', new RecipeData(6, "TrophyNeck,1", "Dandelion,6", "Wood,24", "Stone,12") },
        { 'E', new RecipeData(3, "TrophyDraugr,1", "Entrails,3", "ElderBark,12", "Guck,6") },
        { 'D', new RecipeData(3, "TrophyWolf,1", "WolfPelt,3", "Crystal,6", "Obsidian,9") },
        { 'C', new RecipeData(6, "TrophyDeathsquito,1", "Needle,6", "FineWood,24", "Tar,12") },
        { 'B', new RecipeData(6, "TrophySeeker,1", "Carapace,12", "YggdrasilWood,18", "BlackMarble,24") },
        { 'A', new RecipeData(3, "TrophyVolture,1", "ProustitePowder,6", "Blackwood,12", "Grausten,24") },
        { 'S', new RecipeData(1, "TrophyFader,1") }
    };

    private static readonly Dictionary<char, RecipeData> DefaultRecipesWeaponBlessed = new()
    {
        { 'F', new RecipeData(2, "TrophyBjorn,1", "BjornPaw,4", "BoneFragments,12", "Bronze,8") },
        { 'E', new RecipeData(2, "TrophyDraugrElite,1", "Bloodbag,4", "Chain,2", "Iron,8") },
        { 'D', new RecipeData(4, "TrophyFenring,1", "WolfFang,8", "JuteRed,4", "Silver,16") },
        { 'C', new RecipeData(4, "TrophyBjornUndead,1", "UndeadBjornRibcage,8", "LinenThread,24", "BlackMetal,16") },
        { 'B', new RecipeData(1, "TrophyGjall,1", "Bilebag,2", "BlackCore,1", "Eitr,4") },
        { 'A', new RecipeData(4, "TrophyFallenValkyrie,1", "CelestialFeather,12", "BonemawSerpentTooth,6", "FlametalNew,16") },
        { 'S', new RecipeData(1, "TrophyFader,1") }
    };

    private static readonly Dictionary<char, RecipeData> DefaultRecipesArmor = new()
    {
        { 'F', new RecipeData(2, "TrophyBoar,1", "LeatherScraps,2", "Resin,6", "Flint,4") },
        { 'E', new RecipeData(3, "TrophyBlob,1", "Ooze,6", "ElderBark,12", "Guck,6") },
        { 'D', new RecipeData(3, "TrophyHatchling,1", "FreezeGland,3", "Crystal,6", "Obsidian,9") },
        { 'C', new RecipeData(3, "TrophyGoblin,1", "Coins,30", "FineWood,12", "Tar,6") },
        { 'B', new RecipeData(6, "TrophyTick,1", "GiantBloodSack,6", "YggdrasilWood,18", "BlackMarble,24") },
        { 'A', new RecipeData(3, "TrophyAsksvin,1", "SulfurStone,3", "Blackwood,12", "Grausten,24") },
        { 'S', new RecipeData(1, "TrophyFader,1") }
    };

    private static readonly Dictionary<char, RecipeData> DefaultRecipesArmorBlessed = new()
    {
        { 'F', new RecipeData(1, "TrophyFrostTroll,1", "TrollHide,5", "Ectoplasm,1", "Bronze,4") },
        { 'E', new RecipeData(1, "TrophyAbomination,1", "Root,5", "Chitin,2", "Iron,4") },
        { 'D', new RecipeData(4, "TrophySGolem,1", "WolfClaw,2", "WolfHairBundle,8", "Silver,16") },
        { 'C', new RecipeData(2, "TrophyLox,1", "LoxPelt,6", "BarleyFlour,12", "BlackMetal,8") },
        { 'B', new RecipeData(4, "TrophySeekerBrute,1", "Mandible,4", "JuteBlue,6", "Eitr,16") },
        { 'A', new RecipeData(4, "TrophyMorgen,1", "MorgenHeart,4", "CharredBone,24", "FlametalNew,16") },
        { 'S', new RecipeData(1, "TrophyFader,1") }
    };

    public static int GetBlessedConvertAmount(char tier) => 1;

    public static int GetBlessedConvertRequirement() => 12;

    public static RecipeData GetRecipe(char tier, bool blessed, bool isArmor)
    {
        Dictionary<char, RecipeData> recipes = isArmor
            ? blessed ? DefaultRecipesArmorBlessed : DefaultRecipesArmor
            : blessed ? DefaultRecipesWeaponBlessed : DefaultRecipesWeapon;
        return recipes[tier];
    }
}
