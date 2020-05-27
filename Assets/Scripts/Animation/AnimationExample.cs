using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationExample : MonoBehaviour , GripPlaceDeleteInteractable , TriggerGrabMoveInteractable , CloneMoveInteractable , TouchpadLeftRightClickInteractable
{

    public Transform myBaseToAnimate;
    public Transform[] myRelativePointsToAnimate;
    [HideInInspector] public List<AnimationByRecordedExampleController.ModelBaseDatum> baseExamples;
    [HideInInspector] public List<AnimationByRecordedExampleController.ModelRelativeDatum>[] relativeExamples;
    private AnimationByRecordedExampleController myAnimator;

    public AnimationByRecordedExampleController.RecordingType myRecordingType;

    bool shouldAnimate = false;
    bool shouldReanimateOnEnable = false;
    public float globalSlew = 0.25f;

    private float animationIntertime;

    // NOTE: ensure that goalBaseRotation has the same scale as the mini model of the animal
    // NOTE: ensure that model's pivot and goalBaseRotation are both at center of cube
    private Quaternion goalBaseRotation;

    private Vector3[] goalLocalPositions;

    public Color unactivated, fullyActivated, disabled;
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

    void OnEnable()
    {
        if( shouldReanimateOnEnable )
        {
            Animate( animationIntertime );
        }
    }

    void OnDisable()
    {
        shouldReanimateOnEnable = true;
    }

    public void Initialize( 
        List<AnimationByRecordedExampleController.ModelBaseDatum> baseData,
        List<AnimationByRecordedExampleController.ModelRelativeDatum>[] relativeData,
        AnimationByRecordedExampleController animator,
        AnimationByRecordedExampleController.RecordingType recordingType
    )
    {
        baseExamples = baseData;
        relativeExamples = relativeData;
        myAnimator = animator;
        myRecordingType = recordingType;
    }

    public void ResetAnimator( AnimationByRecordedExampleController animator )
    {
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
        // don't set color if I'm disabled
        if( !amEnabled ) { return; }
        
        // clamp
        a = Mathf.Clamp01( a );

        // compute and set
        activationDisplay.material.color = Color.Lerp( unactivated, fullyActivated, a );
    }

    CloneMoveInteractable CloneMoveInteractable.Clone( out Transform t )
    {
        return CloneExample( myAnimator, out t );
    }

    public AnimationExample CloneExample( AnimationByRecordedExampleController newAnimator, out Transform t )
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
        cloned.Initialize(
            clonedBaseExamples,
            clonedRelativeExamples,
            newAnimator,
            myRecordingType
        );

        // start animating
        cloned.Animate( animationIntertime );


        if( !amEnabled )
        {
            cloned.ToggleEnabled();
        }

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

    private bool amEnabled = true;
    void ToggleEnabled()
    {
        amEnabled = !amEnabled;

        if( amEnabled )
        {
            // go back to normal
            SetActivation( 0 );
        }
        else
        {
            // disable
            activationDisplay.material.color = disabled;
        }

        myAnimator.RescanProvidedExamples();
    }

    public bool IsEnabled()
    {
        return amEnabled;
    }

    void TouchpadLeftRightClickInteractable.InformOfLeftClick()
    {
        ToggleEnabled();
    }

    void TouchpadLeftRightClickInteractable.InformOfRightClick()
    {
        ToggleEnabled();
    }


    public static void ShowHints( AnimationByRecordedExampleController creature, float pauseTimeBeforeFade )
    {
        // null check
        if( creature == null ) { return; }
        
        // show a hint for all used examples, not just currently used ones
        foreach( AnimationExample e in creature.examples )
        {
            e.ShowHint( pauseTimeBeforeFade );
        }
    }

    public MeshRenderer myHint;
    private Coroutine hintCoroutine;
    private void ShowHint( float pauseTimeBeforeFade )
    {
        StopHintAnimation();
        hintCoroutine = StartCoroutine( AnimateHint.AnimateHintFade( myHint, pauseTimeBeforeFade ) );
    }

    private void StopHintAnimation()
    {
        if( hintCoroutine != null )
        {
            StopCoroutine( hintCoroutine );
        }
    }

    public SerializableAnimationExample Serialize()
    {
        SerializableAnimationExample serial = new SerializableAnimationExample();
        serial.position = transform.position;
        serial.baseExamples = baseExamples;
        
        // dumb hack because apparently we can't serialize arrays of lists
        serial.relativeExamples = new List<SerializableRelativeDatumList>();
        for( int i = 0; i < relativeExamples.Length; i++ )
        {
            SerializableRelativeDatumList newList = new SerializableRelativeDatumList();
            newList.examples = relativeExamples[i];
            serial.relativeExamples.Add( newList );
        }

        serial.animationIntertime = animationIntertime;
        serial.enabled = amEnabled;
        serial.recordingType = myRecordingType;
        serial.prefab = prefabName;

        return serial;
    }

    public static AnimationExample Deserialize( SerializableAnimationExample serial, AnimationByRecordedExampleController animator )
    {
        GameObject prefab = (GameObject) Resources.Load( "Prefabs/" + serial.prefab );
        AnimationExample example = Instantiate( prefab ).GetComponent<AnimationExample>();
        example.transform.position = serial.position;

        // annoying conversion due to serialization limits
        List<AnimationByRecordedExampleController.ModelRelativeDatum>[] relativeExamples
            = new List<AnimationByRecordedExampleController.ModelRelativeDatum>[serial.relativeExamples.Count];
        for( int i = 0; i < relativeExamples.Length; i++ )
        {
            relativeExamples[i] = serial.relativeExamples[i].examples;
        }

        example.Initialize( serial.baseExamples, relativeExamples, animator, serial.recordingType );
        example.Animate( serial.animationIntertime );

        if( !serial.enabled )
        {
            example.ToggleEnabled();
        }

        return example;
    }
}


[System.Serializable]
public class SerializableAnimationExample
{
    public Vector3 position;
    public List<AnimationByRecordedExampleController.ModelBaseDatum> baseExamples;
    public List<SerializableRelativeDatumList> relativeExamples;
    public float animationIntertime;
    public string prefab;
    public bool enabled;
    public AnimationByRecordedExampleController.RecordingType recordingType;
}

[System.Serializable]
public class SerializableRelativeDatumList
{
    public List<AnimationByRecordedExampleController.ModelRelativeDatum> examples;
}