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
                _actions = MaskboundCorgiActions.CreateWithDefaultBindings();
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

            public static MaskboundCorgiActions CreateWithDefaultBindings()
            {
                var actions = new MaskboundCorgiActions();

                actions.MoveLeft.AddDefaultBinding(Key.A);
                actions.MoveLeft.AddDefaultBinding(Key.LeftArrow);
                actions.MoveLeft.AddDefaultBinding(InputControlType.LeftStickLeft);
                actions.MoveLeft.AddDefaultBinding(InputControlType.DPadLeft);

                actions.MoveRight.AddDefaultBinding(Key.D);
                actions.MoveRight.AddDefaultBinding(Key.RightArrow);
                actions.MoveRight.AddDefaultBinding(InputControlType.LeftStickRight);
                actions.MoveRight.AddDefaultBinding(InputControlType.DPadRight);

                actions.MoveUp.AddDefaultBinding(Key.W);
                actions.MoveUp.AddDefaultBinding(Key.UpArrow);
                actions.MoveUp.AddDefaultBinding(InputControlType.LeftStickUp);
                actions.MoveUp.AddDefaultBinding(InputControlType.DPadUp);

                actions.MoveDown.AddDefaultBinding(Key.S);
                actions.MoveDown.AddDefaultBinding(Key.DownArrow);
                actions.MoveDown.AddDefaultBinding(InputControlType.LeftStickDown);
                actions.MoveDown.AddDefaultBinding(InputControlType.DPadDown);

                actions.AimLeft.AddDefaultBinding(InputControlType.RightStickLeft);
                actions.AimRight.AddDefaultBinding(InputControlType.RightStickRight);
                actions.AimUp.AddDefaultBinding(InputControlType.RightStickUp);
                actions.AimDown.AddDefaultBinding(InputControlType.RightStickDown);

                actions.Jump.AddDefaultBinding(Key.Space);
                actions.Jump.AddDefaultBinding(InputControlType.Action1);

                actions.Run.AddDefaultBinding(Key.LeftShift);
                actions.Run.AddDefaultBinding(InputControlType.LeftBumper);

                actions.Dash.AddDefaultBinding(Key.LeftControl);
                actions.Dash.AddDefaultBinding(InputControlType.Action2);

                actions.Roll.AddDefaultBinding(Key.LeftAlt);
                actions.Roll.AddDefaultBinding(InputControlType.Action4);

                actions.Attack.AddDefaultBinding(Key.E);
                // actions.Attack.AddDefaultBinding(Mouse.LeftButton);
                actions.Attack.AddDefaultBinding(InputControlType.Action3);
                actions.Attack.AddDefaultBinding(InputControlType.RightTrigger);

                actions.SpecialAttack.AddDefaultBinding(Key.Q);
                actions.SpecialAttack.AddDefaultBinding(Mouse.RightButton);
                actions.SpecialAttack.AddDefaultBinding(InputControlType.Action4);
                actions.SpecialAttack.AddDefaultBinding(InputControlType.LeftTrigger);

                actions.Interact.AddDefaultBinding(Key.F);
                actions.Interact.AddDefaultBinding(InputControlType.Action1);

                actions.Reload.AddDefaultBinding(Key.R);
                actions.Reload.AddDefaultBinding(InputControlType.Action4);

                actions.Pause.AddDefaultBinding(Key.Escape);
                actions.Pause.AddDefaultBinding(InputControlType.Start);

                actions.SwitchWeapon.AddDefaultBinding(Key.Tab);
                actions.SwitchWeapon.AddDefaultBinding(InputControlType.RightBumper);

                actions.SwitchCharacter.AddDefaultBinding(Key.C);
                actions.TimeControl.AddDefaultBinding(Key.T);
                actions.Swim.AddDefaultBinding(Key.Space);
                actions.Glide.AddDefaultBinding(Key.Space);
                actions.Jetpack.AddDefaultBinding(Key.Space);
                actions.Fly.AddDefaultBinding(Key.Space);
                actions.Grab.AddDefaultBinding(Key.G);
                actions.Throw.AddDefaultBinding(Key.H);
                actions.Push.AddDefaultBinding(Key.P);
                actions.Grip.AddDefaultBinding(Key.G);
                actions.Block.AddDefaultBinding(Key.B);

                actions.ListenOptions.IncludeUnknownControllers = true;
                actions.ListenOptions.IncludeMouseButtons = true;
                actions.ListenOptions.UnsetDuplicateBindingsOnSet = true;

                return actions;
            }
        }
    }
}
