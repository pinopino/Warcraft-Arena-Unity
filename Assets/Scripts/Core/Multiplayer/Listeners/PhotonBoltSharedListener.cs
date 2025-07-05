using UnityEngine;

namespace Core
{
    public class PhotonBoltSharedListener : PhotonBoltBaseListener
    {
        [SerializeField] PhotonBoltReference photon;

        public override void SceneLoadLocalDone(string map)
        {
            base.SceneLoadLocalDone(map);

            if (BoltNetwork.IsConnected && BoltNetwork.IsClient)
                World.MapManager.InitializeLoadedMap(1);
        }
    }
}
