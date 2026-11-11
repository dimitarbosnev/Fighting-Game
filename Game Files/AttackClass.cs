using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;
using GXPEngine.Core;

public class AttackClass : AnimationSprite
{
    public enum State {StartUp, Active, Recovery, None}

    bool hit;
    bool cancel;
    bool airjuggle;
    int validFrame;
    int hitStop;
    State current;
    public AttackClass(string filename, int cols, int rows, int frames, int vFrame, int hStop) : base(filename, cols, rows, frames, false, false)
    {
        validFrame = vFrame;
        hitStop = hStop;
        current = State.None;
    }
    public void SetAttackProperties(bool can, bool air)
    {
        cancel = can;
        airjuggle = air;
    }
    public bool FrameValidation()
    {
        if (currentFrame < validFrame)
            current = State.StartUp;
        if (currentFrame > validFrame)
            current = State.Recovery;
        if (!hit && currentFrame == validFrame)
        {
            current = State.Active;
            return true;
        }
        else
            return false;
    }
    public void ResetAttack()
    {
        hit = false;
        currentFrame = 0;
        current = State.None;
    }

    public void ResetHit(int frame)
    {
        hit = false;
        validFrame = frame;
    }

    public void Hit()
    {
        hit = true;
    }

    public bool HitConfirm()
    {
        return hit;
    }

    public int HitStop()
    {
        return hitStop;
    }

    public State GetState()
    {
        return current;
    }

    public bool Cancel()
    {
        return cancel;
    }

    public bool Juggle()
    {
        return airjuggle;
    }
}
