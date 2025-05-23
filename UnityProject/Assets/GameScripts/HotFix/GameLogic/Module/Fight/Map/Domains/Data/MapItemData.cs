namespace GameLogic.Fight
{
    public class MapItemData
    {
        public Constant.MapItemType MapItemType { get; set; }

        public int IndexX { get; private set; }

        public int IndexY { get; private set; }

        public MapItemData(int indexX, int indexY)
        {
            IndexX = indexX;
            IndexY = indexY;
        }
    }
}