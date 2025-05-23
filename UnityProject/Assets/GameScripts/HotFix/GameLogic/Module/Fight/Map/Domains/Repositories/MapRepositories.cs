using System;

namespace GameLogic.Fight
{
    public class MapRepositories
    {
        private MapItemData[,] _mapItems;

        public void InitMap(int mapSizeX, int mapSizeY)
        {
            _mapItems = new MapItemData[mapSizeX, mapSizeY];
        }

        public int GetMapSizeX()
        {
            return _mapItems.GetLength(0);
        }

        public int GetMapSizeY()
        {
            return _mapItems.GetLength(1);
        }

        public MapItemData GetMapItem(int x, int y)
        {
            if (x < 0 || x >= GetMapSizeX() || y < 0 || y >= GetMapSizeY())
            {
                throw new ArgumentOutOfRangeException("坐标超出地图范围");
            }
            return _mapItems[x, y];
        }

        public void SetMapItem(MapItemData mapItemData)
        {
            if (mapItemData.IndexX < 0 || mapItemData.IndexX >= GetMapSizeX() || mapItemData.IndexY < 0 ||
                mapItemData.IndexY >= GetMapSizeY())
            {
                throw new ArgumentOutOfRangeException("坐标超出地图范围");
            }

            _mapItems[mapItemData.IndexX, mapItemData.IndexY] = mapItemData;
        }

        public void CreateMapItem(int x, int y, MapItemData mapItemData)
        {
            if (x < 0 || x >= GetMapSizeX() || y < 0 || y >= GetMapSizeY())
            {
                throw new ArgumentOutOfRangeException("坐标超出地图范围");
            }

            _mapItems[x, y] = mapItemData;
        }

        public MapItemData[,] CloneMapItems()
        {
            return (MapItemData[,])_mapItems.Clone();
        }
    }
}