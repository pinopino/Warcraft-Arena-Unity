using UnityEngine;

namespace Core
{
    public abstract partial class Unit
    {
        internal class MotionController : IUnitBehaviour
        {
            private const int IgnoredFramesAfterControlGained = 10;

            private Unit unit;
            private BoltEntity moveEntity; // 说明：我实在是觉得这里两个字段是为坐骑或者那种可乘骑的载具准备的
            private IMoveState moveState;

            private int currentMovementIndex;
            private int remoteControlGainFrame;

            private readonly IdleMovement idleMovement = new IdleMovement();
            private readonly MovementGenerator[] movementGenerators = new MovementGenerator[MovementUtils.MovementSlots.Length];
            private readonly bool[] startedMovement = new bool[MovementUtils.MovementSlots.Length];

            private MovementGenerator CurrentMovement => movementGenerators[currentMovementIndex];
            private bool CurrentAlreadyStarted => startedMovement[currentMovementIndex];

            internal bool UsesKinematicMovement { get; set; }
            internal bool HasMovementControl { get; private set; }

            public MovementFlags MovementFlags { get; private set; }
            public bool Jumping { get; set; }
            public bool IsMoving => MovementFlags.IsMoving();

            public bool HasClientLogic => true;
            public bool HasServerLogic => true;

            void IUnitBehaviour.DoUpdate(int deltaTime)
            {
                if (!CurrentMovement.Update(unit, deltaTime))
                    ResetCurrentMovement();
            }

            void IUnitBehaviour.HandleUnitAttach(Unit unit)
            {
                this.unit = unit;

                currentMovementIndex = 0;

                StartMovement(idleMovement, MovementSlot.Idle);

                if (!unit.IsOwner)
                {
                    Debug.Log("         " + DebugHelper.Prefix + "4.d.1. unit.Motion.HandleUnitAttach，unit.IsOwner=" + unit.IsOwner + "，因此注册回调OnUnitStateFlagsChanged，以便监听IUnitState.MovementFlags值的变化"); // 删除
                    /*
                     * 说明：
                     * 这里做如下猜测：测试下来我们知道了作为客户端的玩家会有两个实体同时存在，服务器端有一个，
                     * 自己本地还有一个；
                     * 
                     * 作为自己本地的那个，其实功能很弱的，它的内在状态改变一定都发生在服务端。因此，此种情况
                     * 下就需要要注册回调，以便服务端运算出来状态变了就需要通过回调通知给客户端。
                     * 
                     * 类似的情况没猜错的话应该会有不少存在；比如目前发现的都记录在下面：
                     * player.cs上HandleStateCallbacks
                     * 
                     */
                    unit.AddCallback(nameof(IUnitState.MovementFlags), OnUnitStateFlagsChanged);
                }
                else
                {
                    Debug.Log("         " + DebugHelper.Prefix + "4.d.1. unit.Motion.HandleUnitAttach，unit.IsOwner=" + unit.IsOwner + "，因此没有注册回调了"); // 删除
                }
            }

            void IUnitBehaviour.HandleUnitDetach()
            {
                if (!unit.IsOwner)
                    unit.RemoveCallback(nameof(IUnitState.MovementFlags), OnUnitStateFlagsChanged);

                ResetAllMovement();

                DetachMoveState(true);

                currentMovementIndex = 0;

                unit = null;
            }

            public bool HasMovementFlag(MovementFlags flag)
            {
                return (MovementFlags & flag) != 0;
            }

            public void ModifyConfusedMovement(bool isConfused)
            {
                if (isConfused)
                    StartMovement(new ConfusedMovement(), MovementSlot.Controlled);
                else
                    CancelMovement(MovementType.Confused, MovementSlot.Controlled);
            }

            public void StartChargingMovement(Vector3 chargePoint, float chargeSpeed)
            {
                StartMovement(new ChargeMovement(chargePoint, chargeSpeed), MovementSlot.Controlled);
            }

            public void StartPounceMovement(Vector3 pouncePoint, float pounceSpeed)
            {
                StartMovement(new PounceMovement(pouncePoint, pounceSpeed), MovementSlot.Controlled);
            }

            internal void OverrideMovementFlags(MovementFlags flags)
            {
                MovementFlags = flags;

                UpdateMovementState();
            }

            internal void SetMovementFlag(MovementFlags flag, bool add)
            {
                if (add)
                    MovementFlags |= flag;
                else
                    MovementFlags &= ~flag;

                UpdateMovementState();
            }

            internal void UpdateMovementControl(bool hasControl)
            {
                Debug.Log("     " + DebugHelper.Prefix + "6.a. 此时执行修改前unit.IsOwner=" + unit.IsOwner + "，unit.Motion.HasMovementControl=" + HasMovementControl); // 删除

                if (unit.IsOwner && hasControl && !HasMovementControl)
                    remoteControlGainFrame = BoltNetwork.ServerFrame;

                HasMovementControl = hasControl;

                Debug.Log("     " + DebugHelper.Prefix + "6.b. 紧接着修改后unit.IsOwner=" + unit.IsOwner + "，unit.Motion.HasMovementControl=" + HasMovementControl); // 删除
            }

            internal void AttachMoveState(BoltEntity moveEntity)
            {
                this.moveEntity = moveEntity;

                moveState = moveEntity.GetState<IMoveState>();
                // 说明：（doc103）
                // state.SetTransforms(state.CubeTransform, transform);
                // Here we tell Bolt to use the transform of the current game object 
                // where the CubeBehaviour script is attached to and replicate it over the network.
                // 有点像在给bolt侧的entity的tf属性绑定本地tf属性的味道，绑定！
                moveState.SetTransforms(moveState.LocalTransform, moveEntity.transform);

                if (unit.IsOwner)
                    moveState.AddCallback(nameof(IUnitState.MovementFlags), OnMoveStateFlagsChanged);
            }

            internal void DetachMoveState(bool destroyEntity)
            {
                if (moveEntity == null)
                    return;

                if (unit.IsOwner)
                    moveState.RemoveCallback(nameof(IUnitState.MovementFlags), OnMoveStateFlagsChanged);

                if (destroyEntity)
                {
                    if (!moveEntity.IsOwner || !moveEntity.IsAttached)
                        Destroy(moveEntity.gameObject);
                    else
                        BoltNetwork.Destroy(moveEntity.gameObject);
                }

                moveEntity = null;
                moveState = null;
            }

            internal void SimulateOwner()
            {
                if (unit.Motion.HasMovementControl && moveEntity != null)
                {
                    bool shouldIgnore = BoltNetwork.ServerFrame < remoteControlGainFrame + IgnoredFramesAfterControlGained;
                    if (!shouldIgnore)
                    {
                        unit.Position = moveEntity.transform.position;
                        unit.Rotation = moveEntity.transform.rotation;
                    }
                }

                if (unit.Motion.IsMoving)
                    unit.IsVisibilityChanged = true;
            }

            internal void SimulateController()
            {
                if (!unit.IsOwner && moveEntity != null)
                {
                    moveEntity.transform.position = unit.Position;
                    moveEntity.transform.rotation = unit.Rotation;
                }
            }

            private void StartMovement(MovementGenerator movement, MovementSlot newMovementSlot)
            {
                int newMovementIndex = (int)newMovementSlot;

                FinishMovement(newMovementIndex);

                if (currentMovementIndex < newMovementIndex)
                    currentMovementIndex = newMovementIndex;

                movementGenerators[newMovementIndex] = movement;

                if (currentMovementIndex > newMovementIndex)
                    startedMovement[newMovementIndex] = false;
                else
                    BeginMovement(newMovementIndex);
            }

            private void CancelMovement(MovementType movementType, MovementSlot cancelledMovementSlot)
            {
                int cancelledIndex = (int)cancelledMovementSlot;
                if (movementGenerators[cancelledIndex] == null)
                    return;

                if (movementGenerators[cancelledIndex].Type != movementType)
                    return;

                if (currentMovementIndex == cancelledIndex)
                    ResetCurrentMovement();
                else
                    FinishMovement(cancelledIndex);
            }

            private void ResetCurrentMovement()
            {
                while (currentMovementIndex > 0)
                {
                    FinishMovement(currentMovementIndex);

                    currentMovementIndex--;

                    if (CurrentMovement != null)
                    {
                        if (!CurrentAlreadyStarted)
                            BeginMovement(currentMovementIndex);

                        break;
                    }
                }
            }

            private void ResetAllMovement()
            {
                while (currentMovementIndex > 0)
                {
                    FinishMovement(currentMovementIndex);

                    currentMovementIndex--;
                }
            }

            private void BeginMovement(int index)
            {
                SwitchGenerator(index, true);
            }

            private void FinishMovement(int index)
            {
                if (movementGenerators[index] == null || index == 0)
                    return;

                if (startedMovement[index])
                    SwitchGenerator(index, false);

                movementGenerators[index] = null;
            }

            private void SwitchGenerator(int index, bool active)
            {
                if (active)
                    movementGenerators[index].Begin(unit);
                else
                    movementGenerators[index].Finish(unit);

                startedMovement[index] = active;
                unit.CharacterController.UpdateRigidbody();
            }

            private void SetFlags(MovementFlags flags)
            {
                MovementFlags = flags;

                UpdateMovementState();
            }

            private void UpdateMovementState()
            {
                if (unit.World.HasServerLogic)
                    unit.entityState.MovementFlags = (int)MovementFlags;
                else if (moveEntity != null && HasMovementControl)
                    moveState.MovementFlags = (int)MovementFlags;
            }

            private void OnUnitStateFlagsChanged()
            {
                if (unit.IsController && HasMovementControl)
                    return;

                SetFlags((MovementFlags)unit.entityState.MovementFlags);
            }

            private void OnMoveStateFlagsChanged()
            {
                if (HasMovementControl)
                    SetFlags((MovementFlags)moveState.MovementFlags);
            }
        }
    }
}