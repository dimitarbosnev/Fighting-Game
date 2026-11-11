using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;
using GXPEngine.Core;
public class Indication : AnimationSprite
{
    Sprite won;
    public Indication(TiledObject obj = null) : base("Assets/Boxes/Round_Empty.png",1,1,-1,false,false)
    {
        won = new Sprite("Assets/Boxes/Round_Won.png");
        won.SetOrigin(won.width / 2, won.height / 2);
        won.visible = false;
        AddChild(won);
    }

    public void RoundWon()
    {
        won.visible = true;
    }
}

