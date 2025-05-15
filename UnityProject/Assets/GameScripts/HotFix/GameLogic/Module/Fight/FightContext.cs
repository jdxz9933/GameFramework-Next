using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Loxodon.Framework.Contexts;
using UGFExtensions.Await;
using UnityEngine;

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
        }
    }
}