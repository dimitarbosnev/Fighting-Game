using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;
public class CharacterIcon : AnimationSprite
{
    Sprite p1;
    Sprite p2;
    AnimationSprite sprite;
    public CharacterIcon(TiledObject obj = null) : base("Assets/Boxes/HurtBox.png", 1,1,-1,false,false)
    {
        //alpha = 0;
        string filename = obj.GetStringProperty("image", "Assets/Boxes/HurtBox.png");
        sprite = new AnimationSprite(filename, 1, 1, -1, false, false);
        sprite.SetOrigin(sprite.width / 2, sprite.height / 2);
        sprite.SetScaleXY(0.5f);
        AddChild(sprite);

        p1 = new Sprite("Assets/Boxes/Player1.png");
        p1.SetScaleXY(0.5f);
        p1.SetXY(-width/2+p1.width/4 - 1,height/2-p1.height-0);
        p1.visible = false;
        AddChild(p1);

        p2 = new Sprite("Assets/Boxes/Player2.png");
        p2.SetScaleXY(0.5f);
        p2.SetXY(width / 2 - p2.width*1.2f + 1, height / 2 - p2.height - 0);
        p2.visible = false;
        AddChild(p2);
    }

    public void P1Vis()
    {
        p1.visible = true;
    }

    public void P2Vis()
    {
        p2.visible = true;
    }

    public void Reset()
    {
        p1.visible = false;
        p2.visible = false;
    }
}

