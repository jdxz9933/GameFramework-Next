using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic {
    public static partial class Constant {
        public static class User {
            public const int NameMaxLength = 16;

            public const string LoaclPlayerImagePath = "local_1_1.webp";
            public const string LoaclPlayerHeadImagePath = "local_head1_1.webp";

            public const string SettingKey = "user_setting";
            
            

            public const string LocalImgPath = "local_";

            //匹配网络图片路径
            public const string WebImgPath = "http";

            public const int UserRenameCost = 50;

            public enum SlotType {
                /// <summary>
                /// 脸部套装
                /// </summary>
                FaceSuit = 1201,

                /// <summary>
                /// 套装
                /// </summary>
                Suit = 1202,

                /// <summary>
                /// 贴纸套装格子
                /// </summary>
                DecalsSuit = 1204,

                /// <summary>
                /// 形象
                /// </summary>
                Player = 1203,
            }

            public static readonly int OfficialGid = 1;

            public static string GetTextureName2(int type, long slotIndex) {
                return $"{Constant.User.LocalImgPath}{type}_{slotIndex}.webp";
            }
        }
    }
}