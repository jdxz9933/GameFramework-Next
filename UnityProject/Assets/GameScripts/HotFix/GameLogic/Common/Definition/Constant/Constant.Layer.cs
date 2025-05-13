//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine;

namespace GameLogic {

    public static partial class Constant {
        /// <summary>
        /// 层。
        /// </summary>
        public static class Layer {
            public const string DefaultLayerName = "Default";
            public static readonly int DefaultLayerId = LayerMask.NameToLayer(DefaultLayerName);

            public const string UILayerName = "UI";
            public static readonly int UILayerId = LayerMask.NameToLayer(UILayerName);
            
            public const string DressPlayerLayerName = "DressPlayer";
            public static readonly int DressPlayerLayerId = LayerMask.NameToLayer(DressPlayerLayerName);
            
            public const string DecalsLayerName = "Decals";
            public static readonly int DecalsLayerId = LayerMask.NameToLayer(DecalsLayerName);
            
            public const string DecalsUILayerName = "DecalsUI";
            public static readonly int DecalsUILayerId = LayerMask.NameToLayer(DecalsUILayerName);
            
            public const string ParticleUIILayerName = "ParticleUI";
            public static readonly int ParticleUILayerId = LayerMask.NameToLayer(ParticleUIILayerName);
        }
    }

}