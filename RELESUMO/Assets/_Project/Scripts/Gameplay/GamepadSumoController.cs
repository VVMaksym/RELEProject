using ReleSumo.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ReleSumo.Gameplay
{
    /// <summary>
    /// Analog gamepad adapter for SumoMotor. Select a gamepad by index for local multiplayer.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SumoMotor))]
    public sealed class GamepadSumoController : MonoBehaviour
    {
        [Header("Device")]
        [SerializeField, Min(0)] private int gamepadIndex = 0;
        [SerializeField, Range(0f, 0.95f)] private float movementDeadzone = 0.15f;

        [Header("Movement Reference")]
        [Tooltip("Optional movement reference. When empty, the active Main Camera is used.")]
        [SerializeField] private Transform movementReference = null;
        [SerializeField] private bool useMainCameraAsMovementReference = true;

        [Header("Actions")]
        [SerializeField] private GamepadButton dashButton = GamepadButton.RightShoulder;
        [SerializeField] private GamepadButton shoveButton = GamepadButton.South;
        [SerializeField] private GamepadButton braceButton = GamepadButton.LeftShoulder;

        private SumoMotor motor;
        private Transform cachedMainCameraTransform;

        private void Awake()
        {
            motor = GetComponent<SumoMotor>();
        }

        private void Update()
        {
            Gamepad gamepad = GetSelectedGamepad();
            if (gamepad == null)
            {
                motor.SetCommand(SumoCommand.None);
                return;
            }

            Vector2 rawMove = ApplyRadialDeadzone(gamepad.leftStick.ReadValue());
            Vector2 move = CameraRelativeMovement.ToWorldDirection(
                rawMove,
                GetMovementReference());

            motor.SetCommand(new SumoCommand(
                move,
                dash: gamepad[dashButton].isPressed,
                shove: gamepad[shoveButton].isPressed,
                brace: gamepad[braceButton].isPressed));
        }

        private void OnDisable()
        {
            if (motor != null)
            {
                motor.SetCommand(SumoCommand.None);
            }
        }

        private Gamepad GetSelectedGamepad()
        {
            return gamepadIndex >= 0 && gamepadIndex < Gamepad.all.Count
                ? Gamepad.all[gamepadIndex]
                : null;
        }

        private Transform GetMovementReference()
        {
            if (movementReference != null)
            {
                return movementReference;
            }

            if (!useMainCameraAsMovementReference)
            {
                return null;
            }

            if (cachedMainCameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                cachedMainCameraTransform = mainCamera != null ? mainCamera.transform : null;
            }

            return cachedMainCameraTransform;
        }

        private Vector2 ApplyRadialDeadzone(Vector2 input)
        {
            float magnitude = input.magnitude;
            if (magnitude <= movementDeadzone)
            {
                return Vector2.zero;
            }

            float scaledMagnitude = Mathf.InverseLerp(movementDeadzone, 1f, magnitude);
            return input.normalized * scaledMagnitude;
        }
    }
}
