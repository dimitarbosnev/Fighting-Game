using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using TiledMapParser;

public class Ground : AnimationSprite
{
    public Ground(TiledObject obj = null) : base("Assets/Background/Background_layer2.png", 1,1)
    {
        visible = false;
    }
}

