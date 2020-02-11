using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationExample : MonoBehaviour , GripPlaceDeleteInteractable , TriggerGrabMoveInteractable
{

    public Transform myBaseToAnimate;
    public Transform[] myRelativePointsToAnimate;
    [HideInInspector] public List<AnimationByRecordedExampleController.ModelBaseDatum> baseExamples;
    [HideInInspector] public List<AnimationByRecordedExampleController.ModelRelativeDatum>[] relativeExamples;
    private AnimationByRecordedExampleController myAnimator;

    bool shouldAnimate = false;
    public float globalSlew = 0.25f;

    private Quaternion goalBaseRotation;

    private Vector3[] goalLocalPositions;

    public Color unactivated, fullyActivated;
    public MeshRenderer activationDisplay;

    // Start is called before the first frame update
    void Start()
    {
        goalLocalPositions = new Vector3[ myRelativePointsToAnimate.Length ];
    }

    // Update is called once per frame
    void Update()
    {
        if( shouldAnimate )
        {
            // slew the relative positions
            for( int i = 0; i < myRelativePointsToAnimate.Length; i++ )
            {
                Vector3 goalPosition = myBaseToAnimate.TransformPoint( goalLocalPositions[i] );
                Vector3 oldPosition = myRelativePointsToAnimate[i].position;
                Vector3 currentPosition = oldPosition + globalSlew * ( goalPosition - oldPosition );
                myRelativePointsToAnimate[i].position = currentPosition;
            }

            // slew base
            myBaseToAnimate.rotation = Quaternion.Slerp( myBaseToAnimate.rotation, goalBaseRotation, globalSlew );
        }
    }

    public void Initiate( 
        List<AnimationByRecordedExampleController.ModelBaseDatum> baseData,
        List<AnimationByRecordedExampleController.ModelRelativeDatum>[] relativeData,
        AnimationByRecordedExampleController animator
    )
    {
        baseExamples = baseData;
        relativeExamples = relativeData;
        myAnimator = animator;
    }

    public void Animate( float interFrameTime )
    {
        StartCoroutine( AdvanceThroughData( interFrameTime ) );
        shouldAnimate = true;
    }

    private IEnumerator AdvanceThroughData( float interFrameTime )
    {
        int frame = 0;
        while( true )
        {
            goalBaseRotation = baseExamples[frame].rotation;
            for( int i = 0; i < relativeExamples.Length; i++ )
            {
                goalLocalPositions[i] = relativeExamples[i][frame].positionRelativeToBase;
            }

            frame++;
            frame %= baseExamples.Count;
            yield return new WaitForSeconds( interFrameTime );
        }
    }

    void GripPlaceDeleteInteractable.JustPlaced()
    {
        // do nothing; we will not place this with a grip
    }

    void GripPlaceDeleteInteractable.AboutToBeDeleted()
    {
        // forget me
        myAnimator.ForgetExample( this );
    }

    void TriggerGrabMoveInteractable.InformOfTemporaryMovement( Vector3 currentPosition )
    {
        // ignore temporary movement
    }

    void TriggerGrabMoveInteractable.FinalizeMovement( Vector3 endPosition )
    {
        // rewrite my examples
        float height, steepness, distanceAbove;
        myAnimator.FindTerrainInformation( transform.position, out height, out steepness, out distanceAbove );
        for( int i = 0; i < baseExamples.Count; i++ )
        {
            myAnimator.UpdateBaseDatum( baseExamples[i], height, steepness );
        }
        
        
        // and tell my animator to update
        myAnimator.RescanProvidedExamples();
    }

    public void SetActivation( float a )
    {
        // clamp
        a = Mathf.Clamp01( a );

        // compute and set
        activationDisplay.material.color = Color.Lerp( unactivated, fullyActivated, a );
    }
}
