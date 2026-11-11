using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;                           // System.Drawing contains drawing tools such as Color definitions
using System.Collections.Generic;
public class MyGame : Game
{
    public StartScreen StartScreen;
    public CharacterSelectLevel CharacterSelect;
    public UILevel UILevel;
    public GameLevel GameLevel;
    public PlayAgainLevel PlayAgain;
    public Controller1 controller1;
    public Controller2 controller2;
    // Logical screen size = the size of the Tiled levels (928x544), so they fill the
    // screen. The browser build scales this to fit the canvas and centers it.
    public MyGame() : base(928, 544, true, false, 1920, 1080, true)
    {
        game.targetFps = 60;
        controller1 = new Controller1();
        AddChild(controller1);

        controller2 = new Controller2();
        AddChild(controller2);

        LoadStartLevel("StartScreen.tmx");
    }

    public void LoadGameLevel(string filename)
    {
        int char1 = CharacterSelect.manager.GetIndex1();
        int char2 = CharacterSelect.manager.GetIndex2();
        if (StartScreen != null)
            StartScreen.Destroy();
        if (CharacterSelect != null)
            CharacterSelect.Destroy();      
        if (GameLevel != null)
            GameLevel.Destroy();
        GameLevel = new GameLevel(filename,char1,char2);
        AddChild(GameLevel);

        LoadUILevel("UI.tmx");
    }
    public void LoadCharecterSelect(string filename)
    {
        if (StartScreen != null)
            StartScreen.Destroy();
        if (PlayAgain != null)
            PlayAgain.Destroy();
        if (UILevel != null)
            UILevel.Destroy();
        if (GameLevel != null)
            GameLevel.Destroy();
        if (CharacterSelect != null)
            CharacterSelect.Destroy();
        CharacterSelect = new CharacterSelectLevel(filename);
        AddChild(CharacterSelect);
    }

    public void LoadUILevel(string filename)
    {
        if (UILevel != null)
            UILevel.Destroy();
        UILevel = new UILevel(filename);
        AddChild(UILevel);
    }
    public void LoadStartLevel(string filename)
    {
        if (PlayAgain != null)
            PlayAgain.Destroy();
        if (UILevel != null)
            UILevel.Destroy();
        if (GameLevel != null)
            GameLevel.Destroy();
        if (CharacterSelect != null)
            CharacterSelect.Destroy();
        StartScreen = new StartScreen(filename);
        AddChild(StartScreen);
    }

    public void LoadPlayAgainLevel(string filename)
    {
        if (StartScreen != null)
            StartScreen.Destroy();
        if (UILevel != null)
            UILevel.Destroy();
        if (GameLevel != null)
            GameLevel.Destroy();
        if (CharacterSelect != null)
            CharacterSelect.Destroy();
        PlayAgain = new PlayAgainLevel(filename);
        AddChild(PlayAgain);
    }
    public void LoadCurrentLevel()
    {
        LoadGameLevel(GameLevel.currentlevelName);
    }

    void Update()
    {
    }

    static void Main()
    {
        new MyGame().Start();
    }


}