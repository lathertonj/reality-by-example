using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationExample : MonoBehaviour , GripPlaceDeleteInteractable , TriggerGrabMoveInteractable , CloneMoveInteractable
{

    public Transform myBaseToAnimate;
    public Transform[] myRelativePointsToAnimate;
    [HideInInspector] public List<AnimationByRecordedExampleController.ModelBaseDatum> baseExamples;
    [HideInInspector] public List<AnimationByRecordedExampleController.ModelRelativeDatum>[] relativeExamples;
    private AnimationByRecordedExampleController myAnimator;

    bool shouldAnimate = false;
    public float globalSlew = 0.25f;

    private float animationIntertime;

    private Quaternion goalBaseRotation;

    private Vector3[] goalLocalPositions;

    public Color unactivated, fullyActivated;
    public MeshRenderer activationDisplay;
    private float prevEulerY;

    // can't have a reference to the prefab itself. very frustrating.
    public string prefabName;
    private GameObject animationExamplePrefab;


    // awake is called during Instantiate()
    void Awake()
    {
        goalLocalPositions = new Vector3[ myRelativePointsToAnimate.Length ];
    }

    // Start is called before the first frame update
    void Start()
    {
        prevEulerY = transform.rotation.eulerAngles.y;
        animationExamplePrefab = (GameObject) Resources.Load( "Prefabs/" + prefabName );
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
        animationIntertime = interFrameTime;
        StartCoroutine( AdvanceThroughData() );
        shouldAnimate = true;
    }

    private IEnumerator AdvanceThroughData()
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
            yield return new WaitForSeconds( animationIntertime );
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
        UpdateFeatures();
        
        
        // and tell my animator to update
        myAnimator.RescanProvidedExamples();
    }

    void UpdateFeatures()
    {
        // new features
        float height, steepness, distanceAbove;
        myAnimator.FindTerrainInformation( transform.position, out height, out steepness, out distanceAbove );

        // rotate rotations
        float newEulerY = transform.rotation.eulerAngles.y;
        Quaternion spinRotation = Quaternion.AngleAxis( newEulerY - prevEulerY, Vector3.up );
        prevEulerY = newEulerY;

        for( int i = 0; i < baseExamples.Count; i++ )
        {
            myAnimator.UpdateBaseDatum( baseExamples[i], height, steepness, spinRotation );
        }
    }

    public void SetActivation( float a )
    {
        // clamp
        a = Mathf.Clamp01( a );

        // compute and set
        activationDisplay.material.color = Color.Lerp( unactivated, fullyActivated, a );
    }

    CloneMoveInteractable CloneMoveInteractable.Clone( out Transform t )
    {
        // make a new version
        AnimationExample cloned = Instantiate( animationExamplePrefab, transform.position, transform.rotation ).GetComponent<AnimationExample>();

        // clone data
        List<AnimationByRecordedExampleController.ModelBaseDatum> clonedBaseExamples =
            new List<AnimationByRecordedExampleController.ModelBaseDatum>();
        for( int i = 0; i < baseExamples.Count; i++ )
        {
            clonedBaseExamples.Add( baseExamples[i].Clone() );
        }

        List<AnimationByRecordedExampleController.ModelRelativeDatum>[] clonedRelativeExamples =
            new List<AnimationByRecordedExampleController.ModelRelativeDatum>[ relativeExamples.Length ];
        for( int i = 0; i < relativeExamples.Length; i++ )
        {
            clonedRelativeExamples[i] = new List<AnimationByRecordedExampleController.ModelRelativeDatum>();
            for( int j = 0; j < relativeExamples[i].Count; j++ )
            {
                clonedRelativeExamples[i].Add( relativeExamples[i][j].Clone() );
            }
        }

        // copy over data
        cloned.Initiate(
            clonedBaseExamples,
            clonedRelativeExamples,
            myAnimator
        );

        // start animating
        cloned.Animate( animationIntertime );

        t = cloned.transform;

        return cloned;
    }

    void CloneMoveInteractable.InformOfTemporaryMovement( Vector3 currentPosition )
    {
        // don't do anything while I'm being moved
    }

    void CloneMoveInteractable.FinalizeMovement( Vector3 endPosition )
    {
        // find new features
        UpdateFeatures(); 
        // tell my animator I exist, finally
        myAnimator.ProvideExample( this );
    }
}
