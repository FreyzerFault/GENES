using UnityEngine;

public class Wheel : MonoBehaviour
{
    [SerializeField] private WheelCollider wheelCollider;
    [SerializeField] private Transform wheelModel;

    private void Awake()
    {
        wheelCollider = GetComponent<WheelCollider>();
    }

    private void FixedUpdate()
    {
        wheelCollider.GetWorldPose(out var pos, out var rot);
        // No estoy seguro de si 0.8f sea un buen valor para hacer el movimiento mas smooth
        var smoothLerpCoefficient = 0.8f;
        pos = Vector3.Lerp(wheelModel.transform.position, pos, smoothLerpCoefficient);
        rot = Quaternion.Lerp(wheelModel.transform.rotation, rot, smoothLerpCoefficient);
        wheelModel.transform.SetPositionAndRotation(pos, rot);
    }


    public void Accelerate(float force)
    {
        wheelCollider.motorTorque = force;
    }

    public void Brake(float force)
    {
        wheelCollider.brakeTorque = force;
    }

    public void Steer(float angle)
    {
        wheelCollider.steerAngle = angle;
    }
}