using InControl;
using UnityEngine;

namespace MaskboundJinosi.Input
{
    [AddComponentMenu("Maskbound/Input/Input Inspector Monitor")]
    public class MaskboundInputInspectorMonitor : MonoBehaviour
    {
        [SerializeField] private MaskboundInputBindings inputBindings;

        [Header("Runtime Status")]
        [SerializeField] private string activeDevice;
        [SerializeField] private Vector2 leftStick;
        [SerializeField] private Vector2 rightStick;
        [SerializeField] private bool jump;
        [SerializeField] private bool attack;
        [SerializeField] private bool specialAttack;
        [SerializeField] private bool block;
        [SerializeField] private bool interact;

        private MonitorActions _actions;

        private void Awake()
        {
            _actions = MonitorActions.Create(inputBindings);
        }

        private void OnDestroy()
        {
            _actions?.Destroy();
        }

        private void Update()
        {
            InputDevice device = InputManager.ActiveDevice;
            activeDevice = device != null ? $"{device.Name} ({device.DeviceClass})" : "None";
            leftStick = device != null ? device.LeftStick.Value : Vector2.zero;
            rightStick = device != null ? device.RightStick.Value : Vector2.zero;

            if (_actions == null)
            {
                return;
            }

            jump = _actions.Jump.IsPressed;
            attack = _actions.Attack.IsPressed;
            specialAttack = _actions.SpecialAttack.IsPressed;
            block = _actions.Block.IsPressed;
            interact = _actions.Interact.IsPressed;
        }

        private class MonitorActions : PlayerActionSet
        {
            public readonly PlayerAction Jump;
            public readonly PlayerAction Attack;
            public readonly PlayerAction SpecialAttack;
            public readonly PlayerAction Block;
            public readonly PlayerAction Interact;

            private MonitorActions()
            {
                Jump = CreatePlayerAction("Monitor Jump");
                Attack = CreatePlayerAction("Monitor Attack");
                SpecialAttack = CreatePlayerAction("Monitor Special Attack");
                Block = CreatePlayerAction("Monitor Block");
                Interact = CreatePlayerAction("Monitor Interact");
            }

            public static MonitorActions Create(MaskboundInputBindings inputBindings)
            {
                if (inputBindings == null)
                {
                    inputBindings = ScriptableObject.CreateInstance<MaskboundInputBindings>();
                }

                MonitorActions actions = new MonitorActions();
                inputBindings.Jump.ApplyTo(actions.Jump);
                inputBindings.Attack.ApplyTo(actions.Attack);
                inputBindings.SpecialAttack.ApplyTo(actions.SpecialAttack);
                inputBindings.Block.ApplyTo(actions.Block);
                inputBindings.Interact.ApplyTo(actions.Interact);
                return actions;
            }
        }
    }
}
