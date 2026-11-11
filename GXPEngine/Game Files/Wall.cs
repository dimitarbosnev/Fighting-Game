using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using TiledMapParser;

public class Wall : AnimationSprite
{
    public Wall(TiledObject obj = null) : base("Assets/Boxes/HurtBox.png",1,1)
    {
        visible = false;
    }
}
