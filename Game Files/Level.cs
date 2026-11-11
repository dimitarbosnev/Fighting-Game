using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;
public class Level : Pivot
{
    protected TiledLoader loader;
    public string currentlevelName;
    //bool respawn = false; used for smart respawn
    public Level(string filename, int i, int y)
    {
        currentlevelName = filename;
        loader = new TiledLoader(filename);
        CreateLevel(i,y);
        //Console.WriteLine("Game Width: " + game.width);
    }

    public virtual void CreateLevel(int i = 0, int y = 0)
    {
        loader.highQualityText = true;
        loader.rootObject = this;
        loader.autoInstance = true;
        loader.addColliders = false;
        loader.LoadImageLayers();
        loader.LoadTileLayers();
        loader.addColliders = true;
        loader.LoadObjectGroups();
    }
}
