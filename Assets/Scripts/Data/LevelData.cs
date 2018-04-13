using EnumProperty;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoundStep : ICloneable
{
    public StepType stepType = StepType.None;
    public int[] CardList;
    public object Clone()
    {
        RoundStep outdata = new RoundStep
        {
            stepType = stepType,
            CardList = (int[])CardList.Clone(),
        };
        return outdata;
    }
}

[Serializable]
public class RoundData : ICloneable
{
    public RoundStep[] steps;
    public object Clone()
    {
        RoundData outdata = new RoundData
        {
            steps = (RoundStep[])steps.Clone(),
        };
        return outdata;
    }
}
[Serializable]
public class LevelData : ICloneable
{
    public RoundData[] rounds;
    public int goal = 10;

    public object Clone()
    {
        LevelData outdata = new LevelData
        {
            rounds = (RoundData[])rounds.Clone(),
        };
        return outdata;
    }
}
