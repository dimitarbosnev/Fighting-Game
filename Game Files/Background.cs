using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using TiledMapParser;
public class Background : AnimationSprite
{
    int time;
    public Background(TiledObject obj = null) : base("Assets/Boxes/Box.png", 1, 1,-1,false,false)
    {
        if (obj != null)
        {
            alpha = obj.GetFloatProperty("alpha", 1f);
            visible = obj.GetBoolProperty("visible", false);
        }
        color = 0x000000;
    }

    void Update()
    {
        if (time > 0)
        {
            if (time % 4 == 0)
            {
                if (color == 0x000000)
                    color = 0x7a7a7a;
                else
                    color = 0x000000;
            }
            if (time == 1)
                color = 0x000000;
            time--;
        }
    }


    public void SetTime(int i)
    {
        time = i;
    }
}