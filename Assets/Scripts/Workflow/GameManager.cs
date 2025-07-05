using Common;
using Core;
using System.Diagnostics;
using UnityEngine;

using EventHandler = Common.EventHandler;

namespace Game
{
    public sealed class GameManager : MonoBehaviour
    {
        private enum UpdatePolicy
        {
            EveryUpdateCall,
            FixedTimeDelta
        }

        [SerializeField] private UpdatePolicy updatePolicy;
        [SerializeField] private long updateTimeMilliseconds = 20;
        [SerializeField] private ScriptableContainer scriptableCoreContainer;
        [SerializeField] private ScriptableContainer scriptableClientContainer;

        private readonly Stopwatch gameTimer = new Stopwatch();
        private long lastWorldUpdateTime;
        private long lastGameUpdateTime;
        private World world;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            scriptableCoreContainer.Register();
            scriptableClientContainer.Register();

            gameTimer.Start();
        }

        private void Update()
        {
            long elapsedTime = gameTimer.ElapsedMilliseconds;
            int worldTimeDiff = (int)(elapsedTime - lastWorldUpdateTime);
            int gameTimeDiff = (int)(elapsedTime - lastGameUpdateTime);
            float gameTimeFloatDiff = gameTimeDiff / 1000.0f;

            lastGameUpdateTime = elapsedTime;

            if (world == null)
                lastWorldUpdateTime = elapsedTime;
            else switch (updatePolicy)
                {
                    case UpdatePolicy.EveryUpdateCall:
                        lastWorldUpdateTime = elapsedTime;
                        world.DoUpdate(worldTimeDiff);
                        break;
                    case UpdatePolicy.FixedTimeDelta:
                        if (worldTimeDiff >= updateTimeMilliseconds)
                            goto case UpdatePolicy.EveryUpdateCall;
                        break;
                    default:
                        goto case UpdatePolicy.EveryUpdateCall;
                }

            scriptableCoreContainer.DoUpdate(gameTimeDiff);

            if (world != null && world.HasClientLogic)
                scriptableClientContainer.DoUpdate(gameTimeFloatDiff);
        }

        private void OnApplicationQuit()
        {
            DestroyWorld();

            scriptableClientContainer.Unregister();
            scriptableCoreContainer.Unregister();
        }

        public void CreateWorld(World newWorld)
        {
            world = newWorld;

            EventHandler.ExecuteEvent(GameEvents.WorldStateChanged, world, true);
        }

        public void DestroyWorld()
        {
            if (world == null)
                return;

            EventHandler.ExecuteEvent(GameEvents.WorldStateChanged, world, false);

            world.Dispose();
            world = null;
        }
    }
}