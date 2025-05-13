using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic {
    public enum SaveKey {
        Base, //基础数据
        Setting, //设置
        GameBagData, //背包数据
        UserData, //玩家数据
        EventTask, //游戏活动数据
        StorySave, //剧情数据
        DressUpData, //换装数据
        DIYPartData, //DIY部件数据
        DIYSceneData, //DIY贴花数据
        ShopData, //商店数据
        GuideData, //新手引导数据
        FunctionalityData, //解锁数据数据

        GameLevelData, //关卡数据
    }

    public abstract class SaveDataBase {
        public abstract string GetSavePath();

        public abstract string GetSaveKey();
    }
}