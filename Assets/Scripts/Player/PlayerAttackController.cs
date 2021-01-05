﻿using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

public enum CancelAction
{
    Jump,
    Attack
}

public class PlayerAttackController : MonoBehaviour {
    public bool isAttacking; // TODO: Reset this!
    public RedAttackProperties attackProperties;
    private PlayerMovementController movementController;
    private PlayerStateManager playerState;
    private CancelAction? currentCancelAction;
    private string currentActiveAttack;

    private PlayerAnimationController animator;

    // How many frames since the player attacked?
    private int framesIntoAttack;

    void Start()
    {
        movementController = GetComponent<PlayerMovementController>();
        playerState = GetComponent<PlayerStateManager>();
        animator = GetComponent<PlayerAnimationController>();
        isAttacking = false;
        currentCancelAction = null;
        currentActiveAttack = null;
        framesIntoAttack = 0;

        movementController.LandEvent += LandCancelHandler;
    }

    void Update() {
        framesIntoAttack++;
    }
    
    public void GroundedAttackFlags(string attackName)
    {
        if (movementController.isGrounded && !animator.AnimationGetBool("IsJumping"))
        {
            if (isAttacking)
            {
                // Player is already attacking, is a cancel possible?
                if (animator.AnimationGetBool("CanCancel"))
                {
                    animator.AnimationSetBool(attackName, true);
                    currentActiveAttack = attackName;
                }
            }
            else
            {
                if (animator.AnimationGetBool("IsRunning") && !animator.AnimationGetBool("IsSkidding")) {
                    movementController.Skid();
                }
                isAttacking = true;
                animator.AnimationSetBool(attackName, true);
                currentActiveAttack = attackName;
                animator.AnimationSetBool("IsRunning", false);
                movementController.StopRun();
                framesIntoAttack = 0;
            }
        }
    }

    public void AirAttackFlags(string attackName)
    {
        if (!movementController.isGrounded && animator.AnimationGetBool("IsJumping") && !movementController.isBackDashing)
        {
            if (isAttacking)
            {
                // Player is already attacking, is a cancel possible?
                if (animator.AnimationGetBool("CanCancel"))
                {
                    animator.AnimationSetTrigger(attackName);
                    currentActiveAttack = attackName;
                }
            }
            else
            {
                isAttacking = true;
                animator.AnimationSetTrigger(attackName);
                currentActiveAttack = attackName;
                // animator.AnimationSetBool("IsRunning", false);
                // movementController.StopRun();
                framesIntoAttack = 0;
            }
        }
    }

    public void Attack5B()
    {
        GroundedAttackFlags("5B");
    }
    public void Attack5C()
    {
        GroundedAttackFlags("5C");
    }
    public void AttackJ5B()
    {
        AirAttackFlags("J5B");
    }
    public void AttackJ5C()
    {
        // GroundedAttackFlags("5C");
    }
    public void Throw(bool isForward)
    {
        if (movementController.isGrounded)
        {
            if (!isAttacking)
            {
                if (animator.AnimationGetBool("IsRunning") && !animator.AnimationGetBool("IsSkidding")) {
                    movementController.Skid();
                }
                isAttacking = true; // TODO: Do we need a throw flag?
                playerState.SetThrowDirection(isForward);
                animator.AnimationSetBool("ThrowWhiff", true);
                animator.AnimationSetBool("IsRunning", false);
                movementController.StopRun();
            }
        }
    }
    
    public void ThrowFreeze()
    {
        movementController.ThrowHit();
    }
    public void ThrowUnFreeze()
    {
        movementController.ThrowEnd();
        // MoveDone();
    }

    public void Startup()
    {
        ResetCancel();
        SoundManagerController.playSFX(SoundManagerController.whiffLvl1Sound);
    }
    public void Active() {
        // Activate hitbox and hurtbox
    }
    public void Recovery() {
        // Deactivate hitbox
        ResetCancel();
    }
    public void MoveDone() {
        // Deactivate hurtbox
        ResetAttackStateToNeutral();
    }
    public void Cancel() {
        ResetAttackStateToNeutral();
    }
    public void SetCancel()
    {
        animator.AnimationSetBool("CanCancel", true);
    }
    public void ResetCancel()
    {
        animator.AnimationSetBool("CanCancel", false);
    }

    public void InputBufferCancel(int frameLimit) {
        if (
            framesIntoAttack <= frameLimit &&
            !animator.AnimationGetBool("ThrowHit") &&
            !animator.AnimationGetBool("ThrowWhiff")
        ) {
            ResetAttackStateToNeutral();
            animator.AnimationSetTrigger("InputBufferCancel");
        }
    }

    public void RC(int frameLimit) {
        if (!animator.AnimationGetBool("ThrowHit")) {
            ResetAttackStateToNeutral();
            movementController.RC();
            // stop airdash movement
            animator.AnimationSetTrigger("InputBufferCancel");
        }
    }

    public Vector2 FreezePlayer()
    {
        animator.AnimatorEnable(false);
        return movementController.FreezePlayer();
    }

    public void UnFreezePlayer(Vector2 oldVelocity)
    {
        animator.AnimatorEnable(true);
        movementController.UnFreezePlayer(oldVelocity);
    }

    // Must always be called before Recovery frames
    public async Task TriggerHitStop(Attack AttackData)
    {
        SetCancel();
        Vector2 oldVelocity = FreezePlayer();
        // TODO: Do we need to be able to interrupt hitstop? Probably
        await Task.Delay(AttackData.GetHitStop());
        UnFreezePlayer(oldVelocity);
        UseCancelAction();
    }

    public void SetCancelAction(CancelAction action)
    {
        // OnHit / OnBlock cancels
        if (animator.AnimationGetBool("CanCancel"))
        {
            if (action == CancelAction.Jump && 
                (!attackProperties.CanJumpCancel(currentActiveAttack) ||
                movementController.AirActionsLeft <= 0)
            ) {
                currentCancelAction = null;
            }
            else
            {
                // valid cancel action
                currentCancelAction = action;
                // If no hitstop present, use cancel action now!
                if (animator.GetAnimatorEnable())
                {
                    UseCancelAction();
                }
            }
        }
        else
        {
            // Cannot cancel right now
            currentCancelAction = null;
        }
    }
    private void UseCancelAction()
    {
        if (currentCancelAction != null)
        {
            animator.AnimationSetTrigger("ExecutingCancel");
            playerState.UseCancelAction(currentCancelAction);
            currentCancelAction = null;
        }
    }

    private void LandCancelHandler(object sender, LandEventArgs e) {
        if (isAttacking) {
            ResetAttackStateToNeutral();
            animator.AnimationSetTrigger("ExecutingCancel");
            animator.AnimationSetBool("CanCancel", false);
        }
    }


    public void ResetAttackStateToNeutral()
    {
        animator.AnimationSetBool("5B", false);
        animator.AnimationSetBool("5C", false);
        animator.AnimationResetTrigger("J5B");
        animator.AnimationSetBool("ThrowWhiff", false);
        animator.AnimationSetBool("ThrowHit", false);
        currentActiveAttack = null;
        isAttacking = false;
    }
}