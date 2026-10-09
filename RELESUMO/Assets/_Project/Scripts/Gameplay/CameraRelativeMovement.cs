using UnityEngine;

namespace ReleSumo.Gameplay
{
    /// <summary>
    /// Converts screen-oriented player input into a direction on the world XZ plane.
    /// </summary>
    public static class CameraRelativeMovement
    {
        private const float DirectionEpsilon = 0.0001f;

        public static Vector2 ToWorldDirection(Vector2 input, Transform cameraTransform)
        {
            input = Vector2.ClampMagnitude(input, 1f);
            if (cameraTransform == null || input.sqrMagnitude < DirectionEpsilon)
            {
                return input;
            }

            Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up);

            if (cameraForward.sqrMagnitude < DirectionEpsilon ||
                cameraRight.sqrMagnitude < DirectionEpsilon)
            {
                return input;
            }

            Vector3 worldDirection =
                cameraForward.normalized * input.y +
                cameraRight.normalized * input.x;
            worldDirection = Vector3.ClampMagnitude(worldDirection, 1f);

            return new Vector2(worldDirection.x, worldDirection.z);
        }
    }
}
