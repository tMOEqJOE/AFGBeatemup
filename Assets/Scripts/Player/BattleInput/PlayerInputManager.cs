﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum Numpad
{
    N0, // Initial state (no inputs detected yet)
    N5, // Neutral
    N1, // Downback
    N2, // Down
    N3, // Downforward
    N4, // Back
    N6, // Forward
    N7, // Upback
    N8, // Up
    N9, // Upforward
}
public enum Button
{
    None, // No buttons
    A, // Light
    B, // Medium
    C, // Heavy
    D // Unique
}
public enum Direction
{
    Down, // i.e. 1, 2, 3  are all down inputsInterpretInput();
    Left,
    Up,
    Right
}

public class PlayerInputManager : MonoBehaviour
{
    public int InputHistorySize; // size for input history
    public float Time66; // in (ms) window to input 66 (dash)
    public float Time236; // in (ms) window to input 236
    
    private PlayerMovementController playerMovement;
    private PlayerAttackController playerAttack;
    private PlayerStateManager playerState;
    private PlayerAnimationController animator;

    private Numpad currentInput; // Current stick input in Numpad
    private float runningTime; // How much time (in ms) since last input?
    private IList<Numpad> inputHistory;
    private IList<float> timeHistory;
    private ConcurrentBag<Button> buttonDownBag;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementController>();
        playerAttack = GetComponent<PlayerAttackController>();
        playerState = GetComponent<PlayerStateManager>();
        animator = GetComponent<PlayerAnimationController>();

        currentInput = Numpad.N0;
        runningTime = 0;
        inputHistory = new List<Numpad>();
        timeHistory = new List<float>();
        buttonDownBag = new ConcurrentBag<Button>();

        for (int i = 0; i < InputHistorySize; i++)
        {
            inputHistory.Add(Numpad.N0);
            timeHistory.Add(0);
        }
    }
    public void Update()
    {
        runningTime += Time.deltaTime;
        InterpretMovement();
        if (!buttonDownBag.IsEmpty)
        {
            InterpretButtons();
        }
    }

    /// Called when new input received
    /// takes a numpad direction
    /// modifies input and time History
    public void InterpretNewStickInput(Numpad newInput)
    {
        currentInput = newInput;
        inputHistory.Insert(0, currentInput);
        timeHistory.Insert(0, runningTime);
        runningTime = 0;
        if (inputHistory.Count > InputHistorySize)
        {
            inputHistory.RemoveAt(InputHistorySize);
            timeHistory.RemoveAt(InputHistorySize);
        }
        InterpretDash();
    }

    public void InterpretNewButtonInput(Button buttonPressed)
    {
        buttonDownBag.Add(buttonPressed);
    }

    private void InterpretMovement()
    {
        Numpad firstInput = inputHistory[0];
        Numpad secondInput = inputHistory[1];

        if (IsNumpadUp(firstInput))
        {
            playerState.SetCancelAction(CancelAction.Jump, firstInput);
            playerMovement.Jump(firstInput);
        }
        else if (!playerMovement.isRunning && (firstInput == Numpad.N6 || firstInput == Numpad.N4))
        {
            // Walk check
            playerMovement.Walk(firstInput);
        }
        else if (playerMovement.isRunning && (firstInput == Numpad.N6 || firstInput == Numpad.N3) && !animator.AnimationGetBool("IsSkidding"))
        {
            // Holding run check
            playerMovement.Run(firstInput);
        }
        else
        {
            if (animator.AnimationGetBool("IsRunning") && !animator.AnimationGetBool("IsSkidding"))
            {
                playerMovement.Skid();
            }
            // Nothing / Idle
        }

        if (!IsNumpadUp(firstInput))
        {
            playerMovement.setIsHoldingJump(false);
        }
    }
    private bool IsNumpadUp(Numpad num)
    {
        return num == Numpad.N7 || num == Numpad.N8 || num == Numpad.N9;
    }

    private void InterpretDash()
    {
        Numpad firstInput = inputHistory[0];
        Numpad secondInput = inputHistory[1];
        Numpad thirdInput = inputHistory[2];
        Numpad fourthInput = inputHistory[3];
        float firstTime = timeHistory[0];
        float secondTime = timeHistory[1];

        if (!playerAttack.isAttacking)
        {
            bool forwardDash = 
                (firstInput == Numpad.N6 && secondInput == Numpad.N5 && (thirdInput == Numpad.N6 || thirdInput == Numpad.N9)) ||
                (firstInput == Numpad.N6 && secondInput == Numpad.N5 && thirdInput == Numpad.N8 && fourthInput == Numpad.N9);
            bool backwardDash = 
                (firstInput == Numpad.N4 && secondInput == Numpad.N5 && (thirdInput == Numpad.N4 || thirdInput == Numpad.N7)) ||
                (firstInput == Numpad.N4 && secondInput == Numpad.N5 && thirdInput == Numpad.N8 && fourthInput == Numpad.N7);
            if (forwardDash && !animator.AnimationGetBool("IsRunning") && !animator.AnimationGetBool("IsSkidding"))
            {
                if (firstTime + secondTime <= Time66)
                {
                    if (playerMovement.isGrounded)
                    {
                        // Grounded forward step dash
                        playerMovement.Dash(firstInput);
                    }
                    else
                    {
                        // forward airdash
                        playerMovement.AirDash(true);
                    }
                }
            }
            else if (backwardDash)
            {
                if (firstTime + secondTime <= Time66)
                {
                    if (playerMovement.isGrounded)
                    {
                        // Grounded backdash
                        playerMovement.BackDash(firstInput);
                    }
                    else
                    {
                        // back airdash
                        playerMovement.AirDash(false);
                    }
                }
            }
        }
    }
    private void InterpretSpecial(Button button)
    {
        Numpad firstInput = inputHistory[0];
        Numpad secondInput = inputHistory[1];
        Numpad thirdInput = inputHistory[2];
        Numpad fourthInput = inputHistory[3];
        float firstTime = timeHistory[0];
        float secondTime = timeHistory[1];
        float thirdTime = timeHistory[2];
        // 236 or 236? motion
        if (thirdInput == Numpad.N2 && secondInput == Numpad.N3 && firstInput == Numpad.N6)
        {
            // Motion detected!
            if (firstTime + runningTime <= Time236)
            {
                if (button == Button.A)
                {
                    Debug.Log("Stun Edge!");
                }
            }
        }
        else if (fourthInput == Numpad.N2 && thirdInput == Numpad.N3 && secondInput == Numpad.N6) 
        {
            // Motion detected!
            if (secondTime + firstTime + runningTime <= Time236)
            {
                if (button == Button.A)
                {
                    Debug.Log("Stun Edge Extra!");
                }
            }
        }
        else
        {
            // Normals
            if (button == Button.A)
            {
                playerAttack.Attack5B();
            }
            else if (button == Button.B)
            {
                playerAttack.Attack5C();
            }
        }
    }

    private void InterpretButtons()
    {
        if (buttonDownBag.IsEmpty)
        {
            throw new InvalidOperationException("Interpretting buttons, but there are no buttons down!");
        }
        Button[] buttonsDown = buttonDownBag.ToArray();
        if (buttonsDown.Contains(Button.A) && buttonsDown.Contains(Button.B))
        {
            // RC?
        }
        else
        {
            if (buttonsDown.Contains(Button.A))
            {
                InterpretSpecial(Button.A);
            }
            else if (buttonsDown.Contains(Button.B))
            {
                InterpretSpecial(Button.B);
            }
            else if (buttonsDown.Contains(Button.C))
            {
                InterpretSpecial(Button.C);
            }
            else if (buttonsDown.Contains(Button.D))
            {
                Numpad firstInput = inputHistory[0];
                switch (firstInput)
                {
                    case Numpad.N6:
                        playerAttack.Throw(true);
                        break;
                    case Numpad.N4:
                        playerAttack.Throw(false);
                        break;
                    default:
                        break;
                }
            }
        }
        // empty out button bag
        while (!buttonDownBag.IsEmpty)
        {
            Button button;
            buttonDownBag.TryTake(out button);
        }
    }

    
    public void FacingDirectionChanged()
    {
        // TODO: can be switch case or something else
        if (currentInput == Numpad.N7)
        {
            InterpretNewStickInput(Numpad.N9);
        }
        else if (currentInput == Numpad.N4)
        {
            InterpretNewStickInput(Numpad.N6);
        }
        else if (currentInput == Numpad.N1)
        {
            InterpretNewStickInput(Numpad.N3);
        }
        else if (currentInput == Numpad.N9)
        {
            InterpretNewStickInput(Numpad.N7);
        }
        else if (currentInput == Numpad.N6)
        {
            InterpretNewStickInput(Numpad.N4);
        }
        else if (currentInput == Numpad.N3)
        {
            InterpretNewStickInput(Numpad.N1);
        }
    }
}