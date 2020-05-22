using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBirdAngle : MonoBehaviour
{

    public Transform setMyForward;
    public Transform visualizeBoids;
    public Transform visualizeCombinedDirection;

    public Vector3 myDesiredBoids = 1 * Vector3.up;

    Quaternion seamHideRotation = Quaternion.identity;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // set initial
        transform.LookAt( setMyForward );
        Quaternion rotationFromAnimation = transform.rotation; 
        Quaternion rotationWithoutBoids = seamHideRotation * rotationFromAnimation;

        Vector3 velocity = myDesiredBoids;
        // boids desired rotation is to move in velocity direction while keeping the base animation's up-vector
        Quaternion boidsDesiredRotation = Quaternion.LookRotation( velocity, rotationWithoutBoids * Vector3.up );

        visualizeBoids.position = transform.position + ( 1 + velocity.magnitude ) * ( boidsDesiredRotation * Vector3.forward );

        // difference between the desired boids position and rotation without boids
        Quaternion boidsDesiredChange = boidsDesiredRotation * Quaternion.Inverse( rotationWithoutBoids );

        // update seam hide rotation by a certain percentage of the boids desired change, according to strength of boids
        // maximum = velocity of 2 --> 50% of the way there
        float amountToChange = velocity.magnitude.MapClamp( 0, 2, 0, 0.5f );
        Quaternion combinedSeamHideAndBoidsRotation = Quaternion.Slerp( seamHideRotation, boidsDesiredChange * seamHideRotation, amountToChange );

        // update seam hide to be in line with output from most recent boids
        //seamHideRotation = Quaternion.AngleAxis( combinedSeamHideAndBoidsRotation.eulerAngles.y, Vector3.up );

        Quaternion goalBaseRotation = combinedSeamHideAndBoidsRotation * rotationFromAnimation;
        visualizeCombinedDirection.position = transform.position + 1 * ( goalBaseRotation * Vector3.forward );
    }

}
