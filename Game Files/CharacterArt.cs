using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;
public class CharacterArt : AnimationSprite
{
    public List<AnimationSprite> Image = new List<AnimationSprite>();
    public List<AnimationSprite> TextWhite = new List<AnimationSprite>();
    public List<AnimationSprite> TextBlack = new List<AnimationSprite>();
    public int time;
    public CharacterArt(TiledObject obj = null) : base("Assets/Boxes/HurtBox.png", 1, 1, -1, false, false)
    {
        alpha = 0;
        //AddImage("Assets/Character Art/player_0.png");
        AddImage("Assets/Character Art/player_1.png");
        AddImage("Assets/Character Art/player_2.png");
        AddTextWhite("Assets/Character Art/Kumagawa_white.png");
        AddTextBlack("Assets/Character Art/Kumagawa_black.png");
        AddTextWhite("Assets/Character Art/Momo_white.png");
        AddTextBlack("Assets/Character Art/Momo_black.png");
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

    void AddTextWhite(string filename)
    {
        AnimationSprite image = new AnimationSprite(filename, 1, 1, -1, false, false);
        image.SetOrigin(image.width / 2, -image.height);
        image.SetXY(width / 2, 110);
        image.SetScaleXY(1f);
        image.visible = false;
        TextWhite.Add(image);
        AddChild(image);
    }
    void AddTextBlack(string filename)
    {
        AnimationSprite image = new AnimationSprite(filename, 1, 1, -1, false, false);
        image.SetOrigin(image.width / 2, -image.height);
        image.SetXY(width / 2, 110);
        image.SetScaleXY(1f);
        image.visible = false;
        TextBlack.Add(image);
        AddChild(image);
    }
    public void ShowImage(int i)
    {
        foreach (AnimationSprite a in TextWhite)
        {
            a.scaleX = Mathf.Abs(a.scaleX);
            a.visible = false;
        }
        foreach (AnimationSprite a in TextBlack)
            a.scaleX = Mathf.Abs(a.scaleX);
        foreach (AnimationSprite a in Image)
            a.visible = false;
        Image[i].visible = true;
        TextWhite[i].visible = true;

    }

    public void Flicker(int i)
    {
        if (time % 5 == 0)
            TextBlack[i].visible = true;
        else
            TextBlack[i].visible = false;
        if (time > 0)
            time--;
    }

    public void SetTime()
    {
        time = 30;
    }

    public void DeSelect(int i)
    {
        TextBlack[i].visible = false;
    }

    public void FlipText()
    {
        foreach (AnimationSprite a in TextWhite)
            a.scaleX = -Mathf.Abs(a.scaleX);
        foreach (AnimationSprite a in TextBlack)
            a.scaleX = -Mathf.Abs(a.scaleX);
    }
}

