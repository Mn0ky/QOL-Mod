using UnityEngine;

namespace QOL;

public class Accel : MonoBehaviour
{
    private Rigidbody _whipTip;
    private float m_ElevationAngle = 45f;
    private float m_Impulse = 70f;

    private Vector3 facingDirection
    {
        get
        {
            Vector3 result = transform.forward;
            result.y = 0f;
            return result.sqrMagnitude == 0f ? Vector3.forward : result.normalized;
        }
    }

    void Start()
    {
        _whipTip = GetComponent<Rigidbody>();

        Vector3 direction = facingDirection;
        direction = Quaternion.AngleAxis(m_ElevationAngle, Vector3.Cross(direction, Vector3.up * 50)) * direction;
        _whipTip.AddForce(direction * m_Impulse, ForceMode.Impulse);
    }   

    /*void FixedUpdate()
    {
        _whipTip.AddForce(new Vector3(-10, 5), ForceMode.Impulse);
    }*/

    void Update() => Debug.Log("Velocity: " + _whipTip.velocity);
}