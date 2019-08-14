/*
Hello reader! This file is your one-stop shop for like
most of the game functionality! Yay!
Oh, it also contains all of the data structures that I have defined!
*/

using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

#region GameStructuresAndEnumerations
// yo these all need filling out
public enum pokemon_type
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

/*
enum pokemon_egg_group
{

};
*/

public enum pokemon_move_class
{
    MELEE,
    PROJECTILE,
    AOE
}

/* This struct is used to contain information about a single move. The game 
 * basically stores a whole list of all the moves where each element of the list
 * is one of these structs! Rad!
     */ 
public class pokemon_move_data
{
    // TODO(BluCloos): Fill this struct out fam!
    public string name;
    public pokemon_type type;
    public pokemon_move_class moveClass;
    public float powerPoints;
    public float basePower;
    // TODO(BluCloos): At some point we are going to have to implement status
    // effects. But as of now, who the fuck cares?
};

// TODO(BluCloos): I can't seem to find the right name for this...
struct pokemon_move_meta
{
    public string moveName;
    public byte levelLearnedAt;
};

// NOTE(BluCloos): Okay so basically I just figured out how I am going to make this backend 
// interface properly with fsm addons! So I can just fire events and provide static methods that
// can be called. This is by far the best solution to the problem. Awesome stuff.

/* This structure stores save file information for a particular pokemon
 * */
public struct pokemon_profile
{
    public int auroraDexNumber;

    uint experiencePoints;
    // Every time the experience points increase, the level is recalculated. If it is such that
    // the pokemon has gained a level, the stats are then updated. An event is then raised that
    // the pokemon has gained a level!  
    uint level;

    public List<string> learnedMoves;
    public int[] currentMoves; // Set of 4 ints that point into the learnedMoves array.
    // these specify the set of 'activeMoves' that the pokemon has.

    // Current calculated stats based on the level of the pokemon.
    // these are updated using the base stats of the pokemon every time 
    // the pokemon levels up! Awesome, huh?
    public byte hpStat;
    public byte attackStat;
    public byte defenseStat;
    public byte spAttackStat;
    public byte spDefenseStat;
    public byte speedStat;

    // these are the current stats and can differ from the calculated stats for
    // the any number of reasons. For example, the pokemon may be damaged from battle.
    // they may be using stat boosters like those X defend things.
    public byte cHpStat;
    public byte cattackStat;
    public byte cdefenseStat;
    public byte cspAttackStat;
    public byte cspDefenseStat;
    public byte cspeedStat;
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
    //public string ability1;
    //public string ability2;
    //public string ability3;
    public pokemon_type type1;
    public pokemon_type type2;
    //public bool hasGender;
    //public float percentChanceFemale;
    //public uint minHatchSteps;
    //public uint maxHatchSteps;
    //public pokemon_egg_group eggGroup1;
    //public pokemon_egg_group eggGroup2;
    public float height;
    public float weight;
    public float walkingSpeed; // this is in meters per second
    public float runningSpeed; // this is in meters per second
    public byte hpBaseStat;
    public byte attackBaseStat;
    public byte defenseBaseStat;
    public byte spAttackBaseStat;
    public byte spDefenseBaseStat;
    public byte speedBaseStat;
    public string levelingType;
    // TODO(BluCloos): Is this value actually an integer from 0 through 255?
    public byte catchRate;
    //public byte evolutionLevel;
    // TODO(BluCloos): Should this remain a string or an enumeration?
    //public string evolutionItem;
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
    [Tooltip("Total amount of moves in the game.")]
    public uint totalMoves;
    [Tooltip("The default walking speed of spawned Pokemon.")]
    public float defaultWalkingSpeed = 1.4f;
    [Tooltip("The default running speed of spawned Pokemon.")]
    public float defaultRunningSpeed = 3.3f;
    // This is a debug parameter. I use it to spawn any pokemon I want! Crazy!
    public uint pokemonToSpawn;
    #endregion

    #region PrivateVariables
    private static CameraRig mainCameraRig; // initialized in start
    private static PlayerController2 currentPlayerController; // initialized in start
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
            pokemonData.walkingSpeed = defaultWalkingSpeed;
            pokemonData.runningSpeed = defaultRunningSpeed;

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
                        case "walkingspeed":
                            pokemonData.walkingSpeed = FloatFromString(content);
                            break;
                        case "runningspeed":
                            pokemonData.runningSpeed = FloatFromString(content);
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

    private pokemon_move_data ParsePokemonMoveDataResource(string resourceName)
    {
        var textFile = Resources.Load<TextAsset>(resourceName);
        if (textFile != null)
        {
            string[] lines = textFile.text.Split('\n');
            pokemon_move_data moveData = new pokemon_move_data();
            for (int i = 0; i < lines.Length; i++)
            {
                // TODO(BluCloos): This code here is very similar to the fucking
                // other code in the other parse function. This should be abstracted because
                // that's what we programmers do! Fuck yeah!
                string line = lines[i];
                string[] lineSplit = line.Split(':');
                string handle = lineSplit[0].ToLower();
                string content = StringTrimWhitespace(lineSplit[1]);

                switch (handle)
                {
                    case "name":
                        moveData.name = content;
                        break;
                    case "pp":
                        moveData.powerPoints = UnsignedIntFromString(content);
                        break;
                    case "power":
                        moveData.powerPoints = UnsignedIntFromString(content);
                        break;
                    case "type":
                        moveData.type = PokemonTypeFromString(content);
                        break;
                    case "class":
                        switch (content.ToLower())
                        {
                            case "melee":
                                moveData.moveClass = pokemon_move_class.MELEE;
                                break;
                            case "aoe":
                                moveData.moveClass = pokemon_move_class.AOE;
                                break;
                            case "projectile":
                                moveData.moveClass = pokemon_move_class.PROJECTILE;
                                break;
                        }
                        break;
                }
            }

            return moveData;
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

        // Load all the moves
        for (int i = 0; i < totalMoves; i++)
        {
            string resName = (i + 1).ToString() + "_move";
            pokemon_move_data pokemonMoveData = ParsePokemonMoveDataResource(resName);
            pokemonMoveTable.Add(pokemonMoveData.name, pokemonMoveData);
        }
    }
    #endregion

    #region PublicFunctions

    public static void SwitchPlayer(PlayerController2 pc)
    {
        // TODO(BluCloos): Auto position the camera to avoid the fast teleport of the camera!
        // NOTE(BluCloos): The above is only applicable when the camera has smooth follow on,
        // which of course we have to assume is like all the time...
        if (currentPlayerController != null)
            currentPlayerController.Deactivate();
        currentPlayerController = pc;
        currentPlayerController.Activate();

        // modify the camera rig to target the current player controller and
        // change the zoom settings so that it 'feels better'.
        // TODO(Noah): Right now the mapping for the maximum character zoom is not really setup
        // at all.
        mainCameraRig.target = currentPlayerController.gameObject.transform;
        mainCameraRig.offset = new Vector3(0.0f, currentPlayerController.GetHeight() / 2.0f, 0.5f);
        
        // NOTE(Reader): This math is basically a cheese. Im just using 
        // known value pairs of minCameraDistances and character heights
        // to create the appropriate linear mapping from the character height to 
        // the minimum camera distance. Think grade 9 maths. y=mx+b 
        float m = (2.0f / 1.1f);
        float b = 4.0f - m * 1.5f;
        mainCameraRig.minDistance = m * currentPlayerController.GetHeight() + b;
    }

    public static Pokemon InstantiatePokemon(pokemon_profile profile, bool newPokemon, Vector3 worldPos)
    {
        GameObject pokePrefab = Resources.Load<GameObject>("obj_" + profile.auroraDexNumber);
        pokemon_data pokeData = pokemonTable[profile.auroraDexNumber - 1];
        if (pokePrefab != null && pokeData != null)
        {
            GameObject pokeObj = Instantiate(pokePrefab, worldPos, Quaternion.identity);

            pokeObj.layer = LayerMask.NameToLayer("Ignore Raycast");

            // Set the height of the pokemon
            pokeObj.transform.localScale = new Vector3(pokeData.height, pokeData.height, pokeData.height);
            float halfHeight = pokeData.height / 2.0f;

            // Setup the character controller
            CharacterController cc = pokeObj.AddComponent<CharacterController>();
            cc.skinWidth = 0.01f;

            // Setup the player controller component
            PlayerController2 pc = pokeObj.AddComponent<PlayerController2>();
            pc.rootMotion = false;
            pc.walkingLayerMask = 1 << LayerMask.NameToLayer("Default");
            pc.SetWalkingSpeed(pokeData.walkingSpeed);
            pc.SetRunningSpeed(pokeData.runningSpeed);
            pc.feetOffset = 0.0f;
            pc.slopeLimit = 50;
            pc.canJump = true;

            // Setup the collision mesh for the Pokemon
            // TODO(BluCloos): Obviously, the auto generation of the collision mesh
            // needs more tuning! // In fact, this information may need to be precalculated,
            // basically it needs to become loadable data. Just like the running and walkingSpeeds.
            pc.capsuleRadius = pokeData.height * 0.3f;
            pc.localCapsuleSphere1 = new Vector3(0.0f, pc.capsuleRadius, 0.0f);
            pc.localCapsuleSphere2 = new Vector3(0.0f, pokeData.height - pc.capsuleRadius, 0.0f);

            pc.showGroundedHitPos = true;
            pc.showStepCheck = true;
            pc.showEdgeCheck = true;
            pc.showGroundDistanceCheck = true;

            // Grab the animator for your boi and attach that son of a bitch!
            RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>("animator_" + profile.auroraDexNumber);
            pokeObj.GetComponent<Animator>().runtimeAnimatorController = animatorController;

            // NOTE(Reader): This sets up imporant hooks that may not have been set up.
            // Take a look at the actual PlayerController script if you are curious.
            pc.UpdatePlayer();


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
                PlayerController2 pc = playerObj.GetComponent<PlayerController2>();
                SwitchPlayer(pc);
            }
            else
                Debug.Log("Warning: Could not find the player!");
        }

        // spawn a stupid lil debug pokemon, could be fun!
        pokemon_profile stupidPoke = new pokemon_profile();
        stupidPoke.auroraDexNumber = 23;
        //InstantiatePokemon(stupidPoke, false, new Vector3(1.0f, 2.0f, 1.0f));
    }

    bool playingPokemon = false;

    void Update()
    {
        if (Input.GetButtonDown("DebugAction"))
        {
            
            if (!playingPokemon)
            {
                // spawn a random, new pokemon and switch to it
                pokemon_profile newProfile = new pokemon_profile();
                newProfile.auroraDexNumber = (int)pokemonToSpawn;
                Pokemon newPoke = InstantiatePokemon(newProfile, true, Vector3.zero + 2.0f * Vector3.up);
                PlayerController2 pc = newPoke.gameObject.GetComponent<PlayerController2>();
                SwitchPlayer(pc);

                playingPokemon = true;
            }
            else
            {
                // destroy the instantiated pokemon
                Destroy(currentPlayerController.gameObject);
                // switch back to the player
                PlayerController2 pc = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController2>();
                SwitchPlayer(pc);
                playingPokemon = false;
            }
            

            // Destroy(currentPlayerController.gameObject);
            // GameObject newPlayer = Resources.Load<GameObject>("DebugPlayer");
            // PlayerController2 pc2 = Instantiate(newPlayer, new Vector3(0.0f, 1.0f, 0.0f), Quaternion.identity).GetComponent<PlayerController2>();
            // SwitchPlayer(pc2);
        }
    }
    #endregion
}
