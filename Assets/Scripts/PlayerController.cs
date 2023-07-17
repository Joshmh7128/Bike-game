using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    /// script controlls the player on their bike
    /// 

    [SerializeField] WheelCollider backWheel, frontWheel;
    [SerializeField] float pedalTorqueMultiplier, backWheelTorqueMultiplier;
    [SerializeField] InputAction pedalAction; // our input from our triggers pedals

    [SerializeField] Gamepad gamepad;
    [SerializeField] float turnScale, turnSpeed, lastTurnRotation, targetRotation; // what is our turn scale?
    [SerializeField] Rigidbody handlebars; // our handlebars
    [SerializeField] Transform fakeHandlerbars, body, pedalContaniner; // our fake handlebars

    public float forceToApply, readFloat; // the rotational force we want to apply
    [SerializeField] float lastRight, lastLeft;
    [SerializeField] bool onRight; // are we on our right or left foot?

    [SerializeField] Transform pedalT;

    // for our IK purposes
    [SerializeField] Transform rightFootTarget, leftFootTarget, rightFootPop, leftFootPop;
    [SerializeField] Rigidbody rightFootTargetRB, leftFootTargetRB;

    private void OnEnable()
    {
        gamepad = Gamepad.current;
        pedalAction.Enable();
    }

    private void OnDisable()
    {
        pedalAction.Disable();
    }


    private void FixedUpdate()
    {
        // first get our input
        ReceivePedalInput();
        // update feet
        UpdateFeet();
        // process force
        ProcessForce();
        // process handlebar input
        ProcessHandlebarInput();
    }

    void ReceivePedalInput()
    {
        // we only want to get the difference of input from each leg since the last input, until it passes our bottom out threshhold
        // this is how a bike works: force is applied based on how much we push with each foot, until pushing with that foot does nothing else,
        // and we have to switch to our other foot. we have to physically base the foot's threshold reset too, so that we force the player to use the other foot
        /// forces are read like this!
        readFloat = pedalAction.ReadValue<float>();

        // based on our input, apply force downwards to either the right or left pedals
        // get our right input up to 1, as long as it is more than our last rightInput

        if (onRight)
        {
            // if our lastRight is less than 0.9
            if (lastRight < 1)
            {
                if (pedalAction.ReadValue<float>() > lastRight)
                {
                    /// first, apply our force to the back wheel
                    ApplyForce(pedalAction.ReadValue<float>() - lastRight);
                    // then update the last movement amount
                    lastRight = pedalAction.ReadValue<float>();
                }
            } 
            else if (lastRight >= 1)
            {
                // if our lastRight is more than 0.9, we have bottomed out! We are no longer on our right foot
                lastLeft = 0;
                onRight = false;
            }
            
        }

        // get our left input down to -1, as long as it is less than our last leftInput
        if (!onRight)
        {
            // if our lastRight is less than 0.9
            if (lastLeft > -1)
            {
                if (pedalAction.ReadValue<float>() < lastLeft)
                {
                    /// first, apply our force to the back wheel, in this case it has to be the absolute value because it is negative
                    ApplyForce(Mathf.Abs(pedalAction.ReadValue<float>() - lastLeft));
                    // then update the last movement amount
                    lastLeft = pedalAction.ReadValue<float>();
                }
            }
            else if (lastLeft <= -1)
            {
                // if our lastLeft is less than -0.9 then we have bottomed out, go to the right foot
                lastRight = 0;
                onRight = true;
            }
            
        }
    }

    // we apply our force to the pedals and to the back wheel at the same time
    void ApplyForce(float force)
    {
        // add to our force to apply
        forceToApply += force * backWheelTorqueMultiplier * Time.fixedDeltaTime;
    }

    void ProcessForce()
    {
        // apply that rotation to the back wheel
        // backWheel.AddRelativeTorque(new Vector3(forceToApply * backWheelTorqueMultiplier * Time.fixedDeltaTime, 0, 0), ForceMode.Force);
        pedalT.transform.localEulerAngles = new Vector3(pedalT.transform.localEulerAngles.x + forceToApply, 0, 0);

        backWheel.motorTorque = forceToApply * backWheelTorqueMultiplier;

        // overtime, reduce the force
        if (forceToApply > 0)
            forceToApply -= Time.fixedDeltaTime * (backWheelTorqueMultiplier / 50);

        if (forceToApply < 0)
            forceToApply = 0;
    }

    private void UpdateFeet()
    {
        // update feet
        rightFootPop.position = rightFootTarget.position;
        leftFootPop.position = leftFootTarget.position;

    }

    void ProcessHandlebarInput()
    {
        // calculate where we want the handlebars to be
        targetRotation = gamepad.leftStick.ReadValue().x * turnScale;

        // lerp our rotation to that
        float clamped = Mathf.Clamp(targetRotation, -turnScale, turnScale);

        lastTurnRotation = Mathf.Lerp(fakeHandlerbars.localEulerAngles.y, clamped, turnSpeed * Time.fixedDeltaTime);

        fakeHandlerbars.localEulerAngles = new Vector3(0, lastTurnRotation, 0);

        // lerp the rotation of the handlebars
        handlebars.rotation = fakeHandlerbars.rotation;

        frontWheel.steerAngle = lastTurnRotation;
    }
}
