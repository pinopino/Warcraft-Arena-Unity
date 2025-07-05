using UnityEngine;
using UnityEngine.Animations;

namespace Client
{
    public class AnimatorSetIntBehaviour : StateMachineBehaviour
    {
        [SerializeField] private string parameterName;
        [SerializeField] private int valueOnEnter;
        [SerializeField] private int valueOnExit;
        [SerializeField] private bool setOnEnter = true;
        [SerializeField] private bool setOnExit = true;

        private int parameterHash;

        private void OnEnable()
        {
            parameterHash = Animator.StringToHash(parameterName);
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            if (setOnEnter)
                animator.SetInteger(parameterHash, valueOnEnter);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            if (setOnExit)
                if (animator.GetCurrentAnimatorStateInfo(layerIndex).shortNameHash != stateInfo.shortNameHash)
                    animator.SetInteger(parameterHash, valueOnExit);
        }
    }
}
