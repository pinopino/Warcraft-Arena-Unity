using UnityEngine;

namespace Client
{
    public class EffectBehaviourFaceCamera : EffectBehaviour
    {
        [SerializeField] private CameraReference cameraReference;
        [SerializeField] private Transform transformToRotate;
        [SerializeField] private Vector3 rotationOffset;

        protected override void OnUpdate(IEffectEntity effectEntity, ref bool keepAlive)
        {
            base.OnUpdate(effectEntity, ref keepAlive);

            if (cameraReference.WarcraftCamera != null)
            {
                Quaternion lookDirectionOffset = Quaternion.Euler(rotationOffset);
                Vector3 projectedCameraDirection = Vector3.ProjectOnPlane(cameraReference.WarcraftCamera.transform.forward, Vector3.up);
                transformToRotate.rotation = Quaternion.LookRotation(projectedCameraDirection) * lookDirectionOffset;
            }
        }
    }
}