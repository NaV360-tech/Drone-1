using UnityEngine;

public class Propellers : MonoBehaviour
{
    [Header("Drone Settings")]
    public float moveSpeed = 5f;
    public float ascendSpeed = 5f;
    public float rotationSpeed = 80f;
    public float tiltAmount = 20f;

    [Header("Physics")]
    public Rigidbody rb;

    [Header("Propellers")]
    public Transform[] propellers;   // Drag MainProp, MainProp.001, MainProp.002, MainProp.003 here
    public float propellerSpeed = 800f;

    float horizontal, vertical, yaw, upDown;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.drag = 1f;
    }

    void Update()
    {
        // ✅ Inputs
        horizontal = Input.GetAxis("Horizontal");   // A/D
        vertical = Input.GetAxis("Vertical");       // W/S

        upDown = 0f;
        if (Input.GetKey(KeyCode.Space)) upDown = 1f;        // Up
        if (Input.GetKey(KeyCode.LeftControl)) upDown = -1f; // Down

        yaw = 0f;
        if (Input.GetKey(KeyCode.Q)) yaw = -1f;  // Rotate Left
        if (Input.GetKey(KeyCode.E)) yaw = 1f;   // Rotate Right

        // ✅ Spin propellers (alternate spin for realism)
        if (propellers.Length > 0)
        {
            for (int i = 0; i < propellers.Length; i++)
            {
                if (i % 2 == 0)
                    propellers[i].Rotate(Vector3.up * propellerSpeed * Time.deltaTime, Space.Self);
                else
                    propellers[i].Rotate(Vector3.down * propellerSpeed * Time.deltaTime, Space.Self);
            }
        }
    }

    void FixedUpdate()
    {
        // ✅ Small lift to counter gravity (hover effect)
        rb.AddForce(Vector3.up * 9.81f, ForceMode.Acceleration);

        // ✅ Movement
        Vector3 move = (transform.forward * vertical + transform.right * horizontal) * moveSpeed;
        Vector3 ascend = Vector3.up * upDown * ascendSpeed;
        rb.AddForce(move + ascend, ForceMode.Acceleration);

        // ✅ Yaw rotation
        rb.AddTorque(Vector3.up * yaw * rotationSpeed);

        // ✅ Tilt effect for realism
        Quaternion targetRotation = Quaternion.Euler(vertical * -tiltAmount, transform.eulerAngles.y, horizontal * -tiltAmount);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
    }
}
