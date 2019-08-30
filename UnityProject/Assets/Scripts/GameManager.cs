/*
Hello reader! This file is your one-stop shop for like
most of the game functionality! Yay!
Basically, it manages the game. 
*/

using System.Collections.Generic;
using UnityEngine;
using PokemonHeader;
//using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // TODO(BluCloos): We might want to make some of these inspector variables be ranges to ensure that
    // we only get sane values.
    #region InspectorVariables
    [SerializeField]
    [Tooltip("Total amount of pokemon in the Aurora regional dex.")]
    private uint totalPokemon = 0;
    [SerializeField]
    [Tooltip("Total amount of moves in the game.")]
    private uint totalMoves = 0;
    [SerializeField]
    [Tooltip("The default walking speed of spawned Pokemon.")]
    private float defaultWalkingSpeed = 1.4f;
    [SerializeField]
    [Tooltip("The default running speed of spawned Pokemon.")]
    private float defaultRunningSpeed = 3.3f;
    [SerializeField]
    [Tooltip("The amount of health bar lengths per second that Pokemon health bars decrease / increase at.")]
    private float pokemonHealthVelocity = 1.2f;
    // TODO(BluCloos): This is a debug parameter. I use it to spawn any pokemon I want! Crazy!
    // so eventually this needs to get purged out of existence! In fact, it will die in response
    // to the birth of the debug menu.
    [SerializeField]
    private uint pokemonToSpawn;
    #endregion

    #region References
    private CameraRig mainCameraRig;
    #endregion

    #region PrivateVariables
    private PlayerController2 currentPlayerController;
    private static GameManager gm;
    private List<PokemonData> pokemonTable = new List<PokemonData>();
    private Dictionary<string, PokemonMoveData> pokemonMoveTable = new Dictionary<string, PokemonMoveData>();
    float[,] pokemonTypeTable;
    #endregion

    #region PrivateFunctions
    /* As of now, this function loads all data regarding every Pokemon in
       the Aurora Pokedex. It also loads all the different moves in the game. 
    */
    private void LoadPokemonTables()
    {
        // Load all the pokemons
        for (uint i = 0; i < totalPokemon; i++)
        {
            string resName = (i + 1).ToString();
            PokemonData pokemonData = FileParsing.ParsePokemonDataResource(resName, 
                defaultWalkingSpeed, defaultRunningSpeed);
            pokemonTable.Add(pokemonData);
        }

        // Load all the moves
        for (int i = 0; i < totalMoves; i++)
        {
            string resName = (i + 1).ToString() + "_move";
            PokemonMoveData pokemonMoveData = FileParsing.ParsePokemonMoveDataResource(resName);
            pokemonMoveTable.Add(pokemonMoveData.name, pokemonMoveData);
        }

        // Load the type table
        gm.pokemonTypeTable = FileParsing.ParseTypeTableResource("moveTable");
    }

    private void DebugTests()
    {
        // Test the FileParsing class
        Debug.Log("Testing FileParsing class");
        FileParsing.DebugTest();
        Debug.Log("Done Test");

        // Testing the PokeMath class
        Debug.Log("Testing PokeMath class");
        PokeMath.DebugVerify();
        Debug.Log("Done Test");

        // Test that the type table has been initialized.
        Debug.Log("Testing the type table");
        float mod = GetTypeMod(pokemon_type.ELECTRIC, pokemon_type.GROUND);
        Debug.Log("Electric vs Ground: " + mod);
        mod = GetTypeMod(pokemon_type.GROUND, pokemon_type.POISON);
        Debug.Log("Ground vs Poison: " + mod);
        Debug.Log("Done Test");

        // Test that the PokeData is OK
        PokemonData pokeData = GetPokemonData(100);
        Debug.Log(pokeData);
    }
    #endregion

    #region PublicInterface

    public static float GetPokemonHealthVelocity(){ return gm.pokemonHealthVelocity; }

    /// <summary>
    /// This function will create a new random pokemon with the given dex Id and level.
    /// </summary>
    public static pokemon_profile CreateNewPokemonProfile(int auroraDexNumber, int level)
    {
        pokemon_profile profile = new pokemon_profile();
        // First things first we need to grab some important info
        // aka we need the pokeData information!
        PokemonData pokeData = GameManager.GetPokemonData(auroraDexNumber);
        if (pokeData != null)
        {
            // TODO(BluCloos): There is a lot more stuff we are going to need to initialize.
            // For now, we just need to set up

            profile.auroraDexNumber = auroraDexNumber;
            profile.experiencePoints = PokeMath.ExpFromLevel(level, pokeData.levelingType);
            profile.level = level;

            profile.hpEV = 0;
            profile.attackEV = 0;
            profile.defenseEV = 0;
            profile.spAttackEV = 0;
            profile.spDefenseEV = 0;
            profile.speedEV = 0;

            // TODO(BluCloos): Make the probability of higher IV less likely by making their probability align
            // with an exponential that has a fraction base.
            // this can be done by making space much larger near the lower vaules and space really small near the
            // higher values. When I refer to space I just mean the actual range size of the Random.Range() call
            profile.hpIV = (byte)Random.Range(0.0f, 31.0f);
            profile.attackIV = (byte)Random.Range(0.0f, 31.0f);
            profile.defenseIV = (byte)Random.Range(0.0f, 31.0f);
            profile.spAttackIV = (byte)Random.Range(0.0f, 31.0f);
            profile.spDefenseIV = (byte)Random.Range(0.0f, 31.0f);
            profile.speedIV = (byte)Random.Range(0.0f, 31.0f);

            // TODO(BluCloos): Actually implement natures. Of course, this is only necessary if we decide that it is, so... who knows?
            profile.hpStat = PokeMath.CalculateHp(pokeData.hpBaseStat, profile.hpIV, profile.hpEV, profile.level);
            profile.attackStat = PokeMath.CalculateStat(pokeData.attackBaseStat, profile.attackIV, profile.attackEV, 1.0f, profile.level);
            profile.defenseStat = PokeMath.CalculateStat(pokeData.defenseBaseStat, profile.defenseIV, profile.defenseEV, 1.0f, profile.level);
            profile.spDefenseStat = PokeMath.CalculateStat(pokeData.spDefenseBaseStat, profile.spDefenseIV, profile.spDefenseEV, 1.0f, profile.level);
            profile.spAttackStat = PokeMath.CalculateStat(pokeData.spAttackBaseStat, profile.spAttackIV, profile.spAttackEV, 1.0f, profile.level);
            profile.speedStat = PokeMath.CalculateStat(pokeData.speedBaseStat, profile.speedIV, profile.speedEV, 1.0f, profile.level);

            profile.cHpStat = profile.hpStat;
            profile.cAttackStat = profile.attackStat;
            profile.cDefenseStat = profile.defenseStat;
            profile.cSpAttackStat = profile.spAttackStat;
            profile.cSpDefenseStat = profile.spDefenseStat;
            profile.cSpeedStat = profile.speedStat;

            // TODO(BluCloos): We need to make sure the the random generation of moves yeilds in at least one attacking move
            // Here is how I think it should be implemented. We generate the moves as we are doing currently. After, we check to see
            // if there is at least one attacking move. If there is none, we go through all the learnable moves, find the first attacking move,
            // and replace the first random move with said attacking move.  

            // generate a list of learnableMoves based on the level of the Pokemon
            List<string> learnableMoves = new List<string>();
            for (int i = 0; i < pokeData.moves.Count; i++)
            {
                pokemon_move_meta learnableMove = pokeData.moves[i];
                if (learnableMove.levelLearnedAt <= level)
                {
                    learnableMoves.Add(learnableMove.moveName);
                }
            }

            profile.learnedMoves = new List<string>();
            profile.moves = new int[4];
            // Now, based on the list of learnable moves, we are going to add said moves to our SICK pokemon
            for (int i = 0; i < 4; i++)
            {
                if (learnableMoves.Count > 0)
                {
                    int randomMove = (int)Random.Range(0.0f, (float)(learnableMoves.Count - 1));
                    string newMove = learnableMoves[randomMove];
                    learnableMoves.RemoveAt(randomMove); // Remove it from the list so we don't encounter it again
                    profile.learnedMoves.Add(newMove); // Actually add said new move to the pokemon!
                }
            }

            // Assign the move indices!
            for (int i = 0; i < 4; i++)
            {
                int newIndex = -1;
                if (i < profile.learnedMoves.Count)
                {
                    newIndex = i;
                }

                profile.moves[i] = newIndex;
            }
        }
        else
        {
            // Return an invalid profile
            profile.auroraDexNumber = -1;
        }

        return profile;
    }

    /// <summary>
    /// This function returns the modifier for moves of the attackingType against the defendingType
    /// </summary>
    public static float GetTypeMod(pokemon_type attackingType, pokemon_type defendingType)
    {
        return gm.pokemonTypeTable[(int)attackingType, (int)defendingType];
    }

    /// <summary>
    /// Given the name of the move this function returns the data structure for that move.
    /// </summary>
    public static PokemonMoveData GetPokemonMoveData(string key)
    {
        if (gm.pokemonMoveTable.ContainsKey(key))
            return gm.pokemonMoveTable[key];
        else
            return null;
    }

    /// <summary>
    /// This function will return the data corresponding to the pokemon dex number provided.
    /// If the data can not be found in the global tables, this function will return null;
    /// </summary>
    public static PokemonData GetPokemonData(int auroraDexNumber)
    {
        if (auroraDexNumber > gm.pokemonTable.Count || auroraDexNumber <= 0)
        {
            Debug.LogWarning("auroraDexNumber out of range!: " + auroraDexNumber);
            return null;
        }

        return gm.pokemonTable[auroraDexNumber - 1];
    }

    public static void DebugPrintPokemon(pokemon_profile profile)
    {
        if (profile.auroraDexNumber != -1)
        {
            Debug.Log("Printing Pokemon Profile!");
            Debug.Log("Dex Number: " + profile.auroraDexNumber);
            Debug.Log("EXP: " + profile.experiencePoints);
            Debug.Log("Level: " + profile.level);

            Debug.Log("HP IV: " + profile.hpIV);
            Debug.Log("ATTACK IV: " + profile.attackIV);
            Debug.Log("DEFENSE IV: " + profile.defenseIV);
            Debug.Log("SPATTACK IV: " + profile.spAttackIV);
            Debug.Log("SPDEFENSE IV: " + profile.spDefenseIV);
            Debug.Log("SPEED IV: " + profile.speedIV);

            Debug.Log("HP Stat: " + profile.hpStat);
            Debug.Log("ATTACK Stat: " + profile.attackStat);
            Debug.Log("DEFENSE Stat: " + profile.defenseStat);
            Debug.Log("SPATTACK Stat: " + profile.spAttackStat);
            Debug.Log("SPDEFENSE Stat: " + profile.spDefenseStat);
            Debug.Log("SPEED Stat: " + profile.speedStat);

            for (int i = 0; i < 4; i++)
            {
                if (profile.moves[i] != -1)
                {
                    Debug.Log("Move" + i + ": " + profile.learnedMoves[profile.moves[i]]);
                }
            }

            Debug.Log("Done.");
        }
        else
        {
            Debug.Log("Pokemon Profile invalid!");
        }
    }

    public static void DebugPrintPokemonMove(PokemonMoveData move)
    {
        if (move != null)
        {
            Debug.Log("Printing Pokemon Move:");
            Debug.Log("Name: " + move.name);
            Debug.Log("Type: " + move.type);
            Debug.Log("MoveClass: " + move.moveClass);
            Debug.Log("PowerPoints: " + move.powerPoints);
            Debug.Log("BasePower: " + move.basePower);
            Debug.Log("Acurracy: " + move.accuracy);
            Debug.Log("Done.");
        }
    }

    /// <summary>
    /// This function will change the current player being controlled to the one given. 
    /// Note that this function will not delete the current player, it will merely
    /// make it ignore input. 
    /// </summary>
    public static void SwitchPlayer(PlayerController2 pc)
    {
        if (gm.currentPlayerController != null)
            gm.currentPlayerController.Deactivate();
        gm.currentPlayerController = pc;
        gm.currentPlayerController.Activate();

        // modify the camera rig to target the current player controller and
        // change the zoom settings so that it 'feels better'.
        // TODO(Noah): Right now the mapping for the maximum character zoom is not really setup
        // at all.
        gm.mainCameraRig.target = gm.currentPlayerController.gameObject.transform;
        gm.mainCameraRig.offset = new Vector3(0.0f, gm.currentPlayerController.GetHeight() / 2.0f, 0.5f);
        
        // NOTE(Reader): This math is basically a cheese. Im just using 
        // known value pairs of minCameraDistances and character heights
        // to create the appropriate linear mapping from the character height to 
        // the minimum camera distance. Think grade 9 maths. y=mx+b 
        float m = (2.0f / 1.1f);
        float b = 4.0f - m * 1.5f;
        gm.mainCameraRig.minDistance = m * gm.currentPlayerController.GetHeight() + b;
    }

    /// <summary>
    /// This function will spawn a pokemon at the provided world position.
    /// This function is multi-purpose. You may use it to create either a randomly generated pokemon
    /// or spawn an existing pokemon. The newPokemon parameter controls this behaviour. If you are creating
    /// a randomly generated pokemon, the profile need only be initialized with the dex index; otherwise, it
    /// must be full initialized. If the pokemon was unable to be initialized this function will return null.
    /// This will happen when the data for the pokemon is not in the global table.
    /// </summary>
    public static Pokemon InstantiatePokemon(pokemon_profile profile, bool newPokemon, Vector3 worldPos)
    {
        GameObject pokePrefab = Resources.Load<GameObject>("obj_" + profile.auroraDexNumber);
        PokemonData pokeData = GetPokemonData(profile.auroraDexNumber);
        if (pokePrefab != null && pokeData != null)
        {
            GameObject pokeObj = Instantiate(pokePrefab, worldPos, Quaternion.identity);

            pokeObj.layer = LayerMask.NameToLayer("Player");

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

            // Instantiate the hp bar on the pokemon
            GameObject healthPrefab = Resources.Load<GameObject>("PokemonHPBar");
            GameObject healthObj = Instantiate(healthPrefab, pokeObj.transform, false);
            healthObj.transform.localPosition = new Vector3(0.0f, 1.2f, 0.0f);

            Pokemon pokePoke = pokeObj.AddComponent<Pokemon>();

            // Set up the pokemon profile
            bool result;
            if (newPokemon)
            {
                // TODO(BluCloos): Generate the new pokemon information
                result = pokePoke.InitializeAsNew(profile.auroraDexNumber, 34);
            }
            else
            {
                pokePoke._set_profile(profile);
                result = true;
            }
            
            // TODO(BluCloos): Handle the cases where the pokemon spawning does not work!

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

            mainCameraRig = Camera.main.GetComponent<CameraRig>();
            if (mainCameraRig == null)
                Debug.Log("Warning: Please attach a CameraRig component to the main camera!");

        }
    }

    void Start()
    {
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
        stupidPoke.auroraDexNumber = 100;
        InstantiatePokemon(stupidPoke, true, new Vector3(1.0f, 2.0f, 1.0f));

        DebugTests();
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
