using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;


public class CharacterSelectLevel : Level
{
    public CharacterSelectManager manager;

    public CharacterSelectLevel(string filename) : base(filename,0,0)
    {
        Sound charSelect = new Sound("Assets/Sound/VoiceLines/ChareterSelect.wav");
        charSelect.Play(false,0,0.7f);

        //Console.WriteLine("Game Width: " + game.width);
    }

    public override void CreateLevel(int i, int y)
    {
        loader.highQualityText = true;
        loader.rootObject = this;
        loader.autoInstance = true;
        loader.addColliders = false;
        loader.LoadImageLayers();
        loader.LoadTileLayers();
        loader.addColliders = true;
        loader.LoadObjectGroups();
        manager = FindObjectOfType<CharacterSelectManager>();
       manager.AddCharaters(FindObjectsOfType<CharacterIcon>());
        manager.AddImages(FindObjectsOfType<CharacterArt>());
    }
}

