using UnityEngine;
using UnityEngine.Animations;

namespace Client
{
    public class AnimatorSetRandomIntBehaviour : StateMachineBehaviour
    {
        [SerializeField] private string parameterName;
        [SerializeField] private int minInclusive;
        [SerializeField] private int maxExclusive;

        private int parameterHash;

        private void OnEnable()
        {
            parameterHash = Animator.StringToHash(parameterName);
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            animator.SetInteger(parameterHash, Random.Range(minInclusive, maxExclusive));
        }
    }
}