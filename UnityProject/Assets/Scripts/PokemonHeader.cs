using System.Collections.Generic; 

/// <summary>
/// This namespace contains all the structures and classes that aren't derived from Monobehaviour. Basically,
/// they are just data and don't have many methods associated with them.
/// </summary>
namespace PokemonHeader
{
    /// <summary>
    /// Enumeration for all the different Pokemon types.
    /// </summary>
    public enum pokemon_type
    {
        NORMAL = 0,
        FIRE,
        WATER,
        ELECTRIC,
        GRASS,
        ICE,
        FIGHTING,
        POISON,
        GROUND,
        FLYING,
        PSYCHIC,
        BUG,
        ROCK,
        GHOST,
        DRAGON,
        DARK,
        STEEL,
        FAIRY,
        UNKNOWN,
        Count
    };

    /// <summary>
    /// Enumeration for all the different move types.
    /// </summary>
    public enum pokemon_move_class
    {
        MELEE,
        PROJECTILE,
        AOE
    };

    /// <summary>
    /// This structure is the information pertaining to a single move / attack. Like 
    /// the pokemon_data class, this information is considered to be global.
    /// </summary>
    public class PokemonMoveData
    {
        // TODO(BluCloos): Status effects of moves
        public string name;
        public pokemon_type type;
        public pokemon_move_class moveClass;
        public int powerPoints;
        public int basePower;
        public int accuracy;
    };

    // TODO(BluCloos): I can't seem to find the right name for this...
    
    /// <summary>
    /// This structure is used by the pokemon_data class to describe which moves the 
    /// pokemon may learn and at what level. 
    /// </summary>
    public struct pokemon_move_meta
    {
        public string moveName;
        public int levelLearnedAt;
    };

    // TODO(BluCloos): Every time the experience points increase, the level is recalculated. 
    // If it is such that the pokemon has gained a level, the stats are then updated. 
    // An event is then raised that the pokemon has gained a level!

    /// <summary>
    /// This structure is all data pertaining to a single Pokémon that is considered 
    /// local to that Pokémon. This is the information that is saved to disk for Pokémon
    /// that the player has caught.
    /// </summary>
    public struct pokemon_profile
    {
        public int auroraDexNumber;
        public uint experiencePoints;
        public int level;

        // These are indices into the learnedMoves array
        // An index of -1 means an empty move slot!
        public int[] moves;
        public List<string> learnedMoves;

        public byte hpIV;
        public byte attackIV;
        public byte defenseIV;
        public byte spAttackIV;
        public byte spDefenseIV;
        public byte speedIV;

        public byte hpEV;
        public byte attackEV;
        public byte defenseEV;
        public byte spAttackEV;
        public byte spDefenseEV;
        public byte speedEV;

        // These are the 'base' calculated stats. Not be confused with the actual stats.
        public int hpStat;
        public int attackStat;
        public int defenseStat;
        public int spAttackStat;
        public int spDefenseStat;
        public int speedStat;

        // These are the current stats. They derive from the base calculated stats.
        public int cHpStat;
        public int cAttackStat;
        public int cDefenseStat;
        public int cSpAttackStat;
        public int cSpDefenseStat;
        public int cSpeedStat;
    };

    /// <summary>
    /// This structure is all data pertaining to a single Pokémon that is considered 
    /// 'global'. 
    /// </summary>
    public class PokemonData
    {
        public string name;
        public pokemon_type type1;
        public pokemon_type type2;
        public float height;
        public float weight;
        public float walkingSpeed;
        public float runningSpeed;
        public byte hpBaseStat;
        public byte attackBaseStat;
        public byte defenseBaseStat;
        public byte spAttackBaseStat;
        public byte spDefenseBaseStat;
        public byte speedBaseStat;
        public string levelingType;
        public List<pokemon_move_meta> moves;

        // TODO(BluCloos): Is this value actually an integer from 0 through 255?
        public byte catchRate;
        
        // TODO(BluCloos): Once the debug view is up, this functionality won't be needed anymore. 
        // It will be at this point that all functionality will have been purged from this file.
        public override string ToString()
        {
            string pokemonInfo = "Name: " + name + "\nType1: " + type1 + "\nType2: " + type2
                + "\nHeight: " + height + "\nWeight: " + weight + "\nHP: " + hpBaseStat +
                "\nATTACK: " + attackBaseStat + "\nDEFENSE: " + defenseBaseStat + "\nSPATTACK: "
                + spAttackBaseStat + "\nSPDEFENSE: " + spDefenseBaseStat + "\nSPEED: " + speedBaseStat +
                "\nLeveling Type: " + levelingType + "\nCatch Rate: " + catchRate;
            string pokemonMoves = "";

            for (int i = 0; i < moves.Count; i++)
            {
                pokemon_move_meta moveMeta = moves[i];
                pokemonMoves += moveMeta.moveName + " @ " + moveMeta.levelLearnedAt + "\n";
            }

            return pokemonInfo + "\nMoves:\n" + pokemonMoves;
        }
    }
}


