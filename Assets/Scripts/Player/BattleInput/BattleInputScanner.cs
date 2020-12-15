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
public enum ButtonStatus
{
    Down, // Pressed down on this frame!
    Hold, // Button was pressed earlier, being held down now
    Release, // Button was released this frame! (Negative edge)
    Up // Button is in neutral (not pressed) state
}

/// Mainly responsible for managing input history
public class BattleInputScanner : MonoBehaviour
{
    private static readonly int ButtonCount = 4; // A B C D
    public int InputHistorySize; // size for input history
    private int runningFrames; // How much time (in frames) since last input?

    private BattleInputParser parser;

    // Input history data
    public IList<Numpad> inputHistory { get; private set; }
    public IList<IList<ButtonStatus>> buttonHistory { get; private set; }
    public IList<int> timeHistory { get; private set; }

    // Received inputs from ControllerReader
    private Numpad nextDirection;
    private IList<ButtonStatus> nextButtons;
    // new inputs must be added to the input history on the next frame!
    private bool newInputs;

    void Start()
    {
        runningFrames = 0; // Frame 1 will be first update frame
        inputHistory = new List<Numpad>();
        buttonHistory = new List<List<ButtonStatus>>();
        timeHistory = new List<int>();

        parser = GetComponent<BattleInputParser>();

        nextDirection = Numpad.N0;
        nextButtons = new List<ButtonStatus>();
        for (int j = 0; j < ButtonCount; j++) {
            nextButtons.Add(ButtonStatus.Up);
        }
        newInputs = false;

        // initialize input history
        for (int i = 0; i < InputHistorySize; i++)
        {
            inputHistory.Add(Numpad.N0);
            timeHistory.Add(0);

            IList<ButtonStatus> emptyButtons = new List<ButtonStatus>();
            for (int j = 0; j < ButtonCount; j++) {
                emptyButtons.Add(ButtonStatus.Up);
            }
            buttonHistory.Add(emptyButtons);
        }
    }

    // every frame update
    void Update()
    {
        runningFrames++;
        
        if (newInputs) {
            // Add all received inputs to input history
            inputHistory.Insert(0, nextDirection);
            IList<ButtonStatus> copyButtons = new List<ButtonStatus>();
            for (int i = 0; i < ButtonCount; i++) {
                copyButtons[i] = nextButtons[i];
            }
            buttonHistory.Insert(0, copyButtons);
            timeHistory.Insert(0, runningFrames);

            // trim input history size
            if (inputHistory.Count > InputHistorySize)
            {
                inputHistory.RemoveAt(InputHistorySize);
                buttonHistory.RemoveAt(InputHistorySize);
                timeHistory.RemoveAt(InputHistorySize);
            }
        
            // pass data to input parser

            // reset flags and running state
            newInputs = false;
            runningFrames = 0;
        }
        
    }

    /// Called when new input received
    /// takes a numpad direction
    /// modifies input and time History
    public void InterpretNewStickInput(Numpad newInput)
    {
        nextDirection = newInput;
    }

    // TODO: Change Controller Reader to detect button release (and maybe hold)
    public void InterpretNewButtonInput(Button buttonPressed)
    {
        switch (buttonPressed) {
            case Button.A:
                nextButtons[0] = ButtonStatus.Down;
                break;
            case Button.B:
                nextButtons[1] = ButtonStatus.Down;
                break;
            case Button.C:
                nextButtons[2] = ButtonStatus.Down;
                break;
            case Button.D:
                nextButtons[3] = ButtonStatus.Down;
                break;
            default:
                throw new InvalidOperationException(buttonPressed + " is not an ABCD button!");
                break;
        }
    }
    

    // TODO: Connect to player's turnaround event somehow
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