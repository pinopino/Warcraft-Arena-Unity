using UnityEngine;
using UnityEngine.Animations;

namespace Client
{
    public class AnimatorResetTriggerBehaviour : StateMachineBehaviour
    {
        [SerializeField] private string parameterName;
        [SerializeField] private bool resetOnEnter;
        [SerializeField] private bool resetOnExit;

        private int parameterHash;
        private void OnEnable()
        {
            parameterHash = Animator.StringToHash(parameterName);
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            if (resetOnEnter)
                animator.ResetTrigger(parameterHash);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            if (resetOnExit)
                animator.ResetTrigger(parameterHash);
        }
    }
}
