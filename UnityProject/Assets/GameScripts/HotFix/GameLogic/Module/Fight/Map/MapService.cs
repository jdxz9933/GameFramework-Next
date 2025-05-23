using System;
using System.Collections.Generic;

namespace GameLogic.Fight
{
    public class MapService
    {
        private MapRepositories _mapRepositories;


        public MapService()
        {
            _mapRepositories = new MapRepositories();
        }

        public void GenerateMap(int mapSizeX, int mapSizeY)
        {
            _mapRepositories.InitMap(mapSizeX, mapSizeY);

            // 生成地图
            for (int x = 0; x < mapSizeX; x++)
            {
                for (int y = 0; y < mapSizeY; y++)
                {
                    if (x == 0 || y == 0 || x == mapSizeX - 1 || y == mapSizeY - 1)
                    {
                        MapItemData mapItemData = new MapItemData(x, y);
                        mapItemData.MapItemType = Constant.MapItemType.Boundary;
                        _mapRepositories.SetMapItem(mapItemData);
                    }
                    else
                    {
                        var rand = UnityEngine.Random.Range(0, 100);
                        if (rand > 40)
                        {
                            MapItemData mapItemData = new MapItemData(x, y);
                            mapItemData.MapItemType = Constant.MapItemType.Floor;
                            _mapRepositories.SetMapItem(mapItemData);
                        }
                        else
                        {
                            MapItemData mapItemData = new MapItemData(x, y);
                            mapItemData.MapItemType = Constant.MapItemType.Wall;
                            _mapRepositories.SetMapItem(mapItemData);
                        }
                    }
                }
            }
        }


        private int _iterationMapCount;

        public void IterationMapOnce()
        {
            _iterationMapCount++;
            var mapDataCopy = _mapRepositories.CloneMapItems();
            for (int x = 0; x < _mapRepositories.GetMapSizeX(); x++)
            {
                for (int y = 0; y < _mapRepositories.GetMapSizeY(); y++)
                {
                    var mapItemData = _mapRepositories.GetMapItem(x, y);
                    switch (_iterationMapCount)
                    {
                        case >= 1 and <= 4:
                            if (mapItemData.MapItemType != Constant.MapItemType.Boundary)
                            {
                                if (GetAroundWallCount(x, y, mapDataCopy) >= 5 ||
                                    GetAroundWallCount(x, y, mapDataCopy, 2) <= 2)
                                {
                                    mapItemData.MapItemType = Constant.MapItemType.Wall;
                                }
                                else
                                {
                                    mapItemData.MapItemType = Constant.MapItemType.Floor;
                                }
                            }
                            break;
                        case > 4 and <= 7:
                            if (mapItemData.MapItemType != Constant.MapItemType.Boundary)
                            {
                                if (GetAroundWallCount(x, y, mapDataCopy) >= 5)
                                {
                                    mapItemData.MapItemType = Constant.MapItemType.Wall;
                                }
                                else
                                {
                                    mapItemData.MapItemType = Constant.MapItemType.Floor;
                                }
                            }
                            break;
                    }
                }
            }
        }

        public int GetAroundWallCount(int x, int y, MapItemData[,] mapItems, int lap = 1)
        {
            //检查周围元素
            //从上面,顺时针,依次检测
            int wallCount = 0;
            var listIndex = GetLapIndex(x, y, lap);
            foreach ((int x, int y) valueTuple in listIndex)
            {
                //检测当前是否超出范围
                if (valueTuple.x < 0 || valueTuple.x >= _mapRepositories.GetMapSizeX() || valueTuple.y < 0 ||
                    valueTuple.y >= _mapRepositories.GetMapSizeY())
                {
                    //wallCount++;
                }
                else
                {
                    var itemData = mapItems[valueTuple.x, valueTuple.y];
                    if (itemData.MapItemType == Constant.MapItemType.Wall ||
                        itemData.MapItemType == Constant.MapItemType.Boundary)
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
        private List<(int, int)> GetLapIndex(int xPos, int yPos, int lap)
        {
            //计算这一圈的总个数
            var lapIndex = new List<(int, int)>();

            for (int x = xPos - lap; x <= xPos + lap; x++)
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
    }
}