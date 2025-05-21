using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tile : MonoBehaviour
{
    private SpriteRenderer _render;

    private void Awake()
    {
        _render = GetComponent<SpriteRenderer>();
    }

    public void Setup(Vector2 pos, bool isWall,List<(int,int)> list)
    {
        transform.position = pos;
        _render.color = isWall ? Color.black : Color.clear;
    }
}
