using System;                                   // System contains a lot of default C# libraries 
using GXPEngine;                                // GXPEngine contains the engine
using System.Drawing;
using System.Collections.Generic;
using TiledMapParser;

class TestAnimation : AnimationSprite
{
    public TestAnimation(string filename, int cols, int rows) : base(filename, cols, rows)
    {

    }

    void Update()
    {
        Animate();
    }
}

