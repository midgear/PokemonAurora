using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pokemon : MonoBehaviour
{
    private pokemon_profile profile;

    // NOTE(Reader): this function is not to be called during normal use. It is simply a hook
    // used by the game manager whenever one of these bad boys is created.
    public void _set_profile(pokemon_profile profile) { this.profile = profile; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
