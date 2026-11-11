using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;

public class Controller2 : Controller
{
    public event Action<int> ControllerInput2;
    public event Action<int> SecondaryControllerInput2;

    public Controller2() : base()
    {

    }
    public override void CotrollerInputs()
    {
        if (Input.GetKeyUp(Key.LEFT) || Input.GetKeyUp(Key.RIGHT) || Input.GetKeyUp(Key.DOWN) || Input.GetKeyUp(Key.UP))
            GetInput(0);
        //Move Inputs
        //Left
        if (Input.GetKey(Key.LEFT))
        {
            //LeftUp
            if (Input.GetKey(Key.UP))
                GetInput(2);
            //LeftDown
            else if (Input.GetKey(Key.DOWN))
                GetInput(8);
            else
                GetInput(1);
        }

        //Right
        if (Input.GetKey(Key.RIGHT))
        {
            //RightUP
            if (Input.GetKey(Key.UP))
                GetInput(4);
            //RightDown
            else if (Input.GetKey(Key.DOWN))
                GetInput(6);
            else
                GetInput(5);
        }

        //Up
        if (Input.GetKey(Key.UP) && !Input.GetKey(Key.LEFT) && !Input.GetKey(Key.RIGHT))
            GetInput(3);

        //Down
        if (Input.GetKey(Key.DOWN) && !Input.GetKey(Key.LEFT) && !Input.GetKey(Key.RIGHT))
            GetInput(7);

        //Attack Inputs
        //LP
        if (Input.GetKeyDown(Key.H))
            GetInput(9);
        //HP
        if (Input.GetKeyDown(Key.J))
            GetInput(10); ;
        //LK
        if (Input.GetKeyDown(Key.K))
            GetInput(11);
        //HK
        if (Input.GetKeyDown(Key.L))
            GetInput(12);

        //Secondary Inputs
        if (Input.GetKeyDown(Key.LEFT))
            SecondaryGetInput(1);

        if (Input.GetKeyDown(Key.RIGHT))
            SecondaryGetInput(2);

        if (Input.GetKeyDown(Key.K))
            SecondaryGetInput(3);

        if (Input.GetKeyDown(Key.UP))
            SecondaryGetInput(4);

        if (Input.GetKeyDown(Key.DOWN))
            SecondaryGetInput(5);
    }

    void GetInput(int i)
    {

        if (ControllerInput2 != null)
            ControllerInput2(i);
    }

    void SecondaryGetInput(int i)
    {
        if (SecondaryControllerInput2 != null)
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
            SecondaryControllerInput2(i);
        }
    }
}

