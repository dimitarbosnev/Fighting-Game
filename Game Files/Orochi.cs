using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;
using GXPEngine.Core;

class Orochi : Character
{
    public Orochi(bool assign) : base(assign)
    {

    }
    protected override void SetStats()
    {
        MaxHealth = 1100;
        currentHealth = MaxHealth;
        MaxPower = 500;
        currentPower = 0;
    }
    protected override void Initalize()
    {
        //Attacks
        LightPunch = new AttackClass("Assets/Charecter 1/C1 LightPunch.png", 5, 1, 5, 2, 10);
        LightPunch.SetAttackProperties(true, false);
        LightPunch.SetOrigin(LightPunch.width / 2, LightPunch.height / 2);
        LightPunch.SetCycle(0, 5, 4);
        LightPunch.SetXY(0, -8);
        AddChild(LightPunch);
        LightPunch.visible = false;

        HardPunch = new AttackClass("Assets/Charecter 1/C1 HardPunch.png", 6, 1, 6, 3, 14);
        HardPunch.SetAttackProperties(true, false);
        HardPunch.SetOrigin(HardPunch.width / 2, HardPunch.height / 2);
        HardPunch.SetCycle(0, 6, 3);
        HardPunch.SetXY(0, -8);
        AddChild(HardPunch);
        HardPunch.visible = false;

        LightKick = new AttackClass("Assets/Charecter 1/C1 LightKick.png", 5, 1, 5, 2, 8);
        LightKick.SetAttackProperties(false, false);
        LightKick.SetOrigin(LightKick.width / 2, LightKick.height / 2);
        LightKick.SetCycle(0, 5, 3);
        LightKick.SetXY(0, -8);
        AddChild(LightKick);
        LightKick.visible = false;
        //Edit hard kick could become cool special and another hard kick
        HardKick = new AttackClass("Assets/Charecter 1/C1 HardKick.png", 11, 1, 11, 6, 14);
        HardKick.SetAttackProperties(true, false);
        HardKick.SetOrigin(HardKick.width / 2, HardKick.height / 2);
        HardKick.SetCycle(0, 11, 3);
        HardKick.SetXY(0, -8);
        AddChild(HardKick);
        HardKick.visible = false;

        SpecialAttack = new AttackClass("Assets/Charecter 1/C1 SpecialAttack.png", 12, 1, 12, 3, 20);
        SpecialAttack.SetAttackProperties(false, true);
        SpecialAttack.SetOrigin(SpecialAttack.width / 2, SpecialAttack.height / 2);
        SpecialAttack.SetCycle(0, 12, 5);
        SpecialAttack.SetXY(100, -200);
        AddChild(SpecialAttack);
        SpecialAttack.visible = false;

        UltimateAttack = new AttackClass("Assets/Charecter 1/C1 Ultimate.png", 10, 1, 9, 5, 6);
        UltimateAttack.SetAttackProperties(false, true);
        UltimateAttack.SetOrigin(UltimateAttack.width / 2, UltimateAttack.height / 2);
        UltimateAttack.SetCycle(0, 10, 5);
        UltimateAttack.SetXY(100, -200);
        AddChild(UltimateAttack);
        UltimateAttack.visible = false;

        CrouchAttack = new AttackClass("Assets/Charecter 1/C1 CrouchAttack.png", 5, 1, 5, 2, 8);
        CrouchAttack.SetAttackProperties(false, false);
        CrouchAttack.SetOrigin(CrouchAttack.width / 2, CrouchAttack.height / 2);
        CrouchAttack.SetCycle(0, 5, 3);
        CrouchAttack.SetXY(0, -8);
        AddChild(CrouchAttack);
        CrouchAttack.visible = false;

        JumpAttack = new AttackClass("Assets/Charecter 1/C1 JumpAttack.png", 3, 1, 1, 0, 8);
        JumpAttack.SetAttackProperties(false, true);
        JumpAttack.SetOrigin(JumpAttack.width / 2, JumpAttack.height / 2);
        JumpAttack.SetCycle(0, 1, 20);
        JumpAttack.SetXY(0, -8);
        AddChild(JumpAttack);
        JumpAttack.visible = false;
        //States
        Idle = new AnimationSprite("Assets/Charecter 1/C1 Idle.png", 2, 1, 2, false, false);
        Idle.SetOrigin(Idle.width / 2, Idle.height / 2);
        Idle.SetCycle(0, 2, 16);
        Idle.SetXY(0, -8);
        AddChild(Idle);
        Idle.visible = false;

        Dead = new AnimationSprite("Assets/Charecter 1/C1 KO.png", 8, 1, 8, false, false);
        Dead.SetOrigin(Dead.width / 2, Dead.height / 2);
        Dead.SetCycle(0, 8, 8);
        Dead.SetXY(0, -8);
        AddChild(Dead);
        Dead.visible = false;

        Crouch = new AnimationSprite("Assets/Charecter 1/C1 Crouch.png", 2, 1, 2, false, false);
        Crouch.SetOrigin(Crouch.width / 2, Crouch.height / 2);
        Crouch.SetCycle(0, 2, 10);
        Crouch.SetXY(0, -8);
        AddChild(Crouch);
        Crouch.visible = false;

        Hit = new AnimationSprite("Assets/Charecter 1/C1 Hit.png", 3, 1, 3, false, false);
        Hit.SetOrigin(Hit.width / 2, Hit.height / 2);
        Hit.SetCycle(0, 3, 10);
        Hit.SetXY(0, -8);
        AddChild(Hit);
        Hit.visible = false;

        Block = new AnimationSprite("Assets/Charecter 1/C1 Block.png", 3, 1, 3, false, false);
        Block.SetOrigin(Block.width / 2, Block.height / 2);
        Block.SetCycle(0, 2, 4);
        Block.SetXY(0, -8);
        AddChild(Block);
        Block.visible = false;

        Walk = new AnimationSprite("Assets/Charecter 1/C1 Walk.png", 4, 1, 4, false, false);
        Walk.SetOrigin(Walk.width / 2, Walk.height / 2);
        Walk.SetCycle(0, 4, 4);
        Walk.SetXY(0, -8);
        AddChild(Walk);
        Walk.visible = false;

        Jump = new AnimationSprite("Assets/Charecter 1/C1 Jump.png", 1, 1, 1, false, false);
        Jump.SetOrigin(Jump.width / 2, Jump.height / 2);
        Jump.SetCycle(0, 1, 4);
        Jump.SetXY(0, -8);
        AddChild(Jump);
        Jump.visible = false;

        KnockDown = new AnimationSprite("Assets/Charecter 1/C1 KnockDown.png", 1, 1, 1, false, false);
        KnockDown.SetOrigin(KnockDown.width / 2, KnockDown.height / 2);
        KnockDown.SetCycle(0, 1, 4);
        KnockDown.SetXY(0, -8);
        AddChild(KnockDown);
        KnockDown.visible = false;

        DashForward = new AnimationSprite("Assets/Charecter 1/C1 DashForward.png", 3, 1, 3, false, false);
        DashForward.SetOrigin(DashForward.width / 2, DashForward.height / 2);
        DashForward.SetCycle(0, 3, 4);
        DashForward.SetXY(0, -8);
        AddChild(DashForward);
        DashForward.visible = false;

        DashBackwards = new AnimationSprite("Assets/Charecter 1/C1 DashBackwards.png", 3, 1, 3, false, false);
        DashBackwards.SetOrigin(DashBackwards.width / 2, DashBackwards.height / 2);
        DashBackwards.SetCycle(0, 3, 4);
        DashBackwards.SetXY(0, -8);
        AddChild(DashBackwards);
        DashBackwards.visible = false;

        specialSound = new Sound("Assets/Charecter 1/SpecialMove.wav");
        ultSound = new Sound("Assets/Charecter 1/UltSound.wav");
    }
    void LPAttack()
    {
        LightPunch.ResetAttack();
        SetCurrentAttack(LightPunch);
        SetHurBox(50, -30, 2f, 1f);
        SetAttackDmg(40, 3);
        CurrentAttack = LightPunch;
        attacking = true;
    }
    void HPAttack()
    {
        HardPunch.ResetAttack();
        SetCurrentAttack(HardPunch);
        SetHurBox(60, -35, 2f, 1f);
        SetAttackDmg(70, 4);
        CurrentAttack = HardPunch;
        attacking = true;
    }
    void LKAttack()
    {
        LightKick.ResetAttack();
        SetCurrentAttack(LightKick);
        SetHurBox(40, -40, 2f, 1f);
        hurtBox.rotation = -40;
        SetAttackDmg(40, 3);
        CurrentAttack = LightKick;
        attacking = true;
    }
    void HKAttack()
    {
        CurrentAttack = HardKick;
        CurrentAttack.ResetAttack();
        SetCurrentAttack(CurrentAttack);
        SetHurBox(70, -50, 2f, 1f);
        hurtBox.rotation = -40;
        SetAttackDmg(100, 7, 7);
        attacking = true;

    }
    void CrouchingAttack()
    {
        CrouchAttack.ResetAttack();
        SetCurrentAttack(CrouchAttack);
        SetHurBox(50, 50, 1.5f, 1f);
        SetAttackDmg(50, 4);
        CurrentAttack = CrouchAttack;
        attacking = true;
    }
    void JumpingAttack()
    {
        JumpAttack.ResetAttack();
        SetCurrentAttack(JumpAttack);
        SetHurBox(15, -10, 5f, 1.5f);
        SetAttackDmg(60, 3);
        SetPlayerColl(-40, -40, 2f, 4f);
        playerColl.rotation = -40;
        CurrentAttack = JumpAttack;
        attacking = true;
    }
    void SPAttack()
    {
        CurrentAttack = SpecialAttack;
        CurrentAttack.ResetAttack();
        CurrentAttack.ResetHit(3);
        SetCurrentAttack(CurrentAttack);
        SetHurBox(60, -50, 3f, 6f);
        SetAttackDmg(60, 2, 20);
        SetPlayerColl(0, 0, 0, 0);
        attacking = true;
        specialSound.Play(false, 0, 0.7f);
    }
    void UltAttack()
    {
        CurrentAttack = UltimateAttack;
        CurrentAttack.ResetAttack();
        CurrentAttack.ResetHit(6);
        SetCurrentAttack(CurrentAttack);
        SetHurBox(180, 0, 5f, 5f);
        SetAttackDmg(30, 20, 15);
        SetPlayerColl(0, 0, 0f, 0f);
        attacking = true;
        ultSound.Play(false, 0, 0.7f);
        ResetSpBar();
        TriggerSuper(true);
    }
    protected override void BasicAttackInputs()
    {
        if (ValidInput(9))
        {
            LPAttack();
        }
        else if (ValidInput(10) && grounded)
        {
            HPAttack();
        }
        else if (ValidInput(11))
        {
            if (!ValidInput(6) && !ValidInput(7) && !ValidInput(8) && grounded)
            {
                LKAttack();
            }
            else if (!grounded && !crouching)
            {
                JumpingAttack();
            }
            else if (grounded && (ValidInput(6) || ValidInput(7) || ValidInput(8)))
            {
                CrouchingAttack();
            }
        }
        else if (ValidInput(12) && grounded)
        {
                HKAttack();
        }
    }
    protected override void SpecialAttackInputs()
    {

        if (grounded && DoubleQuarterCircleForward() && (ValidInput(9) || ValidInput(10)) && currentPower >= MaxPower)
        {
            UltAttack();
        }
        else if (grounded && ZMotionForward() && (ValidInput(9) || ValidInput(10)) && (CurrentAttack != SpecialAttack || !attacking))
        {
            SPAttack();
        }
    }
    protected override void AttackAddition()
    {
        if (CurrentAttack == LightPunch)
            if (CurrentAttack.currentFrame == 1 && grounded)
            {
                if (scaleX > 0)
                    vx = Horizontalspeed *2;
                else
                    vx = -Horizontalspeed *2;
            }
        if (CurrentAttack == HardKick)
        {
            if (CurrentAttack.currentFrame >= 1 && CurrentAttack.currentFrame <= 4 && grounded)
            {
                if (scaleX > 0)
                    vx = Horizontalspeed * 2;
                else
                    vx = -Horizontalspeed * 2;
            }
            if (CurrentAttack.currentFrame == 9 && grounded)
            {
                if (scaleX < 0)
                    vx = Horizontalspeed;
                else
                    vx = -Horizontalspeed;
            }
        }
        if (CurrentAttack == SpecialAttack)
        {
            if (CurrentAttack.currentFrame < 6 && CurrentAttack.currentFrame > 3)
            {
                if (scaleX > 0)
                    vx = Horizontalspeed;
                else
                    vx = -Horizontalspeed;
            }

            if (CurrentAttack.currentFrame == 4)
            {
                CurrentAttack.ResetHit(5);
                SetHurBox(70, -100, 2f, 8f);
                SetAttackDmg(80, 2, 15);
            }
            if (CurrentAttack.currentFrame == 6)
            {
                CurrentAttack.ResetHit(7);
                SetHurBox(70, -250, 4f, 4f);
                SetAttackDmg(100, 8, 10);
            }
        }

        if (CurrentAttack == HardPunch)
            if (CurrentAttack.currentFrame == 2)
            {
                if (scaleX > 0)
                    vx = Horizontalspeed * 3;
                else
                    vx = -Horizontalspeed * 3;
            }

        if (CurrentAttack == UltimateAttack)
        {
            if (CurrentAttack.currentFrame == 5)
            {
                CurrentAttack.ResetHit(5);
            }
            if (CurrentAttack.currentFrame == 6)
            {
                SetPlayerColl(20, 20, 3f, 4f);
                SetHurBox(200, -20, 8f, 6f);
                CurrentAttack.ResetHit(6);
            }
        }
    }
}