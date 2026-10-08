using ReleSumo.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ReleSumo.Gameplay
{
    /// <summary>
    /// Keyboard adapter for testing the hybrid movement/combat action space.
    /// Keys are configurable so two local fighters can use separate bindings.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SumoMotor))]
    public sealed class KeyboardSumoController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Key moveForwardKey = Key.W;
        [SerializeField] private Key moveBackwardKey = Key.S;
        [SerializeField] private Key moveLeftKey = Key.A;
        [SerializeField] private Key moveRightKey = Key.D;

        [Header("Actions")]
        [SerializeField] private Key dashKey = Key.LeftShift;
        [SerializeField] private Key shoveKey = Key.Space;
        [SerializeField] private Key braceKey = Key.LeftCtrl;

        private SumoMotor motor;

        private void Awake()
        {
            motor = GetComponent<SumoMotor>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                motor.SetCommand(SumoCommand.None);
                return;
            }

            Vector2 move = new(
                ReadAxis(keyboard, moveRightKey, moveLeftKey),
                ReadAxis(keyboard, moveForwardKey, moveBackwardKey));

            motor.SetCommand(new SumoCommand(
                move,
                dash: IsPressed(keyboard, dashKey),
                shove: IsPressed(keyboard, shoveKey),
                brace: IsPressed(keyboard, braceKey)));
        }

        private void OnDisable()
        {
            if (motor != null)
            {
                motor.SetCommand(SumoCommand.None);
            }
        }

        private static float ReadAxis(Keyboard keyboard, Key positive, Key negative)
        {
            float positiveValue = IsPressed(keyboard, positive) ? 1f : 0f;
            float negativeValue = IsPressed(keyboard, negative) ? 1f : 0f;
            return positiveValue - negativeValue;
        }

        private static bool IsPressed(Keyboard keyboard, Key key)
        {
            return key != Key.None && keyboard[key].isPressed;
        }
    }
}
