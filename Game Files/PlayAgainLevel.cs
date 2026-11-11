using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;
public class PlayAgainLevel : Level
{
    bool index1 = true;
    bool index2 = true;
    bool select1 = false;
    bool select2 = false;
    List<Button> buttons = new List<Button>();
    SoundChannel music;
    int delay = 30;
    //bool respawn = false; used for smart respawn
    public PlayAgainLevel(string filename) : base(filename, 0, 0)
    {
        ((MyGame)game).controller1.SecondaryControllerInput1 += AddInput1;
        ((MyGame)game).controller2.SecondaryControllerInput2 += AddInput2;
        music = new Sound("Assets/Sound/Music/Music4.wav", true).Play(false,0,0.7f);
    }

    public override void CreateLevel(int i = 0, int y = 0)
    {
        loader.highQualityText = true;
        loader.rootObject = this;
        loader.autoInstance = true;
        loader.addColliders = false;
        loader.LoadImageLayers();
        loader.LoadTileLayers();
        loader.addColliders = true;
        loader.LoadObjectGroups();

        buttons.AddRange(FindObjectsOfType<Button>());
    }

    void Update()
    {
        if (select1 && select2)
        {
            delay--;
        }
        if (delay == 0)
        {
            if (index1 && index2)
            {
                music.IsPaused = true;
                ((MyGame)game).LoadCharecterSelect("CharecterSelect.tmx");
            }
            else
            {
                music.IsPaused = true;
                ((MyGame)game).LoadStartLevel("StartScreen.tmx");
            }
        }

        if (index2)
        { 
            buttons[3].visible = true;
            buttons[1].visible = false;
        }
        else
        {
            buttons[1].visible = true;
            buttons[3].visible = false;
        }

        if (index1)
        {
            buttons[2].visible = true;
            buttons[0].visible = false;
        }
        else
        {
            buttons[0].visible = true;
            buttons[2].visible = false;
        }
    }

    void AddInput1(int i)
    {
        if (!select1)
        {
            if (i == 4 || i == 5)
            {
                if (index1)
                    index1 = false;
                else
                    index1 = true;
            }
            if (i == 3)
                select1 = true;
        }
        else if (select1 && i == 3)
            select1 = false;
        Console.WriteLine("Index1: " + index1);
    }
    void AddInput2(int i)
    {
        if (!select2)
        {
            if (i == 4 || i == 5)
            {
                if (index2)
                    index2 = false;
                else
                    index2 = true;
            }
            if (i == 3)
                select2 = true;
        }
        else if  (select2 && i == 3)
            select2 = false;
        Console.WriteLine("Index2: " + index2);
    }

    protected override void OnDestroy()
    {
        ((MyGame)game).controller1.SecondaryControllerInput1 -= AddInput1;
        ((MyGame)game).controller2.SecondaryControllerInput2 -= AddInput2;
        base.OnDestroy();
    }
}
