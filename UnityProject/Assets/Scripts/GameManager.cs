/*
Hello reader! This file is your one-stop shop for like
most of the game functionality! Yay!
Oh, it also contains all of the data structures that I have defined!
*/

using System.Collections.Generic;
using System;
using UnityEngine; 

# region GameStructuresAndEnumerations
// yo these all need filling out
enum pokemon_type
{
    NORMAL = 0,
    FIGHTING,
    FLYING,
    POISON,
    GROUND,
    ROCK,
    BUG,
    GHOST,
    STEEL,
    FIRE,
    WATER,
    GRASS,
    ELECTRIC,
    PSYCHIC,
    ICE,
    DRAGON,
    DARK,
    FAIRY,
    UNKNOWN,
    elementCount
};

enum pokemon_egg_group
{

};

/* This struct is used to contain information about a single move. The game 
 * basically stores a whole list of all the moves where each element of the list
 * is one of these structs! Rad!
     */ 
struct pokemon_move_data
{
    // TODO(BluCloos): Fill this struct out fam!
    //public string name;
};

// TODO(BluCloos): I can't seem to find the right name for this...
struct pokemon_move_meta
{
    public string moveName;
    public byte levelLearnedAt;
};

/* This structure stores save file information for a particular pokemon
 * */
public struct pokemon_profile
{
    public int auroraDexNumber;
};

// TODO(BluCloos): For memory saving purposes, should we not remove a bunch of the junk
// in this structure?
/* This class is used to contain information about a single pokemon. This gameObject stores a
 * whole list of them, basically the table of all the pokemon in the game.
     */
class pokemon_data
{
    public string name;
    // TODO(BluCloos): Should this be an enumeration?
    public string ability1;
    public string ability2;
    public string ability3;
    public pokemon_type type1;
    public pokemon_type type2;
    public bool hasGender;
    public float percentChanceFemale;
    public uint minHatchSteps;
    public uint maxHatchSteps;
    public pokemon_egg_group eggGroup1;
    public pokemon_egg_group eggGroup2;
    public float height;
    public float weight;
    public byte hpBaseStat;
    public byte attackBaseStat;
    public byte defenseBaseStat;
    public byte spAttackBaseStat;
    public byte spDefenseBaseStat;
    public byte speedBaseStat;
    public string levelingType;
    // TODO(BluCloos): Is this value actually an integer from 0 through 255?
    public byte catchRate;
    public byte evolutionLevel;
    // TODO(BluCloos): Should this remain a string or an enumeration?
    public string evolutionItem;
    public List<pokemon_move_meta> moves;

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
            pokemonMoves +=  moveMeta.moveName + " @ " + moveMeta.levelLearnedAt + "\n";
        }

        return pokemonInfo + "\nMoves:\n" + pokemonMoves; 
    }
}
#endregion

public class GameManager : MonoBehaviour
{
    #region PublicVariables
    [Tooltip("Total amount of pokemon in the Aurora regional dex.")]
    public uint totalPokemon;
    public uint pokemonToSpawn;
    #endregion

    #region PrivateVariables
    private static CameraRig mainCameraRig; // initialized in start
    private static PlayerController currentPlayerController; // initialized in start
    private static GameManager gm; // initialized in awake
    private static List<pokemon_data> pokemonTable = new List<pokemon_data>(); // initialized in start
    private static Dictionary<string, pokemon_move_data> pokemonMoveTable = new Dictionary<string, pokemon_move_data>(); // initialized in start
    #endregion

    #region PrivateFunctions
    private float FloatFromString(string str)
    {
        float foo;
        if (float.TryParse(str, out foo))
            return foo;
        else
            return 0.0f;
    }

    private uint UnsignedIntFromString(string str)
    {
        uint foo;
        if (uint.TryParse(str, out foo))
            return foo;
        else
            return 0;
    }

    /* This function will take in a string and remove all whitespace before and after the main
     * 'message': Ex) "   hello world!  " ---> "hello world!" 
     */
    private string StringTrimWhitespace(string str)
    {
        return str.Trim();
    }

    private void DebugLogStringAsHexSet(string str)
    {
        char[] strArr = str.ToCharArray();
        string runningStr = "{";
        for (int i = 0; i < str.Length; i++)
        {
            int val = Convert.ToInt32(strArr[i]);
            runningStr += "0x" + val.ToString("X") + ", ";
        }
        runningStr += "}";
        Debug.Log(runningStr);
    }

    // TODO(BluCloos): Surely there is an easier way to do this...
    private pokemon_type PokemonTypeFromString(string typeName)
    {
        switch (typeName.ToLower())
        {
            case "normal":
                return pokemon_type.NORMAL;
            case "fighting":
                return pokemon_type.FIGHTING;
            case "flying":
                return pokemon_type.FLYING;
            case "poison":
                return pokemon_type.POISON;
            case "ground":
                return pokemon_type.GROUND;
            case "rock":
                return pokemon_type.ROCK;
            case "bug":
                return pokemon_type.BUG;
            case "ghost":
                return pokemon_type.GHOST;
            case "steel":
                return pokemon_type.STEEL;
            case "fire":
                return pokemon_type.FIRE;
            case "water":
                return pokemon_type.WATER;
            case "grass":
                return pokemon_type.GRASS;
            case "electric":
                return pokemon_type.ELECTRIC;
            case "psychic":
                return pokemon_type.PSYCHIC;
            case "ice":
                return pokemon_type.ICE;
            case "dragon":
                return pokemon_type.DRAGON;
            case "dark":
                return pokemon_type.DARK;
            case "fairy":
                return pokemon_type.FAIRY;
            default:
                return pokemon_type.UNKNOWN;
        }
    }

    /* This function loads the resource file called resourceName and parses out the
     * data into a pokemon_data structure. If the pokemon resource does exist, this function will
     * return null. For pokemon whos data is not yet defined, their table entry is a null
     * reference. This is for data saving purposes. 
     */
    private pokemon_data ParsePokemonDataResource(string resourceName)
    {
        var textFile = Resources.Load<TextAsset>(resourceName);
        if (textFile != null)
        {
            string[] lines = textFile.text.Split('\n');
            bool inMoves = false;
            pokemon_data pokemonData = new pokemon_data();
            pokemonData.moves = new List<pokemon_move_meta>();

            for (uint i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (!inMoves)
                {
                    string[] lineSplit = line.Split(':');
                    string handle = lineSplit[0].ToLower();
                    string content = StringTrimWhitespace(lineSplit[1]);

                    switch (handle)
                    {
                        case "name":
                            pokemonData.name = content;
                            break;
                        case "type1":
                            pokemonData.type1 = PokemonTypeFromString(content);
                            break;
                        case "type2":
                            pokemonData.type2 = PokemonTypeFromString(content);
                            break;
                        case "height":
                            pokemonData.height = FloatFromString(content);
                            break;
                        case "weight":
                            pokemonData.weight = FloatFromString(content);
                            break;
                        case "hp":
                            pokemonData.hpBaseStat = (byte)UnsignedIntFromString(content);
                            break;
                        case "attack":
                            pokemonData.attackBaseStat = (byte)UnsignedIntFromString(content);
                            break;
                        case "defense":
                            pokemonData.defenseBaseStat = (byte)UnsignedIntFromString(content);
                            break;
                        case "spattack":
                            pokemonData.spAttackBaseStat = (byte)UnsignedIntFromString(content);
                            break;
                        case "spdefense":
                            pokemonData.spDefenseBaseStat = (byte)UnsignedIntFromString(content);
                            break;
                        case "speed":
                            pokemonData.speedBaseStat = (byte)UnsignedIntFromString(content);
                            break;
                        case "levelingtype":
                            pokemonData.levelingType = content;
                            break;
                        case "catchrate":
                            pokemonData.catchRate = (byte)UnsignedIntFromString(content);
                            break;
                        case "moves":
                            inMoves = true;
                            break;
                        default:
                            // uhhhhhhhhhhhhhhhhhhhhh I don't even know
                            break;
                    }
                }
                else
                {
                    pokemon_move_meta moveMeta;
                    moveMeta.levelLearnedAt = 0;

                    string[] lineSplit = line.Split('/');
                    moveMeta.moveName = lineSplit[0];
                    string level = lineSplit[1];

                    if (!level.Equals("-"))
                    {
                        moveMeta.levelLearnedAt = (byte)UnsignedIntFromString(level);
                    }

                    pokemonData.moves.Add(moveMeta);
                }
            }

            return pokemonData;
        }
        else
        {
            return null;
        }
    }

    /* As of now, this function loads all data regarding every Pokemon in
       the Aurora Pokedex. It also loads all the different moves in the game. 
    */
    private void LoadPokemonTables()
    {
        // Load all the pokemons
        for (uint i = 0; i < totalPokemon; i++)
        {
            string resName = (i + 1).ToString();
            pokemon_data pokemonData = ParsePokemonDataResource(resName);
            pokemonTable.Add(pokemonData);
        }
    }
    #endregion

    #region PublicFunctions

    public static void SwitchPlayer(PlayerController pc)
    {
        // TODO(BluCloos): Auto position the camera to avoid the fast teleport of the camera!
        currentPlayerController.Deactivate();
        currentPlayerController = pc;
        currentPlayerController.Activate();
        mainCameraRig.target = currentPlayerController.gameObject.transform;
    }

    public static Pokemon InstantiatePokemon(pokemon_profile profile, bool newPokemon, Vector3 worldPos)
    {
        GameObject pokePrefab = Resources.Load<GameObject>("obj_" + profile.auroraDexNumber);
        pokemon_data pokeData = pokemonTable[profile.auroraDexNumber - 1];
        if (pokePrefab != null && pokeData != null)
        {
            GameObject pokeObj = Instantiate(pokePrefab, worldPos, Quaternion.identity);
            
            // Set the height of the pokemon
            pokeObj.transform.localScale = new Vector3(pokeData.height, pokeData.height, pokeData.height);
            float halfHeight = pokeData.height / 2.0f;

            // Setup the character controller
            CharacterController cc = pokeObj.AddComponent<CharacterController>();
            cc.slopeLimit = 55;
            cc.skinWidth = 0.01f;
            cc.center = new Vector3(0.0f, halfHeight, 0.0f);
            cc.radius = halfHeight;
            cc.height = pokeData.height;

            // Setup the player controller component
            PlayerController pc = pokeObj.AddComponent<PlayerController>();
            pc.rootMotion = false;
            pc.walkingLayerMask = ~(1 << 8);
            pc.walkingSpeed = pc.walkingSpeed * pokeData.height;
            pc.runningSpeed = pc.runningSpeed * pokeData.height;
            pc.feetPosOffsetFromOrigin = 0.0f;

            // Grab the animator for your boi and attach that son of a bitch!
            RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>("animator_" + profile.auroraDexNumber);
            pokeObj.GetComponent<Animator>().runtimeAnimatorController = animatorController;

            Pokemon pokePoke = pokeObj.AddComponent<Pokemon>();

            // Set up the pokemon profile
            if (newPokemon)
            {
                // TODO(BluCloos): Generate the new pokemon information
            }
            else
            {
                pokePoke._set_profile(profile);
            }

            return pokePoke;
        }
        else
        {
            return null;
        }
    }

    #endregion

    #region OverloadedUnityFunctions
    void Awake()
    {
        // NOTE(Reader): This little cute bit here is making sure that
        // only one GameManager ever exists in a scene at a time, and that 
        // the first one to be created will persist through different scenes.
        if (gm != null)
            Destroy(gameObject);
        else
        {
            gm = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        mainCameraRig = Camera.main.GetComponent<CameraRig>();
        if (mainCameraRig == null)
            Debug.Log("Warning: Please attach a CameraRig component to the main camera!");

        LoadPokemonTables();

        // Find the player and set the main camera to target the player
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                currentPlayerController = playerObj.GetComponent<PlayerController>();
                mainCameraRig.target = playerObj.transform;
                currentPlayerController.Activate();
            }
            else
                Debug.Log("Warning: Could not find the player!");
        }
    }

    bool playingPokemon = false;

    void Update()
    {
        if (Input.GetButtonDown("DebugAction"))
        {
            if (!playingPokemon)
            {
                // spawn a random, new pokemon and switch to it
                pokemon_profile newProfile;
                newProfile.auroraDexNumber = (int)pokemonToSpawn;
                Pokemon newPoke = InstantiatePokemon(newProfile, true, Vector3.zero + 2.0f * Vector3.up);
                PlayerController pc = newPoke.gameObject.GetComponent<PlayerController>();
                SwitchPlayer(pc);
                playingPokemon = true;
            }
            else
            {
                // destroy the instantiated pokemon
                Destroy(currentPlayerController.gameObject);
                // switch back to the player
                PlayerController pc = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
                SwitchPlayer(pc);
                playingPokemon = false;
            }
        }
    }
    #endregion
}
