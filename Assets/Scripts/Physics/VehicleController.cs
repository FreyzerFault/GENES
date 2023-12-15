using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleController : MonoBehaviour
{
    [SerializeField] private Wheel frontLeftW;
    [SerializeField] private Wheel frontRightW;
    [SerializeField] private Wheel rearLeftW;
    [SerializeField] private Wheel rearRightW;

    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float maxAcceleration = 50f;
    [SerializeField] private float maxBrakeForce = 100f;

    [SerializeField] private float acceleration;
    [SerializeField] private float brakeForce;
    [SerializeField] private float steerAngle;

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

    private void HandleSteering()
    {
        steerAngle = maxSteerAngle * horizontalInput;
        frontLeftW.Steer(steerAngle);
        frontRightW.Steer(steerAngle);
    }

    private void HandleMotor()
    {
        // MOTOR
        acceleration = verticalInput * maxAcceleration;
        rearLeftW.Accelerate(acceleration);
        rearRightW.Accelerate(acceleration);
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