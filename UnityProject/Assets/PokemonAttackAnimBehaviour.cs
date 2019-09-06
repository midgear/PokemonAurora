using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokemonAttackAnimBehaviour : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("MeleeAttack", false);
        animator.SetBool("ProjectileAttack", false);
        animator.SetBool("AOEAttack", false);
        PlayerController2 pc = animator.gameObject.GetComponent<PlayerController2>();
        pc.TimedLock(stateInfo.length - 0.1f);
        Pokemon poke = animator.gameObject.GetComponent<Pokemon>();
        poke.DelayedAttack(stateInfo.length - 1.5f + poke.attackOffset);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
