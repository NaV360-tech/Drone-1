using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [Tooltip("Target max forward/backward speed (m/s).")]
    public float maxForwardSpeed = 10f;
    [Tooltip("Target max vertical speed (m/s).")]
    public float maxVerticalSpeed = 6f;

    [Header("Acceleration / Smoothing")]
    [Tooltip("How quickly horizontal speed changes to the target (seconds). Lower = snappier.")]
    public float horizontalSmoothTime = 0.15f;
    [Tooltip("How quickly vertical speed changes to the target (seconds).")]
    public float verticalSmoothTime = 0.15f;
    [Tooltip("How quickly yaw (turn) input smooths (seconds).")]
    public float yawSmoothTime = 0.1f;
    [Tooltip("Maximum yaw rate in degrees per second.")]
    public float maxYawRate = 120f;

    [Header("Forces")]
    [Tooltip("Extra throttle to counter gravity and make hovering easy (0 = off; 1 = full gravity counter).")]
    [Range(0f, 1.5f)] public float hoverAssist = 1.0f;
    [Tooltip("How strong the forward acceleration feels.")]
    public float forwardAcceleration = 12f;   // N/kg ≈ m/s^2
    [Tooltip("How strong the vertical acceleration feels.")]
    public float verticalAcceleration = 20f;  // N/kg ≈ m/s^2
    [Tooltip("How strong the yaw torque feels.")]
    public float yawTorque = 6f;

    [Header("Damping / Limits")]
    [Tooltip("Extra damping on sideways drift so it doesn't slide forever.")]
    public float lateralDamping = 2.5f;
    [Tooltip("Clamp horizontal speed to this magnitude (m/s).")]
    public float maxHorizontalSpeed = 12f;

    private Rigidbody rb;

    // Smoothed inputs (stateful)
    private float smoothedForwardInput = 0f, forwardInputVel = 0f;
    private float smoothedLiftInput = 0f, liftInputVel = 0f;
    private float smoothedYawInput = 0f, yawInputVel = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        // smoother visuals
        // Let physics handle rotation; we only drive yaw (around Y). You can tweak constraints if needed:
        // rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void FixedUpdate()
    {
        // --- 1) Read inputs (same bindings as your original script) ---
        float forwardInput = Input.GetAxis("Vertical");   // W/S or Up/Down
        float yawInput = Input.GetAxis("Horizontal");     // A/D or Left/Right

        float liftInput = 0f;                             // Space (up), LeftShift (down)
        if (Input.GetKey(KeyCode.Space)) liftInput += 1f;
        if (Input.GetKey(KeyCode.LeftShift)) liftInput -= 1f;

        // --- 2) Smooth the inputs over time for buttery control ---
        smoothedForwardInput = Mathf.SmoothDamp(
            smoothedForwardInput, forwardInput, ref forwardInputVel, horizontalSmoothTime);

        smoothedLiftInput = Mathf.SmoothDamp(
            smoothedLiftInput, liftInput, ref liftInputVel, verticalSmoothTime);

        smoothedYawInput = Mathf.SmoothDamp(
            smoothedYawInput, yawInput, ref yawInputVel, yawSmoothTime);

        // --- 3) Compute desired local velocities ---
        // Forward speed target (local Z)
        float targetForwardVel = smoothedForwardInput * maxForwardSpeed;
        // Vertical speed target (world Y)
        float targetUpVel = smoothedLiftInput * maxVerticalSpeed;

        // Current velocities
        Vector3 vel = rb.velocity;
        Vector3 localVel = transform.InverseTransformDirection(vel);

        // --- 4) Accelerate toward target velocities (forces) ---
        // Forward acceleration toward target
        float forwardVelError = targetForwardVel - localVel.z;
        float forwardAccelCmd = Mathf.Clamp(forwardVelError / Mathf.Max(Time.fixedDeltaTime, 0.0001f),
                                            -forwardAcceleration, forwardAcceleration);
        Vector3 forwardForce = transform.forward * forwardAccelCmd * rb.mass;

        // Vertical acceleration toward target (world up)
        float upVelError = targetUpVel - vel.y;
        float upAccelCmd = Mathf.Clamp(upVelError / Mathf.Max(Time.fixedDeltaTime, 0.0001f),
                                       -verticalAcceleration, verticalAcceleration);
        // Hover assist to counteract gravity
        float hoverNewton = rb.mass * Physics.gravity.magnitude * hoverAssist;
        Vector3 upForce = (Vector3.up * upAccelCmd * rb.mass) + (Vector3.up * hoverNewton);

        // Apply damping to sideways drift (local X) to feel less slippery
        float lateralVel = localVel.x;
        Vector3 lateralDampForce = -transform.right * lateralVel * lateralDamping * rb.mass;

        rb.AddForce(forwardForce + upForce + lateralDampForce, ForceMode.Force);

        // --- 5) Yaw control (torque) ---
        // Target yaw rate (deg/s), then convert to a torque command
        //float targetYawRate = smoothedYawInput * maxYawRate;
        //float currentYawRateDeg = Mathf.Rad2Deg * transform.InverseTransformDirection(rb.angularVelocity).y;
        //float yawRateError = targetYawRate - currentYawRateDeg;

        // Proportional torque on Y (simple, effective)
        //Vector3 yawTorqueCmd = Vector3.up * (yawRateError * yawTorque);
        //rb.AddTorque(yawTorqueCmd, ForceMode.Force);

        // --- 5) Yaw control (torque) ---
        float targetYawRate = smoothedYawInput * maxYawRate; // deg/s
        float currentYawRateDeg = Mathf.Rad2Deg * rb.angularVelocity.y;
        float yawRateError = targetYawRate - currentYawRateDeg;

        // Apply torque to rotate around world up (Y axis)
        Vector3 yawTorqueCmd = Vector3.up * yawRateError * yawTorque;
        rb.AddTorque(yawTorqueCmd, ForceMode.Acceleration);


        // --- 6) Clamp speeds to keep things sane ---
        // Limit horizontal (XZ) speed
        Vector3 horizVel = new Vector3(vel.x, 0f, vel.z);
        if (horizVel.sqrMagnitude > (maxHorizontalSpeed * maxHorizontalSpeed))
        {
            Vector3 clampedHoriz = horizVel.normalized * maxHorizontalSpeed;
            vel = new Vector3(clampedHoriz.x, vel.y, clampedHoriz.z);
        }
        // Limit vertical speed
        vel.y = Mathf.Clamp(vel.y, -maxVerticalSpeed, maxVerticalSpeed);
        rb.velocity = vel;
    }
}
