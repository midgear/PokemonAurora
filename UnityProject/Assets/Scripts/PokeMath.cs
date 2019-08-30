using UnityEngine;
using PokemonHeader;

// NOTE(BluCloos): I am really annoyed at C# for not giving me the ability to make
// floating functions. Heck, that's some stupid shit right there mans. 
public static class PokeMath
{
    /// <summary>
    /// This function is used to calculate a single stat value for a pokemon. 
    /// It can be used to calculate attack, defense, special attack, special defense, 
    /// and speed.
    /// </summary>
    public static int CalculateStat(byte baseStat, 
                                    byte individualValue, 
                                    byte effortValue, 
                                    float natureMod, 
                                    int level)
    {
        float w1 = 2.0f * (float)baseStat + (float)individualValue + (float)effortValue;
        float w2 = w1 * (float)level / 100.0f + 5.0f;
        float w3 = Mathf.Floor(w2) * natureMod;
        return Mathf.FloorToInt(w3);
    }

    /// <summary>
    /// This function is used to calculate the HP stat value for a pokemon.
    /// </summary>
    public static int CalculateHp(byte baseStat,
                                  byte individualValue,
                                  byte effortValue,
                                  int level)
    {
        float w1 = 2.0f * (float)baseStat + (float)individualValue + (float)effortValue;
        float w2 = w1 * (float)level / 100.0f + (float)level + 10.0f;
        return Mathf.FloorToInt(w2);
    }

    /// <summary>
    /// Given the level of a pokemon in the fast leveling group, this function will return 
    /// the base amount of experience points for said level.
    /// </summary>
    public static uint ExpFromLevelFast(int level)
    {
        float top = 4.0f * (level * level * level);
        return (uint)(top / 5.0f);
    }

    /// <summary>
    /// Given the level of a pokemon in the medium fast leveling group, this function will return 
    /// the base amount of experience points for said level.
    /// </summary>
    public static uint ExpFromLevelMediumFast(int level)
    {
        return (uint)(level * level * level);
    }

    /// <summary>
    /// Given the level of a pokemon in the medium slow leveling group, this function will return 
    /// the base amount of experience points for said level.
    /// </summary>
    public static uint ExpFromLevelMediumSlow(int level)
    {
        float term1 = (6.0f / 5.0f) * (level * level * level);
        float term2 = 15.0f * (level * level);
        float term3 = 100.0f * level;
        return (uint)(term1 - term2 + term3 - 140.0f);
    }

    /// <summary>
    /// Given the level of a pokemon in the medium fast leveling group, this function will return 
    /// the base amount of experience points for said level.
    /// </summary>
    public static uint ExpFromLevelSlow(int level)
    {
        float top = 5.0f * (level * level * level);
        return (uint)(top / 4.0f);
    }

    /// <summary>
    /// Given the level of a pokemon and the leveling group, this function will return the base amount
    /// of experience points for said level.
    /// </summary>
    public static uint ExpFromLevel(int level, string levelingType)
    {
        switch(levelingType.ToLower())
        {
            case "fast":
                return ExpFromLevelFast(level);
            case "medium fast":
                return ExpFromLevelMediumFast(level);
            case "medium slow":
                return ExpFromLevelMediumSlow(level);
            case "slow":
                return ExpFromLevelSlow(level);
            default:
                Debug.LogWarning("Leveling type not recognized! Defaulting to fast group");
                return ExpFromLevelFast(level);
        }
    }

    /// <summary>
    /// This function will calculate the amount of damage to apply given the attacking Pokemon, 
    /// the defending Pokemon, and the move used.
    /// </summary>
    public static int CalculateDamage(pokemon_profile attacker, pokemon_profile defender, PokemonMoveData moveUsed)
    {
        // TODO(BluCloos): Add weather mods and critical hits!
        if (moveUsed != null)
        {
            PokemonData attackerData = GameManager.GetPokemonData(attacker.auroraDexNumber);
            PokemonData defenderData = GameManager.GetPokemonData(defender.auroraDexNumber);

            float w1 = 2.0f * attacker.level / 5.0f + 2.0f;
            float a = (moveUsed.moveClass == pokemon_move_class.MELEE) ? attacker.cAttackStat : attacker.cSpAttackStat;
            float d = (moveUsed.moveClass == pokemon_move_class.MELEE) ? defender.cDefenseStat : defender.cSpDefenseStat;
            float w2 = (w1 * moveUsed.basePower * a / d) / 50.0f + 2.0f;


            float stab = 1.0f;
            if (attackerData != null)
                stab = (moveUsed.type == attackerData.type1 || moveUsed.type == attackerData.type2) ? 1.5f : 1.0f;

            float type = 1.0f;
            if (defenderData != null)
            {
                type *= GameManager.GetTypeMod(moveUsed.type, defenderData.type1);
                type *= GameManager.GetTypeMod(moveUsed.type, defenderData.type2);
            }

            float random = Random.Range(0.85f, 1.0f);
            float m = 1.0f * stab * random * type;
            return (int)(w2 * m);
        }
        else
        {
            Debug.LogWarning("Passed null move");
            return 0;
        }
    }

    /// <summary>
    /// Run this function to verify the integrity of all the functions in this class. 
    /// It doesn't return anything, it simply prints out the results of a bunch of test cases.
    /// </summary>
    public static void DebugVerify()
    {
        Debug.Log("Testing the calculate damage function!");
        pokemon_profile attackingBoi = GameManager.CreateNewPokemonProfile(100, 56);
        pokemon_profile defendingBoi = GameManager.CreateNewPokemonProfile(100, 60);
        PokemonMoveData move = GameManager.GetPokemonMoveData("Tackle");
        float damage = CalculateDamage(attackingBoi, defendingBoi, move);

        Debug.Log("Damage: " + damage);
        GameManager.DebugPrintPokemonMove(move);
        GameManager.DebugPrintPokemon(attackingBoi);
        GameManager.DebugPrintPokemon(defendingBoi);

        Debug.Log("Done testing that shit.");    
    }
}
