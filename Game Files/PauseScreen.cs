using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using TiledMapParser;
public class PauseScreen : AnimationSprite
{
    public PauseScreen(TiledObject obj = null) : base("Assets/Background/PauseMenu4.png", 1, 1)
    {
        visible = false;
    }
}
