using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Flying : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float rotationSpeed = 100f;
    public float liftForce = 30f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 2f;
        rb.angularDrag = 2f;
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
        HandleLift();
    }

    void HandleMovement()
    {
        float moveForward = Input.GetAxis("Vertical");
        Vector3 force = transform.forward * moveForward * moveSpeed;
        rb.AddForce(force, ForceMode.Acceleration);
    }

    void HandleRotation()
    {
        float rotate = Input.GetAxis("Horizontal");
        Vector3 torque = Vector3.up * rotate * rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(torque));
    }

    void HandleLift()
    {
        if (Input.GetKey(KeyCode.Space))
            rb.AddForce(Vector3.up * liftForce, ForceMode.Acceleration);
        else if (Input.GetKey(KeyCode.LeftShift))
            rb.AddForce(Vector3.down * liftForce, ForceMode.Acceleration);
    }
}
