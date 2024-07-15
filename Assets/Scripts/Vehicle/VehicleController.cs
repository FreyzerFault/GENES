using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace GENES.Vehicle
{
    public class VehicleController : MonoBehaviour
    {
        // Wheels
        [SerializeField] private Wheel frontLeftW;
        [SerializeField] private Wheel frontRightW;
        [SerializeField] private Wheel rearLeftW;
        [SerializeField] private Wheel rearRightW;

        // Limits
        [SerializeField] private float maxSteerAngle = 30f;

        [FormerlySerializedAs("maxAcceleration")] [SerializeField]
        private float maxTorqueForce = 50f;

        [SerializeField] private float maxBrakeForce = 100f;
        [SerializeField] private float steerAngularVelocity = 30f;

        // Physics Forces and Angle
        [SerializeField] private float torqueForce;

        [SerializeField] private float brakeForce;
        [SerializeField] private float steerAngle;

        // Inputs
        [SerializeField] private float verticalInput;
        [SerializeField] private float horizontalInput;
        [SerializeField] private float brakeInput;

        // PHYSICS
        private void FixedUpdate()
        {
            HandleMotor();
            HandleBrake();
            HandleSteering();
        }


        // ================================== GUI ==================================
        private void OnGUI()
        {
            // Show Info
            // GUI.Label(new Rect(10, 10, 100, 20), $"Torque: {torqueForce}");
            // GUI.Label(new Rect(10, 30, 100, 20), $"Brake: {brakeForce}");
            // GUI.Label(new Rect(10, 50, 100, 20), $"Steer: {steerAngle}");
        }

        // ================================== GIZMOS ==================================
        private void OnDrawGizmos()
        {
            // Center of Mass
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(GetComponent<Rigidbody>().worldCenterOfMass, 0.2f);

            Gizmos.color = Color.red;
            var position = transform.position;
            var forward = transform.forward;
            var frontPosition = position + forward * 2;
            var steerFrontDirection = Vector3.RotateTowards(
                forward,
                transform.right,
                Mathf.Deg2Rad * steerAngle,
                Mathf.Deg2Rad * maxSteerAngle
            );
            Gizmos.DrawRay(frontPosition, forward * torqueForce / maxTorqueForce);
            Gizmos.DrawRay(frontPosition, steerFrontDirection);
        }

        // ================================== PHYSICS ==================================

        private void HandleSteering()
        {
            steerAngle = Mathf.MoveTowardsAngle(
                steerAngle,
                maxSteerAngle * horizontalInput,
                steerAngularVelocity * Time.fixedDeltaTime
            );
            frontLeftW.Steer(steerAngle);
            frontRightW.Steer(steerAngle);
        }

        private void HandleMotor()
        {
            // MOTOR
            torqueForce = verticalInput * maxTorqueForce;
            rearLeftW.AccelerateByTorque(torqueForce);
            rearRightW.AccelerateByTorque(torqueForce);
        }

        private void HandleBrake()
        {
            // BRAKES
            brakeForce = brakeInput * maxBrakeForce;
            frontLeftW.Brake(brakeForce);
            frontRightW.Brake(brakeForce);
            rearLeftW.Brake(brakeForce);
            rearRightW.Brake(brakeForce);
        }

        // INPUTS
        public void OnForwardBackwards(InputValue value)
        {
            verticalInput = value.Get<float>();
        }

        public void OnSteering(InputValue value)
        {
            horizontalInput = value.Get<float>();
        }

        public void OnBrake(InputValue value)
        {
            brakeInput = value.Get<float>();
        }
    }
}