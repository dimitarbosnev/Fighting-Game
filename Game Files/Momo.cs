using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;
using GXPEngine.Core;

class Momo : Character
{
    public Momo(bool assign) : base(assign)
    {

    }
    protected override void SetStats()
    {
        MaxHealth = 1000;
        currentHealth = MaxHealth;
        MaxPower = 600;
        currentPower = 0;
    }
    protected override void Initalize()
    {
        //Attacks
        LightPunch = new AttackClass("Assets/Charecter 2/C2 LightPunch.png", 5, 1, 5, 2, 6);
        LightPunch.SetAttackProperties(false, false);
        LightPunch.SetOrigin(LightPunch.width / 2, LightPunch.height / 2);
        LightPunch.SetCycle(0, 5, 4);
        LightPunch.SetXY(0, -8);
        AddChild(LightPunch);
        LightPunch.visible = false;

        HardPunch = new AttackClass("Assets/Charecter 2/C2 HardPunch.png", 5, 1, 5, 3, 15);
        HardPunch.SetAttackProperties(false, false);
        HardPunch.SetOrigin(HardPunch.width / 2, HardPunch.height / 2);
        HardPunch.SetCycle(0, 6, 3);
        HardPunch.SetXY(0, -8);
        AddChild(HardPunch);
        HardPunch.visible = false;

        LightKick = new AttackClass("Assets/Charecter 2/C2 LightKick.png", 6, 1, 5, 3, 6);
        LightKick.SetAttackProperties(false, false);
        LightKick.SetOrigin(LightKick.width / 2, LightKick.height / 2);
        LightKick.SetCycle(0, 6, 3);
        LightKick.SetXY(0, -8);
        AddChild(LightKick);
        LightKick.visible = false;

        //Edit hard kick could become cool special and another hard kick

        HardKick = new AttackClass("Assets/Charecter 2/C2 HardKick.png", 6, 1, 6, 3, 15);
        HardKick.SetAttackProperties(false, false);
        HardKick.SetOrigin(HardKick.width / 2, HardKick.height / 2);
        HardKick.SetCycle(0, 6, 5);
        HardKick.SetXY(0, -8);
        AddChild(HardKick);
        HardKick.visible = false;

        SpecialAttack = new AttackClass("Assets/Charecter 2/C2 Special.png", 11, 1, 11, 4, 5);
        SpecialAttack.SetAttackProperties(true, true);
        SpecialAttack.SetOrigin(SpecialAttack.width / 2, SpecialAttack.height / 2);
        SpecialAttack.SetCycle(0, 11, 5);
        SpecialAttack.SetXY(100, -200);
        AddChild(SpecialAttack);
        SpecialAttack.visible = false;

        UltimateAttack = new AttackClass("Assets/Charecter 2/C2 Ultimate.png", 26, 1, 9, 26, 10);
        UltimateAttack.SetAttackProperties(true, true);
        UltimateAttack.SetOrigin(UltimateAttack.width / 2, UltimateAttack.height / 2);
        UltimateAttack.SetCycle(0, 26, 5);
        UltimateAttack.SetXY(100, -200);
        AddChild(UltimateAttack);
        UltimateAttack.visible = false;

        CrouchAttack = new AttackClass("Assets/Charecter 2/C2 CrouchAttack.png", 5, 1, 5, 2, 6);
        CrouchAttack.SetAttackProperties(false, false);
        CrouchAttack.SetOrigin(CrouchAttack.width / 2, CrouchAttack.height / 2);
        CrouchAttack.SetCycle(0, 5, 3);
        CrouchAttack.SetXY(0, -8);
        AddChild(CrouchAttack);
        CrouchAttack.visible = false;

        JumpAttack = new AttackClass("Assets/Charecter 2/C2 JumpAttack.png", 1, 1, 1, 0, 8);
        JumpAttack.SetAttackProperties(false, true);
        JumpAttack.SetOrigin(JumpAttack.width / 2, JumpAttack.height / 2);
        JumpAttack.SetCycle(0, 1, 20);
        JumpAttack.SetXY(0, -8);
        AddChild(JumpAttack);
        JumpAttack.visible = false;

        //States
        Idle = new AnimationSprite("Assets/Charecter 2/C2 Idle.png", 2, 1, 2, false, false);
        Idle.SetOrigin(Idle.width / 2, Idle.height / 2);
        Idle.SetCycle(0, 2, 16);
        Idle.SetXY(0, -8);
        AddChild(Idle);
        Idle.visible = false;

        Dead = new AnimationSprite("Assets/Charecter 2/C2 KO.png", 6, 1, 6, false, false);
        Dead.SetOrigin(Dead.width / 2, Dead.height / 2);
        Dead.SetCycle(0, 6, 8);
        Dead.SetXY(0, -8);
        AddChild(Dead);
        Dead.visible = false;

        Crouch = new AnimationSprite("Assets/Charecter 2/C2 Crouch.png", 1, 1, 2, false, false);
        Crouch.SetOrigin(Crouch.width / 2, Crouch.height / 2);
        Crouch.SetCycle(0, 1, 16);
        Crouch.SetXY(0, -8);
        AddChild(Crouch);
        Crouch.visible = false;

        Hit = new AnimationSprite("Assets/Charecter 2/C2 Hit.png", 3, 1, 3, false, false);
        Hit.SetOrigin(Hit.width / 2, Hit.height / 2);
        Hit.SetCycle(0, 3, 10);
        Hit.SetXY(0, -8);
        AddChild(Hit);
        Hit.visible = false;

        Block = new AnimationSprite("Assets/Charecter 2/C2 Block.png", 2, 1, 2, false, false);
        Block.SetOrigin(Block.width / 2, Block.height / 2);
        Block.SetCycle(0, 2, 4);
        Block.SetXY(0, -8);
        AddChild(Block);
        Block.visible = false;

        Walk = new AnimationSprite("Assets/Charecter 2/C2 Walk.png", 4, 1, 4, false, false);
        Walk.SetOrigin(Walk.width / 2, Walk.height / 2);
        Walk.SetCycle(0, 4, 6);
        Walk.SetXY(0, -8);
        AddChild(Walk);
        Walk.visible = false;

        Jump = new AnimationSprite("Assets/Charecter 2/C2 Jump.png", 1, 1, 1, false, false);
        Jump.SetOrigin(Jump.width / 2, Jump.height / 2);
        Jump.SetCycle(0, 1, 4);
        Jump.SetXY(0, -8);
        AddChild(Jump);
        Jump.visible = false;

        KnockDown = new AnimationSprite("Assets/Charecter 2/C2 KnockDown.png", 1, 1, 1, false, false);
        KnockDown.SetOrigin(KnockDown.width / 2, KnockDown.height / 2);
        KnockDown.SetCycle(0, 1, 4);
        KnockDown.SetXY(0, -8);
        AddChild(KnockDown);
        KnockDown.visible = false;

        DashForward = new AnimationSprite("Assets/Charecter 2/C2 DashForward.png", 3, 1, 3, false, false);
        DashForward.SetOrigin(DashForward.width / 2, DashForward.height / 2);
        DashForward.SetCycle(0, 3, 4);
        DashForward.SetXY(0, -8);
        AddChild(DashForward);
        DashForward.visible = false;

        DashBackwards = new AnimationSprite("Assets/Charecter 2/C2 DashBackwards.png", 3, 1, 3, false, false);
        DashBackwards.SetOrigin(DashBackwards.width / 2, DashBackwards.height / 2);
        DashBackwards.SetCycle(0, 3, 4);
        DashBackwards.SetXY(0, -8);
        AddChild(DashBackwards);
        DashBackwards.visible = false;
    }
    void LPAttack()
    {
        vx = 0;
        CurrentAttack = LightPunch;
        CurrentAttack.ResetAttack();
        SetCurrentAttack(CurrentAttack);
        SetHurBox(70, -15, 2.5f, 1f);
        hurtBox.rotation = -25;
        SetAttackDmg(30, 4);
        attacking = true;
    }
    void HPAttack()
    {
        CurrentAttack = HardPunch;
        CurrentAttack.ResetAttack();
        SetCurrentAttack(CurrentAttack);
        SetHurBox(50, 0, 3f, 6f);
        SetAttackDmg(50,8,7);

        attacking = true;
    }
    void LKAttack()
    {
        CurrentAttack = LightKick;
        CurrentAttack.ResetAttack();
        SetCurrentAttack(CurrentAttack);
        SetHurBox(30, -20, 2f, 1f);
        SetPlayerColl(-50, 0);
        hurtBox.rotation = -40;
        SetAttackDmg(40, 2);
        attacking = true;
    }
    void HKAttack()
    {
        CurrentAttack = HardKick;
        CurrentAttack.ResetAttack();
        SetCurrentAttack(CurrentAttack);
        SetHurBox(30, 0, 3.5f, 2f);
        SetAttackDmg(60, 5);
        SetPlayerColl(-30, 0,5f,2f);
        attacking = true;

    }
    void CrouchingAttack()
    {
        CrouchAttack.ResetAttack();
        SetCurrentAttack(CrouchAttack);
        SetHurBox(70, 50, 1.5f, 1f);
        SetAttackDmg(40, 3);
        CurrentAttack = CrouchAttack;
        attacking = true;
    }
    void JumpingAttack()
    {
        JumpAttack.ResetAttack();
        SetCurrentAttack(JumpAttack);
        SetHurBox(15, -10, 5f, 1.5f);
        SetAttackDmg(50, 2);
        CurrentAttack = JumpAttack;
        attacking = true;
    }
    void SPAttack()
    {
        CurrentAttack = SpecialAttack;
        CurrentAttack.ResetAttack();
        CurrentAttack.ResetHit(3);
        SetCurrentAttack(CurrentAttack);
        SetHurBox(180, -20, 5f, 5f);
        SetAttackDmg(15, 5, 6);
        attacking = true;
    }
    void UltAttack()
    {
        CurrentAttack = UltimateAttack;
        CurrentAttack.ResetAttack();
        CurrentAttack.ResetHit(4);
        SetCurrentAttack(CurrentAttack);
        SetHurBox(100, 0, 4f, 6f);
        SetAttackDmg(80, 5, 25);
        SetPlayerColl(0, 0, 0f, 0f);
        playerColl.rotation = 50;
        attacking = true;
        ResetSpBar();
        TriggerSuper(true);
    }
    protected override void BasicAttackInputs()
    {
        if (ValidInput(9) && grounded)
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
        else if (ValidInput(12))
        {
            HKAttack();
        }

    }
    protected override void SpecialAttackInputs()
    {
        if (grounded && DoubleQuarterCircleBackwards() && (ValidInput(11) || ValidInput(12)) && currentPower >= MaxPower)
            UltAttack();
        else if (grounded && DoubleDown() && (ValidInput(9) || ValidInput(10)) && (CurrentAttack!=SpecialAttack || !attacking))
            SPAttack();
    }
    protected override void AttackAddition()
    {
        if(CurrentAttack == LightPunch)
            if(CurrentAttack.currentFrame == 1)
            {
                if (scaleX > 0)
                    vx = Horizontalspeed * 2;
                else
                    vx = -Horizontalspeed * 2;
            }
        if (CurrentAttack == HardPunch)
            if (CurrentAttack.currentFrame == 1 || CurrentAttack.currentFrame == 2)
            {
                if (scaleX > 0)
                    vx = Horizontalspeed * 2;
                else
                    vx = -Horizontalspeed * 2;
            }
        if (CurrentAttack == HardKick)
        {
            if ((CurrentAttack.currentFrame >= 1 && CurrentAttack.currentFrame <= 3) && grounded)
            {
                if (scaleX > 0)
                    vx = Horizontalspeed * 2;
                else
                    vx = -Horizontalspeed * 2;
            }
        }
        if (CurrentAttack == SpecialAttack)
        {
            if (CurrentAttack.currentFrame == 1 && grounded)
            {
                if (scaleX > 0)
                    vx = Horizontalspeed * 1;
                else
                    vx = -Horizontalspeed * 1;
            }
            if (CurrentAttack.currentFrame == 3)
                CurrentAttack.ResetHit(4);
            if (CurrentAttack.currentFrame == 4)
                CurrentAttack.ResetHit(5);
            if (CurrentAttack.currentFrame == 5)
                CurrentAttack.ResetHit(6);
            if (CurrentAttack.currentFrame == 6)
                CurrentAttack.ResetHit(7);
        }
        if(CurrentAttack == UltimateAttack)
        {
            if (CurrentAttack.currentFrame == 7)
            {
                CurrentAttack.ResetHit(10);
                SetHurBox(160, -320, 3f, 6f);
                hurtBox.rotation = 50;
                SetAttackDmg(80, 6, -25);
                playerColl.rotation = 50;
            }
            if (CurrentAttack.currentFrame == 13)
            {
                CurrentAttack.ResetHit(14);
                SetHurBox(260, -40, 4f, 4f);
                SetAttackDmg(30, 10, 10);
            }
            if (CurrentAttack.currentFrame == 15)
            {
                CurrentAttack.ResetHit(15);
                SetHurBox(260, -40, 6f, 8f);
            }
        }
    }
}
