using System;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    public int seed = 1846128673;
    public int mapSizeX = 100;
    public int mapSizeY = 100;
    
    private Transform _maoGroup;
    private GameObject _goMapTile;

    private GameDef.MapTileType[,] _mapData;
    private Tile[,] _mapTiles;
    
    private int _iterationMapCount;

    private void Start()
    {
        _maoGroup = new GameObject().transform;
        _maoGroup.transform.position = Vector3.zero;

        _goMapTile = new GameObject();
        
        var spriteRenderer = _goMapTile.AddComponent<SpriteRenderer>();
        
        // 创建一个白色的方块纹理
        Texture2D whiteTexture = new Texture2D(100, 100);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
        
        // 创建一个精灵并应用到SpriteRenderer上
        Sprite whiteSprite = Sprite.Create(whiteTexture, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f));
        spriteRenderer.sprite = whiteSprite;
        
        _goMapTile.AddComponent<Tile>();
    }

    /// <summary>
    /// 生成地图
    /// </summary>
    public void GenerateMap()
    {
        UnityEngine.Random.InitState(seed);
        _mapData = new GameDef.MapTileType[mapSizeX,mapSizeY];
        _maoGroup.RemoveAllChild();
        _mapTiles = new Tile[mapSizeX,mapSizeY];

        for (int i = 0; i < mapSizeX; i++)
        {
            for (int j = 0; j < mapSizeY; j++)
            {
                //边界为墙
                if (i == 0 || j == 0 || i == mapSizeX - 1 || j == mapSizeY - 1)
                {
                    _mapData[i, j] = GameDef.MapTileType.Boundary;
                }
                else
                {
                    //随机0-100一个值
                    var rand = UnityEngine.Random.Range(0, 100);
                    if (rand > 40)
                    {
                        _mapData[i, j] = GameDef.MapTileType.Floor;
                    }
                    else
                    {
                        _mapData[i, j] = GameDef.MapTileType.Wall;
                    }
                }
                var tile = Instantiate(_goMapTile, _maoGroup).GetComponent<Tile>();
                _mapTiles[i, j] = tile;
            }
        }

        RedrawMap();

        _iterationMapCount = 0;
    }

    /// <summary>
    /// 迭代地图(旧)
    /// </summary>
    public void IterationMap()
    {
        for (int i = 0; i < mapSizeX; i++)
        {
            for (int j = 0; j < mapSizeY; j++)
            {
                var mapItemData = _mapData[i, j];

                switch (mapItemData)
                {
                    case GameDef.MapTileType.Boundary:
                        break;
                    case GameDef.MapTileType.Wall:
                        if (GetAroundWallCount(i,j) >= 4)
                        {
                            _mapData[i, j] = GameDef.MapTileType.Wall;
                        }
                        else
                        {
                            _mapData[i, j] = GameDef.MapTileType.Floor;
                        }
                        break;
                    case GameDef.MapTileType.Floor:
                        if (GetAroundWallCount(i,j) >= 5)
                        {
                            _mapData[i, j] = GameDef.MapTileType.Wall;
                        }
                        else
                        {
                            _mapData[i, j] = GameDef.MapTileType.Floor;
                        }
                        break;
                }
            }
        }

        RedrawMap();
    }

    /// <summary>
    /// 迭代地图(新)
    /// </summary>
    private void IterationMapNew()
    {
        IterationMapOnce();

        RedrawMap();
    }

    private void IterationMapOnce()
    {
        _iterationMapCount++;
        //拷贝一份当前地图情况
        var mapDataCopy = (GameDef.MapTileType[,]) _mapData.Clone();
        for (int i = 0; i < mapSizeX; i++)
        {
            for (int j = 0; j < mapSizeY; j++)
            {
                var mapItemData = mapDataCopy[i, j];
                switch (_iterationMapCount)
                {
                    case >=1 and <= 4:
                        if (mapItemData != GameDef.MapTileType.Boundary)
                        {
                            if (GetAroundWallCount(i,j) >= 5 || GetAroundWallCount(i,j,2) <= 2)
                            {
                                mapDataCopy[i, j] = GameDef.MapTileType.Wall;
                            }
                            else
                            {
                                mapDataCopy[i, j] = GameDef.MapTileType.Floor;
                            }
                        }
                        break;
                    case >4 and <=7:
                        if (mapItemData != GameDef.MapTileType.Boundary)
                        {
                            if (GetAroundWallCount(i, j) >= 5)
                            {
                                mapDataCopy[i, j] = GameDef.MapTileType.Wall;
                            }
                            else
                            {
                                mapDataCopy[i, j] = GameDef.MapTileType.Floor;
                            }
                        }
                        break;
                }
            }
        }
        _mapData = (GameDef.MapTileType[,]) mapDataCopy.Clone();
    }

    private void RedrawMap()
    {
        for (int i = 0; i < mapSizeX; i++)
        {
            for (int j = 0; j < mapSizeY; j++)
            {
                //根据id确认位置
                _mapTiles[i, j].Setup(new Vector2(i,j),_mapData[i,j] == GameDef.MapTileType.Wall || _mapData[i,j] == GameDef.MapTileType.Boundary,GetLapIndex(i,j,1));
            }
        }
    }

    private int GetAroundWallCount(int x,int y,int lap = 1)
    {
        //检查周围元素
        //从上面,顺时针,依次检测
        int wallCount = 0;

        var listIndex = GetLapIndex(x, y, lap);

        foreach ((int x, int y) valueTuple in listIndex)
        {
            //检测当前是否超出范围
            if (valueTuple.x < 0 || valueTuple.x >= mapSizeX || valueTuple.y < 0 || valueTuple.y >= mapSizeY)
            {
                //wallCount++;
            }
            else
            {
                if (_mapData[valueTuple.x, valueTuple.y] == GameDef.MapTileType.Wall || _mapData[valueTuple.x, valueTuple.y] == GameDef.MapTileType.Boundary)
                {
                    wallCount++;
                }
            }
        }
        
        return wallCount;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="yPos"></param>
    /// <param name="lap"></param>
    /// <param name="xPos"></param>
    /// <returns>返回当前位置对应圈数的index(不剔除的)</returns>
    private List<(int,int)> GetLapIndex(int xPos,int yPos, int lap)
    {
        //计算这一圈的总个数
        var lapIndex = new List<(int, int)>();

        for (int x = xPos-lap; x <= xPos+lap; x++)
        {
            for (int y = yPos - lap; y <= yPos + lap; y++)
            {
                //检测当前位置是否超出圆的半径
                //计算当前点到圆心的距离
                if (Math.Pow(x - xPos, 2) + Math.Pow(y - yPos, 2) - Math.Pow(lap, 2) <= 1)
                {
                    lapIndex.Add((x, y));
                }
            }
        }

        return lapIndex;
    }
    
    void OnGUI()
    {
        // 生成地图
        if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Map"))
        {
            GenerateMap();
        }
        
        //迭代地图
        if (GUI.Button(new Rect(10, 50, 200, 30), "Iterate Map"))
        {
            IterationMapNew();  //可以替换这个方式试一下另一种地图演算 IterationMap
        }
    }
}

public static class GameHelpTools
{
    public static void RemoveAllChild(this Transform parent)
    {
        var childCount = parent.childCount;
        for (int i = childCount - 1; i >= 0 ; i--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
        parent.DetachChildren();
    }
}

public static class GameDef
{
    public enum MapTileType
    {
        Boundary = 0,
        Wall = 1,
        Floor = 2,
    }
}
