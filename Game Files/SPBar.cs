using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;


class SPBar : AnimationSprite
{
    float sp1;
    Sprite fill;
    Sprite border;
    public SPBar(TiledObject obj = null) : base("Assets/Boxes/ultimate_bar_background.png", 1, 1, -1, false, false)
    {
        bool invert = obj.GetBoolProperty("Inverted");
        fill = new Sprite("Assets/Boxes/ultimate_bar_fill.png");

        if (invert)
        {
            fill.SetOrigin(0, fill.height / 2);
            fill.SetXY(width / 2 + 1, 0);
        }
        else
        {
            fill.SetOrigin(0, fill.height / 2);
            fill.SetXY(-width / 2 + 1, 0);
        }
        AddChild(fill);

        border = new Sprite("Assets/Boxes/ultimate_bar.png");
        if (invert)
        {
            border.SetOrigin(0, border.height / 2);
            border.SetXY(width / 2, 0);
        }
        else
        {
            border.SetOrigin(0, border.height / 2);
            border.SetXY(-width / 2, 0);
        }
        AddChild(border);
    }
    public void SetUpBar(Character player)
    {
        player.BarFill += UIUpdate;
        sp1 = (fill.scaleX - fill.scaleX * 0.15f) / player.GetMaxSP();
        fill.SetScaleXY(0.15f, 1);
    }
    public void UIUpdate(float Chp)
    {
        fill.SetScaleXY(0.15f+sp1 * Chp, 1);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
