using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;
public class StartScreen : Level
{
    int delay;
    Sound title;
    SoundChannel music;
    Sound click4;
    //bool respawn = false; used for smart respawn
    public StartScreen(string filename) : base(filename, 0,0)
    {
        SetXY(0, 50);
        title = new Sound("Assets/Sound/VoiceLines/Title_screen.wav");
        music = new Sound("Assets/Sound/Music/Music3.wav",true).Play(false,0,0.6f);
        click4 = new Sound("Assets/Sound/UI/Start2.wav");
    }
    void Update()
    {
        if(Input.GetKeyDown(Key.P) && delay == 0)
        {
            title.Play();
            delay = 60;
            click4.Play();
        }
        if(delay == 1)
        {
            music.IsPaused = true;
            ((MyGame)game).LoadCharecterSelect("CharecterSelect.tmx");
        }
        if (delay > 1)
            delay--;
    }
}