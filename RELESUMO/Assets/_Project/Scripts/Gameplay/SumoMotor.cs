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
        private const int ShoveHitBufferSize = 16;
        private const float DirectionEpsilon = 0.0001f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float acceleration = 28f;
        [SerializeField, Min(0f)] private float brakingAcceleration = 20f;
        [SerializeField, Min(0f)] private float maxPlanarSpeed = 6f;
        [SerializeField, Min(0f)] private float overspeedResistance = 10f;
        [SerializeField, Range(0f, 1f)] private float airControlMultiplier = 0.12f;
        [SerializeField, Min(0f)] private float aimTurnSpeedDegrees = 720f;
        [SerializeField, Range(0f, 1f)] private float minimumGroundNormal = 0.55f;

        [Header("Dash")]
        [FormerlySerializedAs("dashSpeed")]
        [SerializeField, Min(0f)] private float dashVelocityChange = 7f;
        [SerializeField, Min(0f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.8f;
        [SerializeField, Min(0f)] private float dashStaminaCost = 25f;
        [SerializeField, Min(0f)] private float dashImpactImpulse = 4.5f;
        [SerializeField, Min(0f)] private float dashControlSuppression = 0.16f;

        [Header("Shove")]
        [SerializeField, Min(0f)] private float shoveForwardOffset = 1.1f;
        [SerializeField, Min(0.01f)] private float shoveRadius = 0.9f;
        [SerializeField, Min(0f)] private float shoveImpulse = 7f;
        [SerializeField, Min(0f)] private float shoveLiftImpulse = 0.2f;
        [SerializeField, Min(0f)] private float shoveCooldown = 0.65f;
        [SerializeField, Min(0f)] private float shoveStaminaCost = 15f;
        [SerializeField, Min(0f)] private float shoveControlSuppression = 0.2f;
        [SerializeField] private LayerMask opponentLayers = ~0;

        [Header("Brace")]
        [SerializeField, Range(0f, 1f)] private float braceMovementMultiplier = 0.3f;
        [SerializeField, Min(1f)] private float braceMassMultiplier = 2.5f;
        [SerializeField, Range(0f, 1f)] private float braceIncomingImpulseMultiplier = 0.45f;

        [Header("Stamina")]
        [SerializeField, Min(0.01f)] private float maxStamina = 100f;
        [SerializeField, Min(0f)] private float staminaRegenerationPerSecond = 20f;

        private readonly Collider[] shoveHits = new Collider[ShoveHitBufferSize];
        private readonly HashSet<Rigidbody> shovedBodies = new();
        private readonly HashSet<Collider> opponentContacts = new();
        private readonly HashSet<Collider> groundContacts = new();
        private readonly HashSet<SumoMotor> dashHitOpponents = new();

        private Rigidbody body;
        private SumoCommand command;
        private Vector3 aimDirection;
        private float baseMass;
        private float stamina;
        private float dashTimeRemaining;
        private float dashCooldownRemaining;
        private float shoveCooldownRemaining;
        private float controlSuppressionRemaining;

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
        }

        private void OnCollisionStay(Collision collision)
        {
            UpdateCollisionState(collision);
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
            dashCooldownRemaining = 0f;
            shoveCooldownRemaining = 0f;
            controlSuppressionRemaining = 0f;
            opponentContacts.Clear();
            groundContacts.Clear();
            dashHitOpponents.Clear();
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
        /// Attempts a physics shove in the current aim direction.
        /// Returns true when the attack starts, even if it misses.
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
            ApplyShoveHits();
            return true;
        }

        private void StepTimers()
        {
            dashTimeRemaining = Mathf.Max(0f, dashTimeRemaining - Time.fixedDeltaTime);
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

        private void ApplyShoveHits()
        {
            Vector3 center = body.worldCenterOfMass + aimDirection * shoveForwardOffset;
            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                shoveRadius,
                shoveHits,
                opponentLayers,
                QueryTriggerInteraction.Ignore);

            shovedBodies.Clear();

            for (int index = 0; index < hitCount; index++)
            {
                Collider hit = shoveHits[index];
                shoveHits[index] = null;

                if (hit == null)
                {
                    continue;
                }

                SumoMotor opponent = hit.GetComponentInParent<SumoMotor>();
                if (opponent == null || opponent == this || !shovedBodies.Add(opponent.Body))
                {
                    continue;
                }

                Vector3 impulse =
                    aimDirection * shoveImpulse +
                    Vector3.up * shoveLiftImpulse;

                opponent.ReceiveImpact(impulse, shoveControlSuppression);
                ShoveLanded?.Invoke(opponent);
            }
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
            shoveForwardOffset = Mathf.Max(0f, shoveForwardOffset);
            shoveRadius = Mathf.Max(0.01f, shoveRadius);
            shoveImpulse = Mathf.Max(0f, shoveImpulse);
            shoveLiftImpulse = Mathf.Max(0f, shoveLiftImpulse);
            shoveCooldown = Mathf.Max(0f, shoveCooldown);
            shoveStaminaCost = Mathf.Max(0f, shoveStaminaCost);
            shoveControlSuppression = Mathf.Max(0f, shoveControlSuppression);
            braceMassMultiplier = Mathf.Max(1f, braceMassMultiplier);
            maxStamina = Mathf.Max(0.01f, maxStamina);
            staminaRegenerationPerSecond = Mathf.Max(0f, staminaRegenerationPerSecond);
        }

        private void OnDrawGizmosSelected()
        {
            Rigidbody currentBody = body != null ? body : GetComponent<Rigidbody>();
            Vector3 origin = currentBody != null ? currentBody.worldCenterOfMass : transform.position;
            Vector3 direction = aimDirection.sqrMagnitude >= DirectionEpsilon
                ? aimDirection
                : GetInitialAimDirection();

            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.9f);
            Gizmos.DrawLine(origin, origin + direction * shoveForwardOffset);
            Gizmos.DrawWireSphere(origin + direction * shoveForwardOffset, shoveRadius);
        }
    }
}
