using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;


public class PlayerColl : Sprite
{
    public PlayerColl() : base("Assets/Boxes/HurtBox.png")
    {
        visible = false;
        SetOrigin(width / 2, height / 2);
    }
}
