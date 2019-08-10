using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Tooltip("Radius to search when looking for currently controlled Pokemon")]
    public float searchRadius = 10.0f; // Basically 10 meters seems like a reasonable distance, 
    [Tooltip("Mask to use when looking for currently controlled Pokemon")]
    public LayerMask searchMask;
    // I don't, hmu if I am wrong FAM

    private PlayerController pc;
    private CharacterController cc;
    [SerializeField]
    private Transform staticLookAtPoint;
    [SerializeField]
    private Transform dynamicLookAtPoint;

    public float runningLerp = 0.0f;
    private bool lookingAtPokemon = false; 

    // Start is called before the first frame update
    void Start()
    {
        cc = GetComponent<CharacterController>();
        pc = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        // We default the character to be not looking at the pokemon.
        bool localFrameLooking = false;
        Vector3 desiredPos = staticLookAtPoint.position;

        // For the code below we are going to check if we should currently be looking at our
        // pokemans!

        // TODO(BluCloos): It seems that the majority of the time I need this wierd structure
        // which is like the middle of the character controller. So I should probably have a function
        // for that tbh. Seems like a thing I would need.
        Collider[] hitColliders = Physics.OverlapSphere(pc.ControllerFeetPos() + cc.height / 2.0f * Vector3.up, searchRadius, searchMask);

        for (int i = 0; i < hitColliders.Length; i++)
        {
            Collider c = hitColliders[i];
            PlayerController _pc = c.gameObject.GetComponent<PlayerController>();
            if (_pc != null)
            {
                if (_pc.IsActivated())
                {
                    Vector3 deltaPos = c.transform.position - pc.transform.position;
                    float angle = Vector3.Angle(transform.forward, deltaPos);
                    if (angle < 45.0f)
                    {
                        localFrameLooking = true;
                        desiredPos = c.transform.position;
                    }
                }
            }
        }

        // Everytime we have a transition we want to make sure to do the lerp 
        // correctly
        if (localFrameLooking != lookingAtPokemon)
        {
            runningLerp = 0.0f;
            lookingAtPokemon = localFrameLooking;
        }

        dynamicLookAtPoint.position = Vector3.Lerp(dynamicLookAtPoint.position, desiredPos, runningLerp);        
        runningLerp += Time.deltaTime;
    }
}
