using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using TiledMapParser;
public class InsertCoin : AnimationSprite
{
    bool pressed;
    int i;
    Sprite text;
    public InsertCoin(TiledObject obj = null) : base("Assets/Boxes/InsertCoin.png", 1, 1)
    {
        text = new Sprite("Assets/Boxes/InsertCoinBlack.png");
        text.SetOrigin(text.width / 2, text.height / 2);
        text.visible = false;
        AddChild(text);
    }

    void Update()
    {
        if (!pressed)
        {
            if (i > 0)
                visible = true;
            else
                visible = false;
            if (i < -30)
                i = 30;
            i--;
        }
        if(pressed)
        {
            if (i % 5 == 0)
                text.visible = true;
            else
                text.visible = false;
            if(i > 0)
            i--;
        }
        if (Input.GetKeyDown(Key.P))
        {
            i = 30;
            pressed = true;
            visible = true;
        }
    }
}
