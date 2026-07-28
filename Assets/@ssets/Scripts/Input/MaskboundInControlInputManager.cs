using InControl;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.Input
{
    [AddComponentMenu("Maskbound/Input/InControl Input Manager")]
    public class MaskboundInControlInputManager : MoreMountains.CorgiEngine.InputManager
    {
        [Header("InControl")]
        [SerializeField] private MaskboundInputBindings inputBindings;
        [SerializeField] private bool logInputDebug;

        public MMInput.IMButton BlockButton { get; private set; }

        private MaskboundCorgiActions _actions;
        private bool _initialized;

        protected override void Start()
        {
            if (!_initialized)
            {
                Initialization();
            }
        }

        protected override void Initialization()
        {
            base.Initialization();

            if (_actions == null)
            {
                _actions = MaskboundCorgiActions.CreateWithDefaultBindings(inputBindings);
            }

            _initialized = true;
        }

        protected virtual void OnEnable()
        {
            if (_actions != null)
            {
                _actions.Enabled = true;
            }
        }

        protected virtual void OnDisable()
        {
            if (_actions != null)
            {
                _actions.Enabled = false;
            }
        }

        protected virtual void OnDestroy()
        {
            if (_actions != null)
            {
                _actions.Destroy();
                _actions = null;
            }
        }

        protected override void Update()
        {
            if (!InputDetectionActive || IsMobile || _actions == null)
            {
                _primaryMovement = Vector2.zero;
                _secondaryMovement = Vector2.zero;
                ShootAxis = MMInput.ButtonStates.Off;
                SecondaryShootAxis = MMInput.ButtonStates.Off;
                return;
            }

            SetMovement();
            SetSecondaryMovement();
            SetShootAxis();
            GetInputButtons();
        }

        public override void SetMovement()
        {
            if (!InputDetectionActive || _actions == null)
            {
                return;
            }

            _primaryMovement = ApplyDeadZone(_actions.Move.Value);
        }

        public override void SetSecondaryMovement()
        {
            if (!InputDetectionActive || _actions == null)
            {
                return;
            }

            _secondaryMovement = ApplyDeadZone(_actions.Aim.Value);
        }

        protected override void GetInputButtons()
        {
            BindButton(_actions.Jump, JumpButton);
            BindButton(_actions.Run, RunButton);
            BindButton(_actions.Dash, DashButton);
            BindButton(_actions.Roll, RollButton);
            BindButton(_actions.Attack, ShootButton);
            BindButton(_actions.SpecialAttack, SecondaryShootButton);
            BindButton(_actions.Interact, InteractButton);
            BindButton(_actions.Reload, ReloadButton);
            BindButton(_actions.Pause, PauseButton);
            BindButton(_actions.SwitchWeapon, SwitchWeaponButton);
            BindButton(_actions.SwitchCharacter, SwitchCharacterButton);
            BindButton(_actions.TimeControl, TimeControlButton);
            BindButton(_actions.Swim, SwimButton);
            BindButton(_actions.Glide, GlideButton);
            BindButton(_actions.Jetpack, JetpackButton);
            BindButton(_actions.Fly, FlyButton);
            BindButton(_actions.Grab, GrabButton);
            BindButton(_actions.Throw, ThrowButton);
            BindButton(_actions.Push, PushButton);
            BindButton(_actions.Grip, GripButton);
            BindButton(_actions.Block, BlockButton);
        }

        protected override void InitializeButtons()
        {
            base.InitializeButtons();
            ButtonList.Add(BlockButton = new MMInput.IMButton(
                PlayerID,
                "Block",
                BlockButtonDown,
                BlockButtonPressed,
                BlockButtonUp));
        }

        public virtual void BlockButtonDown()
        {
            BlockButton.State.ChangeState(MMInput.ButtonStates.ButtonDown);
        }

        public virtual void BlockButtonPressed()
        {
            BlockButton.State.ChangeState(MMInput.ButtonStates.ButtonPressed);
        }

        public virtual void BlockButtonUp()
        {
            BlockButton.State.ChangeState(MMInput.ButtonStates.ButtonUp);
        }

        protected override void SetShootAxis()
        {
            ShootAxis = GetAxisButtonState(_actions.Attack);
            SecondaryShootAxis = GetAxisButtonState(_actions.SpecialAttack);
        }

        private void BindButton(PlayerAction action, MMInput.IMButton button)
        {
            if (action.WasPressed)
            {
                button.TriggerButtonDown();
                LogButton(action.Name, "Down");
                return;
            }

            if (action.WasReleased)
            {
                button.TriggerButtonUp();
                LogButton(action.Name, "Up");
                return;
            }

            if (action.IsPressed)
            {
                if (button.State.CurrentState == MMInput.ButtonStates.Off)
                {
                    button.TriggerButtonDown();
                    LogButton(action.Name, "Down");
                    return;
                }

                button.TriggerButtonPressed();
                return;
            }

            if (button.State.CurrentState == MMInput.ButtonStates.ButtonPressed)
            {
                button.TriggerButtonUp();
                LogButton(action.Name, "Up");
            }
        }

        private MMInput.ButtonStates GetAxisButtonState(PlayerAction action)
        {
            if (action.WasPressed)
            {
                return MMInput.ButtonStates.ButtonDown;
            }

            if (action.WasReleased)
            {
                return MMInput.ButtonStates.ButtonUp;
            }

            return action.IsPressed ? MMInput.ButtonStates.ButtonPressed : MMInput.ButtonStates.Off;
        }

        private Vector2 ApplyDeadZone(Vector2 value)
        {
            if (Mathf.Abs(value.x) < Threshold.x)
            {
                value.x = 0f;
            }

            if (Mathf.Abs(value.y) < Threshold.y)
            {
                value.y = 0f;
            }

            return Vector2.ClampMagnitude(value, 1f);
        }

        private void LogButton(string actionName, string state)
        {
            if (!logInputDebug)
            {
                return;
            }

            Debug.Log($"InControl Corgi input {actionName}: {state}", this);
        }

        private class MaskboundCorgiActions : PlayerActionSet
        {
            public readonly PlayerAction MoveLeft;
            public readonly PlayerAction MoveRight;
            public readonly PlayerAction MoveUp;
            public readonly PlayerAction MoveDown;
            public readonly PlayerTwoAxisAction Move;

            public readonly PlayerAction AimLeft;
            public readonly PlayerAction AimRight;
            public readonly PlayerAction AimUp;
            public readonly PlayerAction AimDown;
            public readonly PlayerTwoAxisAction Aim;

            public readonly PlayerAction Jump;
            public readonly PlayerAction Run;
            public readonly PlayerAction Dash;
            public readonly PlayerAction Roll;
            public readonly PlayerAction Attack;
            public readonly PlayerAction SpecialAttack;
            public readonly PlayerAction Interact;
            public readonly PlayerAction Reload;
            public readonly PlayerAction Pause;
            public readonly PlayerAction SwitchWeapon;
            public readonly PlayerAction SwitchCharacter;
            public readonly PlayerAction TimeControl;
            public readonly PlayerAction Swim;
            public readonly PlayerAction Glide;
            public readonly PlayerAction Jetpack;
            public readonly PlayerAction Fly;
            public readonly PlayerAction Grab;
            public readonly PlayerAction Throw;
            public readonly PlayerAction Push;
            public readonly PlayerAction Grip;
            public readonly PlayerAction Block;

            private MaskboundCorgiActions()
            {
                MoveLeft = CreatePlayerAction("Move Left");
                MoveRight = CreatePlayerAction("Move Right");
                MoveUp = CreatePlayerAction("Move Up");
                MoveDown = CreatePlayerAction("Move Down");
                Move = CreateTwoAxisPlayerAction(MoveLeft, MoveRight, MoveDown, MoveUp);

                AimLeft = CreatePlayerAction("Aim Left");
                AimRight = CreatePlayerAction("Aim Right");
                AimUp = CreatePlayerAction("Aim Up");
                AimDown = CreatePlayerAction("Aim Down");
                Aim = CreateTwoAxisPlayerAction(AimLeft, AimRight, AimDown, AimUp);

                Jump = CreatePlayerAction("Jump");
                Run = CreatePlayerAction("Run");
                Dash = CreatePlayerAction("Dash");
                Roll = CreatePlayerAction("Roll");
                Attack = CreatePlayerAction("Attack");
                SpecialAttack = CreatePlayerAction("Special Attack");
                Interact = CreatePlayerAction("Interact");
                Reload = CreatePlayerAction("Reload");
                Pause = CreatePlayerAction("Pause");
                SwitchWeapon = CreatePlayerAction("Switch Weapon");
                SwitchCharacter = CreatePlayerAction("Switch Character");
                TimeControl = CreatePlayerAction("Time Control");
                Swim = CreatePlayerAction("Swim");
                Glide = CreatePlayerAction("Glide");
                Jetpack = CreatePlayerAction("Jetpack");
                Fly = CreatePlayerAction("Fly");
                Grab = CreatePlayerAction("Grab");
                Throw = CreatePlayerAction("Throw");
                Push = CreatePlayerAction("Push");
                Grip = CreatePlayerAction("Grip");
                Block = CreatePlayerAction("Block");
            }

            public static MaskboundCorgiActions CreateWithDefaultBindings(MaskboundInputBindings inputBindings)
            {
                var actions = new MaskboundCorgiActions();

                if (inputBindings == null)
                {
                    inputBindings = ScriptableObject.CreateInstance<MaskboundInputBindings>();
                }

                inputBindings.Horizontal.Negative.ApplyTo(actions.MoveLeft);
                inputBindings.Horizontal.Positive.ApplyTo(actions.MoveRight);
                inputBindings.Vertical.Positive.ApplyTo(actions.MoveUp);
                inputBindings.Vertical.Negative.ApplyTo(actions.MoveDown);

                inputBindings.AimHorizontal.Negative.ApplyTo(actions.AimLeft);
                inputBindings.AimHorizontal.Positive.ApplyTo(actions.AimRight);
                inputBindings.AimVertical.Positive.ApplyTo(actions.AimUp);
                inputBindings.AimVertical.Negative.ApplyTo(actions.AimDown);

                inputBindings.Jump.ApplyTo(actions.Jump);
                inputBindings.Run.ApplyTo(actions.Run);
                inputBindings.Dash.ApplyTo(actions.Dash);
                inputBindings.Roll.ApplyTo(actions.Roll);
                inputBindings.Attack.ApplyTo(actions.Attack);
                inputBindings.SpecialAttack.ApplyTo(actions.SpecialAttack);
                inputBindings.Interact.ApplyTo(actions.Interact);
                inputBindings.Reload.ApplyTo(actions.Reload);
                inputBindings.Pause.ApplyTo(actions.Pause);
                inputBindings.SwitchWeapon.ApplyTo(actions.SwitchWeapon);
                inputBindings.SwitchCharacter.ApplyTo(actions.SwitchCharacter);
                inputBindings.TimeControl.ApplyTo(actions.TimeControl);
                inputBindings.Swim.ApplyTo(actions.Swim);
                inputBindings.Glide.ApplyTo(actions.Glide);
                inputBindings.Jetpack.ApplyTo(actions.Jetpack);
                inputBindings.Fly.ApplyTo(actions.Fly);
                inputBindings.Grab.ApplyTo(actions.Grab);
                inputBindings.Throw.ApplyTo(actions.Throw);
                inputBindings.Push.ApplyTo(actions.Push);
                inputBindings.Grip.ApplyTo(actions.Grip);
                inputBindings.Block.ApplyTo(actions.Block);

                actions.ListenOptions.IncludeUnknownControllers = true;
                actions.ListenOptions.IncludeMouseButtons = true;
                actions.ListenOptions.UnsetDuplicateBindingsOnSet = true;

                return actions;
            }
        }
    }
}
