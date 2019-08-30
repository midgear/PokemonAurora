using UnityEngine;
using PokemonHeader;
using System.Collections;

public class Pokemon : MonoBehaviour
{
    // Edit: Due to the way this is being done we no longer need the animation guy. Instead, it can be done from script. Although, 
    // We may make a generic script used for all animations that want to lock the player throughout the animation. Whilst I make this generic 
    // script I can make sure that I finally get the timing right and it doesn't feel like you are waiting forever for the animation to end.


    // NOTE(Reader): Okay so the way that I have the Pokemon attack is really ugly. Basically, I am going to get 
    // the animator to call the TryAttack when the animation is done. This way the pokemon actually attacks at the right time.
        // NOTE(BluCloos): I might have to add some sort of offset to insure that all Pokemon attack at the right time.
    // How does the animator know which attack to do? It doesn't care. The pokemon has state, specifically, it has a currently selected move.
    
    // NOTE(Reader): These kind of perfrom as inspector variables for this script. You can't really have inspector variables
    // because this script is instantiated on the Pokemon at runtime.
    #region ConstantVariables
    private const float meleeAttackRadius = 5f;
    #endregion

    #region PrivateVariables
    private pokemon_profile profile;
    #endregion

    #region References
    private PlayerController2 pc;
    private Animator animator;
    private PokemonHealthBar healthBar;
    #endregion
   
    #region PublicInterface

    public float attackOffset = 0.0f;

    /// <summary>
    /// Call to initialize this Pokemon instance as a new pokemon with the provided dex number and level.
    /// This function will return true upon success and false otherwise. It will fail in the case that the
    /// data for the pokemon is not loaded into the global table.
    /// </summary>    
    public bool InitializeAsNew(int auroraDexNumber, int level)
    {
        pokemon_profile newProfile = GameManager.CreateNewPokemonProfile(auroraDexNumber, level);
        if (newProfile.auroraDexNumber > 0)
        {
            profile = newProfile;
            return true;
        }
        else
        {
            // TODO(BluCloos): Is the line I have commented below safe?
            // I think so because the gameManager deletes that boi if this call fails.
            //profile.auroraDexNumber = -1;
            return false;
        }
    }

    /// <summary>
    /// This function is a callback event received from enemy Pokémon. We calculate the amount of damage done to us. 
    /// </summary>
    public void OnAttacked(pokemon_profile attacker, PokemonMoveData moveData)
    {
        Debug.Log("On no!!!! I am getting attacked!");

        float damageToMe = PokeMath.CalculateDamage(attacker, this.profile, moveData);
        Debug.Log(damageToMe);
        this.profile.cHpStat -= (int)damageToMe;

        // Check if the player is dead?
        if (profile.cHpStat <= 0)
        {
            OnFainted();
        }

        if (healthBar)
            healthBar.Updated((float)profile.cHpStat / (float)profile.hpStat);
        else
            Debug.LogWarning("No health bar");
    }

    public void DelayedAttack(float delayTime)
    {
        IEnumerator corut = DelayedAttackRoutine(delayTime);
        StartCoroutine(corut);
    }
    #endregion

    #region GrossCodes
    // NOTE(Reader): This function is not to be called during normal use. It is simply a hook
    // used by the game manager whenever one of these bad boys is created.
    public void _set_profile(pokemon_profile profile) { this.profile = profile; }
    #endregion

    #region PrivateFunctions
    private void OnFainted()
    {
        if (animator)
        {
            // TODO(BluCloos): When the player walks far enough away,
            // the pokemon should basically die.
            animator.SetBool("Fainted", true);
            pc.Deactivate();
            if (healthBar)
                healthBar.BeginDeath();
        }
    }

    IEnumerator DelayedAttackRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        TryAttack();
        yield return null;
    }

    private int selectedMove = 0;
    private void TryAttack()
    {
        // Even before we do the first thing we are going to make the pokemon try to do 
        // the attacking animation.

        // First things first we have to grab the attacking move and make sure that 
        // its a valid move. This part of the code will handle the case that they don't 
        // have a move in that slot
        if (profile.moves[selectedMove] == -1)
            return;

        Debug.Log("Move index is proper");

        // TODO(BluCloos): Actually flipping use the moves of the Pokemon. Of course, this would imply that we have implemented
        // all the moves that they know.
        // NOTE(Reader): This is kind of brain bending (not really), but uh, sorry if it is. If it isn't, then, good job! 
        //pokemon_move_data attackingMove = GameManager.GetPokemonMoveData(profile.learnedMoves[profile.moves[moveIndex]]);
        PokemonMoveData attackingMove = GameManager.GetPokemonMoveData("Tackle");
        if (attackingMove == null)
            return;

        Debug.Log("Move exists! Passed the tests!");

        // NOTE(Reader): The attack is going to work like this...
        // We are going to shoot a sphereCast forward to determine if
        // any enemy Pokemon is in front of us. If we get a hit, boom!
        // We attack them, otherwise, we just don't attack them.
        // Also, we are going to init the attacking animation.
        RaycastHit hit;
        if (Physics.Raycast(pc.GetMidsectionPos() + transform.forward * (pc.GetRadius() + 0.05f), transform.forward, out hit, meleeAttackRadius))
        {
            Debug.Log("Ray hit!");
            Debug.Log(hit.collider.gameObject.name);
            // okay so basically we straight up goofed that mans
            Pokemon enemy;
            if ((enemy = hit.collider.gameObject.GetComponent<Pokemon>()) != null)
            {
                enemy.OnAttacked(this.profile, attackingMove);
            }
        }
    }
    #endregion

    #region UnityCallbacks
    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogWarning("Unable to get a reference to the animator component!");

        pc = GetComponent<PlayerController2>();
        if (pc == null)
            Debug.LogWarning("Unable to get a reference to the player controller component!");

        healthBar = GetComponentInChildren<PokemonHealthBar>();       
    }

    void Start()
    {
        GameManager.DebugPrintPokemon(profile);
        if (healthBar)
            healthBar.Updated(1.0f);
    }

    void Update()
    {
        if (pc.IsActivated()) // Make sure that this pokemon is being controlled right now (or even just that they are accepting input)
        {
            // Check the input to see if we should do any attacks
            if (Input.GetButtonDown("Attack1"))
            {
                // TODO(BluCloos): add the AOE and projectiles. Right now we only do the melee moves!
                animator.SetBool("MeleeAttack", true);
                selectedMove = 0;
            }
        }        
    }
    #endregion
}
