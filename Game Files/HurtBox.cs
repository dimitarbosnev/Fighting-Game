using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;

public class HurtBox : Sprite
{
    public HurtBox() : base("Assets/Boxes/HurtBox.png")
    {
        SetOrigin(width / 2, height / 2);
        this.visible = false;
        collider.isTrigger = true;
    }


}

