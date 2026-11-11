using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using TiledMapParser;

class Button : Background
{
    public Button(TiledObject obj = null)
    {
        alpha = obj.GetFloatProperty("alpha", 1f);
        visible = false;
    }
}

