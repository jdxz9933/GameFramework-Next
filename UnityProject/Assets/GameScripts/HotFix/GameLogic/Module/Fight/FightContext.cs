using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.HotUpdate;
using Loxodon.Framework.Contexts;
using UGFExtensions.Await;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityGameFramework.Runtime;

namespace GameLogic.Fight
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

                if (fight.ObjMap.TryGetValue("map", out var map))
                {
                    var grid = map.GetComponent<Grid>();
                    var tileMap = map.Find("Tilemap").GetComponent<Tilemap>();

                    var tile = GameModule.Resource.LoadAsset<IsometricRuleTile>("NeighbourTile_Desert_FlatSand");


                    int totalX = 200;
                    int totalY = 200;
                    // var noiseMap = new float[totalX, totalY];
                    var noiseMap = Noise.GenerateNoiseMap(totalX, totalY, 10f, 2, 0.5f, 3);


                    for (int x = 0; x < totalX; x++)
                    {
                        for (int y = 0; y < totalY; y++)
                        {
                            if (noiseMap[x, y] > 0.4f)
                            {
                                tileMap.SetTile(new Vector3Int(x, y, 0), tile);
                            }
                        }
                    }

                    // tileMap.SetTile(Vector3Int.zero, tile);
                    // tileMap.SetTile(new Vector3Int(0, 1, 0), tile);
                    // tileMap.SetTile(new Vector3Int(0, 2, 0), tile);
                    // tileMap.SetTile(new Vector3Int(0, 3, 0), tile);
                    // tileMap.SetTile(new Vector3Int(0, 4, 0), tile);
                    // tileMap.SetTile(new Vector3Int(0, 5, 0), tile);
                    // tileMap.SetTile(new Vector3Int(0, 6, 0), tile);
                    // tileMap.SetTile(new Vector3Int(0, 7, 0), tile);
                    // tileMap.FloodFill(Vector3Int.one, tile);
                }
            }
        }
    }
}