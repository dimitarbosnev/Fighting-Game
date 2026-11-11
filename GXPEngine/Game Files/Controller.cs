using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;

public class Controller : GameObject
{
    protected Sound click1;
    protected Sound click2;
    protected Sound click3;

    public Controller()
    {
        click1 = new Sound("Assets/Sound/UI/Click1.wav");
        click2 = new Sound("Assets/Sound/UI/Click2.wav");
        click3 = new Sound("Assets/Sound/UI/Start1.ogg");
    }
    public virtual void CotrollerInputs()
    { }
    public virtual void Update()
    {
        CotrollerInputs();
    }


}

