using EnumProperty;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Card : ICloneable
{
    public GameCardType cardType = GameCardType.None;
    public int id;
    public int[] Values;
    public object Clone()
    {
        Card outdata = new Card
        {
            cardType = cardType,
            id = id,
            Values = (int[])Values.Clone()
        };
        return outdata;
    }
}
