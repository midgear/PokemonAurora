using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pokemon : MonoBehaviour
{
    private pokemon_profile profile;
    private PlayerController2 pc;
    private Animator animator;
    // NOTE(Reader): This function is not to be called during normal use. It is simply a hook
    // used by the game manager whenever one of these bad boys is created.
    public void _set_profile(pokemon_profile profile) { this.profile = profile; }

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        pc = GetComponent<PlayerController2>();
    }

    private void TryAttack(uint moveIndex)
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (pc.IsActivated()) // Make sure that this pokemon is being controlled right now
        {
            // Check the input to see if we should do any attacks
            if (Input.GetButtonDown("Attack1"))
            {
                TryAttack(0);
            }
        }
    }
}
