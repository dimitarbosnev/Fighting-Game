using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;

public class Controller1 : Controller
{
    public event Action<int> ControllerInput1;
    public event Action<int> SecondaryControllerInput1;
    public Controller1() : base()
    {

    }
    public override void CotrollerInputs()
    {
        if (Input.GetKeyUp(Key.A) || Input.GetKeyUp(Key.D) || Input.GetKeyUp(Key.S) || Input.GetKeyUp(Key.W))
            GetInput(0);
        //Move Inputs
        //Left

        if (Input.GetKey(Key.A))
        {
            //LeftUp
            if (Input.GetKey(Key.W))
                GetInput(2);
            //LeftDown
            else if (Input.GetKey(Key.S))
                GetInput(8);
            else
                GetInput(1);
        }

        //Right
        if (Input.GetKey(Key.D))
        {
            //RightUP
            if (Input.GetKey(Key.W))
                GetInput(4);
            //RightDown
            else if (Input.GetKey(Key.S))
                GetInput(6);
            else
                GetInput(5);
        }

        //Up
        if (Input.GetKey(Key.W) && !Input.GetKey(Key.A) && !Input.GetKey(Key.D))
            GetInput(3);

        //Down
        if (Input.GetKey(Key.S) && !Input.GetKey(Key.A) && !Input.GetKey(Key.D))
            GetInput(7);

        //Attack Inputs
        //LP
        if (Input.GetKeyDown(Key.Z))
            GetInput(9);
        //HP
        if (Input.GetKeyDown(Key.X))
            GetInput(10);
        //LK
        if (Input.GetKeyDown(Key.C))
            GetInput(11);
        //HK
        if (Input.GetKeyDown(Key.V))
            GetInput(12);

        //Secondary Inputs
        if (Input.GetKeyDown(Key.A))
            SecondaryGetInput(1);
  
        if (Input.GetKeyDown(Key.D))
            SecondaryGetInput(2);

        if (Input.GetKeyDown(Key.C))
            SecondaryGetInput(3);

        if (Input.GetKeyDown(Key.W))
            SecondaryGetInput(4);

        if (Input.GetKeyDown(Key.S))
            SecondaryGetInput(5);

    }

    void GetInput(int i)
    {
        if (ControllerInput1 != null)
            ControllerInput1(i);
    }

    void SecondaryGetInput(int i)
    {
        if (SecondaryControllerInput1 != null)
        {
            if (i == 1 || i == 2)
            {
                click1.Play();
            }
            if (i == 4 || i == 5)
            {
                click2.Play();
            }
            if (i == 3)
            {
                click3.Play();
            }
            SecondaryControllerInput1(i);
        }
    }
}
