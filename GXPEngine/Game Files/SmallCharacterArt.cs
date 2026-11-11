using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;
public class SmallCharacterArt : AnimationSprite
{
    public List<AnimationSprite> Image = new List<AnimationSprite>();
    public int time;
    public SmallCharacterArt(TiledObject obj = null) : base("Assets/Boxes/HurtBox.png", 1, 1, -1, false, false)
    {
        alpha = 0;
        //AddImage("Assets/Character Art/player_0.png");
        AddImage("Assets/Character Art/player_1_cut.png");
        AddImage("Assets/Character Art/player_2_cut.png");
    }

    void AddImage(string filename)
    {
        AnimationSprite image = new AnimationSprite(filename, 1, 1, -1, false, false);
        image.SetOrigin(image.width / 2, image.height / 2);
        image.SetXY(width / 2, height / 2);
        image.SetScaleXY(1f);
        image.visible = false;
        Image.Add(image);
        AddChild(image);
    }
    public void ShowImage(Character player)
    {
        if (player is Orochi)
            Image[0].visible = true;
        if (player is Momo)
            Image[1].visible = true;
    }
}