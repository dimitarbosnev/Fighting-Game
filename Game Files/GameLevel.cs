using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;
public class GameLevel : Level
{
    public List<Character> players = new List<Character>();
   // Character Hitter;
    Background background;
    public Timer timer;
    int millis;
    int time = 99;
    public PauseScreen pauseScreen;
    public bool pause;
    public bool roundEnd = false;
    public bool roundBegin = false;
    public bool gameEnd = false;
    int pausetime = 0;
    int delay = 0;
    int RoundCount = 0;
    int P1Round;
    int P2Round;
    Level Texter;
    SoundChannel music;
    public Indication[] indi = new Indication[4];
    Sound[] win = new Sound[2];
    //Announcer Announcer;
    //bool respawn = false; used for smart respawn
    public GameLevel(string filename, int i, int y) : base(filename, i, y)
    {
        music = new Sound("Assets/Sound/Music/Music1.wav", true).Play(false, 0, 0.3f);
        millis = time * 1000;
        win[0] = new Sound("Assets/Sound/SFX/SE_00020.wav");
        win[1] = new Sound("Assets/Sound/SFX/SE_00021.wav");
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
        loader.AddManualType();
        loader.LoadObjectGroups();
        AddPlayers(i, y);
        StartGame(120);
        background = FindObjectOfType<Background>();
        background.visible = false;
    }
    void AddPlayers(int i, int y)
    {
        HPBar[] hPbars = FindObjectsOfType<HPBar>();
        players.Add(AssignChareter(i, true));
        players.Add(AssignChareter(y, false));
        hPbars[0].SetUpBar(players[0]);
        hPbars[1].SetUpBar(players[1]);
        Reset();
        foreach (Character a in players)
        {
            a.AddGround(FindObjectsOfType<Ground>());
            a.AddWalls(FindObjectsOfType<Wall>());
            a.SetOpponent(players.Find(x => x != a));
            a.level = this;
            a.HitConfirm += Pause;
            a.TakeDamage += CheckPlayer;
            a.Super += UltBack;
            AddChild(a);
        }
    }
    void HitPause()
    {
        pausetime--;
    }
    void Pause(AttackClass attack)
    {
        pausetime = attack.HitStop();
        pause = true;
        this.AddChildAt(attack.parent, this.GetChildCount() - 1);
        //Hitter = attack.parent as Character;
    }
    void Update()
    {
        if (!pause && !roundEnd && !roundBegin && !gameEnd)
        {
            millis -= Time.deltaTime;
            if (millis / 1000 <= time)
            {
                if (time > 0)
                    time--;
                timer.Update1(time);
            }
            if (time == 0)
            {
                if (players[0].GetcurrentHP() > players[1].GetcurrentHP())
                {
                    players[1].Kill();
                }
                else if (players[0].GetcurrentHP() < players[1].GetcurrentHP())
                {
                    players[0].Kill();
                }
                else
                {
                    players[Utils.Random(0, 2)].Kill();
                }
                CheckPlayer(0);
            }
        }
        if (Input.GetKeyDown(Key.P))
        {
            pauseScreen.visible = !pauseScreen.visible;
            if (pauseScreen.visible == true)
            {
                pausetime = -1;
                pause = true;
            }
            else
                pausetime = 180;
        }
        if (pausetime == 0)
            pause = false;
        if (pause && pausetime > 0)
            pausetime--;
        if (roundEnd)
            RoundEnd();
        if (roundBegin)
            RoundBegin();
        if (gameEnd)
            GameEnd();
    }
    protected override void OnDestroy()
    {
        foreach (Character a in players)
        {
            a.HitConfirm -= Pause;
            a.TakeDamage -= CheckPlayer;
            a.Super -= UltBack;
        }
        base.OnDestroy();
    }

    Character AssignChareter(int b, bool assign)
    {
        if (b == 0)
            return new Orochi(assign);
        else if (b == 1)
            return new Momo(assign);
        else
            return new Character(assign);
    }
    void CheckPlayer(float i)
    {
        if (!roundEnd && !roundBegin && !gameEnd)
            foreach (Character a in players)
            {
                if (a.GetcurrentHP() <= 0)
                {
                    RoundKO(300);
                    if (a.GetOpponent() == players[0] && P1Round != 2)
                    {
                        P1Round++;
                    }
                    if (a.GetOpponent() == players[1] && P2Round != 2)
                    {
                        P2Round++;
                    }
                    time = 99;
                    millis = time * 1000;
                    break;
                }
            }
    }
    void RoundEnd()
    {
        if (delay == 180)
        {
            if (P1Round > 0)
                indi[P1Round - 1].RoundWon();
            if (P2Round > 0)
                indi[P2Round + 1].RoundWon();
            win[Utils.Random(0, 2)].Play();
        }
        if (delay == 120)
        {
            RemoveChild(Texter);
            Texter.Destroy();
            if (P1Round != 2 && P2Round != 2)
            {
                Texter = new Level("Announcment Maps/Round" + RoundCount + ".tmx", 0, 0);
                roundBegin = true;
                Reset();
                int num = Utils.Random(1, 4);
                Sound a = new Sound("Assets/Sound/VoiceLines/Round" + RoundCount + num + ".wav");
                a.Play();
            }
            if (P1Round == 2)
            {
                Texter = new Level("Announcment Maps/P1wins.tmx", 0, 0);
                gameEnd = true;
                if (players[0] is Orochi)
                {
                    Sound a = new Sound("Assets/Sound/VoiceLines/Kumagawa_Wins.wav");
                    a.Play();
                }
                else if (players[0] is Character)
                {
                    Sound a = new Sound("Assets/Sound/VoiceLines/Momo_Wins.wav");
                    a.Play();
                }
            }
            if (P2Round == 2)
            {
                Texter = new Level("Announcment Maps/P2wins.tmx", 0, 0);
                gameEnd = true;
                if (players[1] is Orochi)
                {
                    Sound a = new Sound("Assets/Sound/VoiceLines/Kumagawa_Wins.wav");
                    a.Play();
                }
                else if (players[1] is Character)
                {
                    Sound a = new Sound("Assets/Sound/VoiceLines/Momo_Wins.wav");
                    a.Play();
                }
            }
            AddChild(Texter);
            roundEnd = false;
        }
        if (delay > 120)
            delay--;
    }
    void GameEnd()
    {
        if (delay == 0)
        {
            music.IsPaused = true;
            ((MyGame)game).LoadPlayAgainLevel("PlayAgain.tmx");
        }
        if (delay > 0)
            delay--;
    }
    void RoundBegin()
    {
        if (delay == 30)
        {
            RemoveChild(Texter);
            Texter.Destroy();
            Texter = new Level("Announcment Maps/Fight.tmx", 0, 0);
            AddChild(Texter);
        }
        if (delay == 0)
        {
            RemoveChild(Texter);
            Texter.Destroy();
            roundBegin = false;
        }
        if (delay > 0)
            delay--;
    }
    void Reset()
    {
        players[0].SetXY(256, 420);
        players[0].ResetHealth();
        players[0].SetState(Character.State.Netural);
        players[1].SetXY(672, 420);
        players[1].ResetHealth();
        players[1].SetState(Character.State.Netural);

    }
    void RoundKO(int i)
    {
        RoundCount++;
        delay = i;
        Texter = new Level("Announcment Maps/KORound.tmx", 0, 0);
        int num = Utils.Random(1, 3);
        Sound a = new Sound("Assets/Sound/VoiceLines/KO" + num + ".wav");
        a.Play();
        AddChild(Texter);
        roundEnd = true;
    }
    void StartGame(int i)
    {
        RoundCount++;
        delay = i;
        Texter = new Level("Announcment Maps/KORound.tmx", 0, 0);
        AddChild(Texter);
        roundEnd = true;
    }
    void UltBack(bool f)
    {
        if (f)
        {
            background.visible = true;
            background.SetTime(20);
            //pausetime = 20;
            //pause = true;
        }
        else
        {
            background.visible = false;
            background.SetTime(0);
        }
    }
}