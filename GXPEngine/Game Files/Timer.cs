using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;
using GXPEngine.Core;

public class Timer : AnimationSprite
{
    EasyDraw time;
    public Timer(TiledObject obj = null) : base("Assets/Boxes/Box.png", 1, 1, -1, false, false)
    {
        alpha = 0;
        time = new EasyDraw(100, 100);
        time.SetOrigin(time.width / 2, time.height / 2);
        time.color = 0x000000;
        time.TextAlign(CenterMode.Center, CenterMode.Center);
        time.TextFont(Utils.LoadFont("Retro Gaming.ttf", 30));
        time.Text("99");
        AddChild(time);
    }

    public void Update1(int i)
    {
        time.ClearTransparent();
        time.Text("" + i);
    }
}

