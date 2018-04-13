
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnumProperty;

/// <summary>
/// 系统的属性
/// </summary>
public class SystemDataProperty : ScriptableObject
{
    public RoundProperty[] AllRounds;
    public LevelData[] AllLevels;
    public Card[] GameAllCards;
}
