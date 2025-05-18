using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.HotUpdate;
using Loxodon.Framework.Contexts;
using UGFExtensions.Await;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace GameLogic
{
    public class FightContext : Context
    {
        public FightContext() : base()
        {
        }

        public async UniTask StartFight()
        {
            await GameModule.Scene.LoadSceneAsync("fight");
            if (DynamicUtils.Dynamics.TryGetValue("fight", out var fight))
            {
                Log.Debug("fight is exist");
            }
        }
    }
}