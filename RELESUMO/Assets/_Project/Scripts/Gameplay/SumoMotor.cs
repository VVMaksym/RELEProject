using System;
using System.Collections.Generic;
using ReleSumo.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace ReleSumo.Gameplay
{
    /// <summary>
    /// Semi-physical movement and combat for one sumo fighter.
    /// The fighter stays upright, while movement, dash, shove, and impacts use physics forces.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class SumoMotor : MonoBehaviour
    {
        private const float DirectionEpsilon = 0.0001f;

        [Header("Movement")]
        [Tooltip("Maximum horizontal acceleration while movement input is held, in m/s².")]
        [SerializeField, Min(0f)] private float acceleration = 20f;
        [Tooltip("Maximum horizontal deceleration with no movement input, in m/s².")]
        [SerializeField, Min(0f)] private float brakingAcceleration = 16f;
        [Tooltip("Normal horizontal movement speed limit, in m/s.")]
        [SerializeField, Min(0f)] private float maxPlanarSpeed = 4.5f;
        [Tooltip("Acceleration used to bring post-impact or post-dash speed back toward the normal limit.")]
        [SerializeField, Min(0f)] private float overspeedResistance = 8f;
        [Tooltip("Fraction of normal movement control available while airborne.")]
        [SerializeField, Range(0f, 1f)] private float airControlMultiplier = 0.1f;
        [Tooltip("Maximum visual/body yaw rotation speed in degrees per second.")]
        [SerializeField, Min(0f)] private float aimTurnSpeedDegrees = 720f;
        [Tooltip("Minimum upward contact-normal dot product that counts as grounded.")]
        [SerializeField, Range(0f, 1f)] private float minimumGroundNormal = 0.55f;

        [Header("Dash")]
        [FormerlySerializedAs("dashSpeed")]
        [Tooltip("Instant horizontal speed added when dash starts, in m/s.")]
        [SerializeField, Min(0f)] private float dashVelocityChange = 4f;
        [Tooltip("Time during which normal movement control is locked and dash contact can land.")]
        [SerializeField, Min(0f)] private float dashDuration = 0.16f;
        [Tooltip("Minimum time between dash attempts, in seconds.")]
        [SerializeField, Min(0f)] private float dashCooldown = 0.8f;
        [Tooltip("Stamina spent by a successful dash start.")]
        [SerializeField, Min(0f)] private float dashStaminaCost = 25f;
        [Tooltip("Additional contact impulse applied to an opponent during a dash, in N·s.")]
        [SerializeField, Min(0f)] private float dashImpactImpulse = 4.5f;
        [Tooltip("How long the opponent temporarily loses movement control after a dash hit.")]
        [SerializeField, Min(0f)] private float dashControlSuppression = 0.16f;

        [Header("Shove")]
        [Tooltip("Short window in which real body contact can turn the shove into a hit.")]
        [SerializeField, Min(0.02f)] private float shoveActiveDuration = 0.18f;
        [Tooltip("Required dot product between shove direction and direction to the opponent. Higher values narrow the forward contact cone.")]
        [SerializeField, Range(-1f, 1f)] private float minimumShoveAlignment = 0.2f;
        [Tooltip("Horizontal contact impulse applied by a successful shove, in N·s.")]
        [SerializeField, Min(0f)] private float shoveImpulse = 9f;
        [Tooltip("Small upward impulse that helps the opponent slide instead of sticking to the floor.")]
        [SerializeField, Min(0f)] private float shoveLiftImpulse = 0.15f;
        [Tooltip("Minimum time between shove attempts, in seconds.")]
        [SerializeField, Min(0f)] private float shoveCooldown = 0.65f;
        [Tooltip("Stamina spent when a shove starts, including a miss.")]
        [SerializeField, Min(0f)] private float shoveStaminaCost = 15f;
        [Tooltip("How long the opponent temporarily loses movement control after a shove hit.")]
        [SerializeField, Min(0f)] private float shoveControlSuppression = 0.2f;

        [Header("Brace")]
        [Tooltip("Fraction of normal movement speed retained while bracing.")]
        [SerializeField, Range(0f, 1f)] private float braceMovementMultiplier = 0.35f;
        [Tooltip("Temporary Rigidbody mass multiplier while bracing.")]
        [SerializeField, Min(1f)] private float braceMassMultiplier = 1.6f;
        [Tooltip("Multiplier applied to explicit dash/shove impulses received while bracing.")]
        [SerializeField, Range(0f, 1f)] private float braceIncomingImpulseMultiplier = 0.6f;

        [Header("Stamina")]
        [Tooltip("Maximum shared stamina pool for dash and shove.")]
        [SerializeField, Min(0.01f)] private float maxStamina = 100f;
        [Tooltip("Stamina restored per second while not bracing.")]
        [SerializeField, Min(0f)] private float staminaRegenerationPerSecond = 20f;

        private readonly HashSet<Collider> opponentContacts = new();
        private readonly HashSet<Collider> groundContacts = new();
        private readonly HashSet<SumoMotor> dashHitOpponents = new();
        private readonly HashSet<SumoMotor> shoveHitOpponents = new();

        private Rigidbody body;
        private SumoCommand command;
        private Vector3 aimDirection;
        private float baseMass;
        private float stamina;
        private float dashTimeRemaining;
        private float shoveTimeRemaining;
        private float dashCooldownRemaining;
        private float shoveCooldownRemaining;
        private float controlSuppressionRemaining;
        private Vector3 shoveDirection;

        public event Action<SumoMotor> DashLanded;
        public event Action<SumoMotor> ShoveLanded;

        public Rigidbody Body => body;
        public Vector3 AimDirection => aimDirection;
        public Vector3 PlanarVelocity =>
            body == null ? Vector3.zero : new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
        public float Stamina => stamina;
        public float NormalizedStamina => maxStamina <= 0f ? 0f : Mathf.Clamp01(stamina / maxStamina);
        public float DashCooldownRemaining => dashCooldownRemaining;
        public float ShoveCooldownRemaining => shoveCooldownRemaining;
        public float ControlSuppressionRemaining => controlSuppressionRemaining;
        public float DashReadiness =>
            dashCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(dashCooldownRemaining / dashCooldown);
        public float ShoveReadiness =>
            shoveCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(shoveCooldownRemaining / shoveCooldown);
        public bool IsDashing => dashTimeRemaining > 0f;
        public bool IsShoving => shoveTimeRemaining > 0f;
        public bool IsBracing { get; private set; }
        public bool IsGrounded => groundContacts.Count > 0;
        public bool IsInContact => opponentContacts.Count > 0;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            baseMass = Mathf.Max(0.0001f, body.mass);
            stamina = maxStamina;
            aimDirection = GetInitialAimDirection();
        }

        private void OnDisable()
        {
            command = SumoCommand.None;
            opponentContacts.Clear();
            groundContacts.Clear();
            dashHitOpponents.Clear();
            shoveHitOpponents.Clear();
            shoveTimeRemaining = 0f;

            if (body != null)
            {
                SetBraceState(false);
            }
        }

        private void FixedUpdate()
        {
            StepTimers();

            Vector3 moveDirection = ToWorldDirection(command.Move);
            UpdateAimDirection(moveDirection);
            SetBraceState(command.Brace);
            RotateTowardsAim();

            if (!IsBracing)
            {
                if (command.Dash)
                {
                    TryDash();
                }

                if (command.Shove)
                {
                    TryShove();
                }

                RegenerateStamina();
            }

            ApplyMovement(moveDirection);
            ApplyOverspeedResistance();
        }

        private void OnCollisionEnter(Collision collision)
        {
            UpdateCollisionState(collision);
            TryApplyDashImpact(collision.collider);
            TryApplyShoveImpact(collision.collider);
        }

        private void OnCollisionStay(Collision collision)
        {
            UpdateCollisionState(collision);
            TryApplyShoveImpact(collision.collider);
        }

        private void OnCollisionExit(Collision collision)
        {
            opponentContacts.Remove(collision.collider);
            groundContacts.Remove(collision.collider);
        }

        /// <summary>
        /// Replaces the command that remains active until another command is submitted.
        /// </summary>
        public void SetCommand(SumoCommand nextCommand)
        {
            command = nextCommand;
        }

        /// <summary>
        /// Clears velocity, actions, cooldowns, contacts, and restores stamina.
        /// Match reset code should position and rotate the body separately.
        /// </summary>
        public void ResetMotion()
        {
            command = SumoCommand.None;
            stamina = maxStamina;
            dashTimeRemaining = 0f;
            shoveTimeRemaining = 0f;
            dashCooldownRemaining = 0f;
            shoveCooldownRemaining = 0f;
            controlSuppressionRemaining = 0f;
            opponentContacts.Clear();
            groundContacts.Clear();
            dashHitOpponents.Clear();
            shoveHitOpponents.Clear();
            shoveDirection = Vector3.zero;
            SetBraceState(false);

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            aimDirection = GetInitialAimDirection();
            body.WakeUp();
        }

        /// <summary>
        /// Starts a physics dash in the current aim direction.
        /// </summary>
        public bool TryDash()
        {
            if (!IsGrounded ||
                IsBracing ||
                IsDashing ||
                dashCooldownRemaining > 0f ||
                !TrySpendStamina(dashStaminaCost))
            {
                return false;
            }

            dashTimeRemaining = dashDuration;
            dashCooldownRemaining = dashCooldown;
            dashHitOpponents.Clear();

            body.AddForce(aimDirection * dashVelocityChange, ForceMode.VelocityChange);
            return true;
        }

        /// <summary>
        /// Opens a short shove window in the current aim direction.
        /// The impulse lands only through a real collider contact with an opponent in front.
        /// </summary>
        public bool TryShove()
        {
            if (!IsGrounded ||
                IsBracing ||
                shoveCooldownRemaining > 0f ||
                !TrySpendStamina(shoveStaminaCost))
            {
                return false;
            }

            shoveCooldownRemaining = shoveCooldown;
            shoveTimeRemaining = shoveActiveDuration;
            shoveDirection = aimDirection;
            shoveHitOpponents.Clear();
            ApplyShoveToCurrentContacts();
            return true;
        }

        private void StepTimers()
        {
            dashTimeRemaining = Mathf.Max(0f, dashTimeRemaining - Time.fixedDeltaTime);
            shoveTimeRemaining = Mathf.Max(0f, shoveTimeRemaining - Time.fixedDeltaTime);
            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - Time.fixedDeltaTime);
            shoveCooldownRemaining = Mathf.Max(0f, shoveCooldownRemaining - Time.fixedDeltaTime);
            controlSuppressionRemaining = Mathf.Max(0f, controlSuppressionRemaining - Time.fixedDeltaTime);
        }

        private void ApplyMovement(Vector3 moveDirection)
        {
            if (IsDashing || controlSuppressionRemaining > 0f)
            {
                return;
            }

            float controlMultiplier = IsGrounded ? 1f : airControlMultiplier;
            float movementMultiplier = IsBracing ? braceMovementMultiplier : 1f;
            Vector3 targetVelocity = moveDirection * (maxPlanarSpeed * movementMultiplier);
            Vector3 velocityDelta = targetVelocity - PlanarVelocity;

            float accelerationLimit = moveDirection.sqrMagnitude >= DirectionEpsilon
                ? acceleration
                : brakingAcceleration;
            accelerationLimit *= controlMultiplier;

            if (accelerationLimit <= 0f || velocityDelta.sqrMagnitude < DirectionEpsilon)
            {
                return;
            }

            Vector3 requiredAcceleration = velocityDelta / Time.fixedDeltaTime;
            Vector3 appliedAcceleration = Vector3.ClampMagnitude(requiredAcceleration, accelerationLimit);
            body.AddForce(appliedAcceleration, ForceMode.Acceleration);
        }

        private void ApplyOverspeedResistance()
        {
            if (IsDashing || controlSuppressionRemaining > 0f || overspeedResistance <= 0f)
            {
                return;
            }

            Vector3 planarVelocity = PlanarVelocity;
            float permittedSpeed = maxPlanarSpeed * (IsBracing ? braceMovementMultiplier : 1f);
            float excessSpeed = planarVelocity.magnitude - permittedSpeed;

            if (excessSpeed <= 0f)
            {
                return;
            }

            float resistance = Mathf.Min(overspeedResistance, excessSpeed / Time.fixedDeltaTime);
            body.AddForce(-planarVelocity.normalized * resistance, ForceMode.Acceleration);
        }

        private void UpdateAimDirection(Vector3 moveDirection)
        {
            if (moveDirection.sqrMagnitude >= DirectionEpsilon)
            {
                aimDirection = moveDirection.normalized;
            }
        }

        private void RotateTowardsAim()
        {
            if (aimDirection.sqrMagnitude < DirectionEpsilon || aimTurnSpeedDegrees <= 0f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            Quaternion nextRotation = Quaternion.RotateTowards(
                body.rotation,
                targetRotation,
                aimTurnSpeedDegrees * Time.fixedDeltaTime);
            body.MoveRotation(nextRotation);
        }

        private void ApplyShoveToCurrentContacts()
        {
            foreach (Collider opponentCollider in opponentContacts)
            {
                TryApplyShoveImpact(opponentCollider);
            }
        }

        private void TryApplyShoveImpact(Collider otherCollider)
        {
            if (!IsShoving || otherCollider == null)
            {
                return;
            }

            SumoMotor opponent = otherCollider.GetComponentInParent<SumoMotor>();
            if (opponent == null || opponent == this || shoveHitOpponents.Contains(opponent))
            {
                return;
            }

            Vector3 directionToOpponent = opponent.Body.worldCenterOfMass - body.worldCenterOfMass;
            directionToOpponent.y = 0f;
            if (directionToOpponent.sqrMagnitude < DirectionEpsilon ||
                Vector3.Dot(shoveDirection, directionToOpponent.normalized) < minimumShoveAlignment)
            {
                return;
            }

            shoveHitOpponents.Add(opponent);
            Vector3 impulse =
                shoveDirection * shoveImpulse +
                Vector3.up * shoveLiftImpulse;

            opponent.ReceiveImpact(impulse, shoveControlSuppression);
            ShoveLanded?.Invoke(opponent);
        }

        private void TryApplyDashImpact(Collider otherCollider)
        {
            if (!IsDashing || otherCollider == null)
            {
                return;
            }

            SumoMotor opponent = otherCollider.GetComponentInParent<SumoMotor>();
            if (opponent == null || opponent == this || !dashHitOpponents.Add(opponent))
            {
                return;
            }

            opponent.ReceiveImpact(aimDirection * dashImpactImpulse, dashControlSuppression);
            DashLanded?.Invoke(opponent);
        }

        private void ReceiveImpact(Vector3 impulse, float suppressionDuration)
        {
            float defenseMultiplier = IsBracing ? braceIncomingImpulseMultiplier : 1f;
            body.AddForce(impulse * defenseMultiplier, ForceMode.Impulse);
            controlSuppressionRemaining = Mathf.Max(
                controlSuppressionRemaining,
                suppressionDuration * defenseMultiplier);
        }

        private void SetBraceState(bool shouldBrace)
        {
            if (IsBracing == shouldBrace)
            {
                return;
            }

            IsBracing = shouldBrace;
            body.mass = IsBracing ? baseMass * braceMassMultiplier : baseMass;
        }

        private void RegenerateStamina()
        {
            stamina = Mathf.Min(maxStamina, stamina + staminaRegenerationPerSecond * Time.fixedDeltaTime);
        }

        private bool TrySpendStamina(float amount)
        {
            amount = Mathf.Max(0f, amount);
            if (stamina < amount)
            {
                return false;
            }

            stamina -= amount;
            return true;
        }

        private void UpdateCollisionState(Collision collision)
        {
            Collider otherCollider = collision.collider;
            if (otherCollider == null)
            {
                return;
            }

            SumoMotor opponent = otherCollider.GetComponentInParent<SumoMotor>();
            if (opponent != null && opponent != this)
            {
                opponentContacts.Add(otherCollider);
            }

            bool hasGroundContact = false;
            for (int index = 0; index < collision.contactCount; index++)
            {
                if (Vector3.Dot(collision.GetContact(index).normal, Vector3.up) >= minimumGroundNormal)
                {
                    hasGroundContact = true;
                    break;
                }
            }

            if (hasGroundContact)
            {
                groundContacts.Add(otherCollider);
            }
            else
            {
                groundContacts.Remove(otherCollider);
            }
        }

        private Vector3 GetInitialAimDirection()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude < DirectionEpsilon ? Vector3.forward : forward.normalized;
        }

        private static Vector3 ToWorldDirection(Vector2 move)
        {
            Vector2 clampedMove = Vector2.ClampMagnitude(move, 1f);
            return new Vector3(clampedMove.x, 0f, clampedMove.y);
        }

        private void OnValidate()
        {
            acceleration = Mathf.Max(0f, acceleration);
            brakingAcceleration = Mathf.Max(0f, brakingAcceleration);
            maxPlanarSpeed = Mathf.Max(0f, maxPlanarSpeed);
            overspeedResistance = Mathf.Max(0f, overspeedResistance);
            aimTurnSpeedDegrees = Mathf.Max(0f, aimTurnSpeedDegrees);
            dashVelocityChange = Mathf.Max(0f, dashVelocityChange);
            dashDuration = Mathf.Max(0f, dashDuration);
            dashCooldown = Mathf.Max(0f, dashCooldown);
            dashStaminaCost = Mathf.Max(0f, dashStaminaCost);
            dashImpactImpulse = Mathf.Max(0f, dashImpactImpulse);
            dashControlSuppression = Mathf.Max(0f, dashControlSuppression);
            shoveActiveDuration = Mathf.Max(0.02f, shoveActiveDuration);
            minimumShoveAlignment = Mathf.Clamp(minimumShoveAlignment, -1f, 1f);
            shoveImpulse = Mathf.Max(0f, shoveImpulse);
            shoveLiftImpulse = Mathf.Max(0f, shoveLiftImpulse);
            shoveCooldown = Mathf.Max(0f, shoveCooldown);
            shoveStaminaCost = Mathf.Max(0f, shoveStaminaCost);
            shoveControlSuppression = Mathf.Max(0f, shoveControlSuppression);
            braceMassMultiplier = Mathf.Max(1f, braceMassMultiplier);
            maxStamina = Mathf.Max(0.01f, maxStamina);
            staminaRegenerationPerSecond = Mathf.Max(0f, staminaRegenerationPerSecond);
        }

    }
}
