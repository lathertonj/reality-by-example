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
    private bool amKnownToAnimator = false;

    private float animationIntertime;

    // NOTE: ensure that goalBaseRotation has the same scale as the mini model of the animal
    // NOTE: ensure that model's pivot and goalBaseRotation are both at center of cube
    private Quaternion goalBaseRotation;

    private Vector3[] goalLocalPositions;

    public Color unactivated, fullyActivated, disabled;
    public MeshRenderer activationDisplay;

    // can't have a reference to the prefab itself. very frustrating.
    public string prefabName;
    private GameObject animationExamplePrefab;

    private List<AnimationExample> myGroup = null;


    // awake is called during Instantiate()
    void Awake()
    {
        goalLocalPositions = new Vector3[ myRelativePointsToAnimate.Length ];
    }

    // Start is called before the first frame update
    void Start()
    {
        animationExamplePrefab = (GameObject) Resources.Load( "Prefabs/" + prefabName );

        // if my group is null, I'm the first one and I haven't been initialized in a group
        if( myGroup == null )
        {
            myGroup = new List<AnimationExample>();
            myGroup.Add( this );
        }
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
        AnimationByRecordedExampleController.RecordingType recordingType,
        bool knownToAnimator
    )
    {
        baseExamples = baseData;
        relativeExamples = relativeData;
        myAnimator = animator;
        myRecordingType = recordingType;
        amKnownToAnimator = knownToAnimator;
    }

    public void ResetAnimator( AnimationByRecordedExampleController animator )
    {
        foreach( AnimationExample e in myGroup )
        {
            e.myAnimator = animator;
        }        
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
        // am I known to the animator? if so, tell it to forget me
        if( amKnownToAnimator ) 
        {
            // don't rescan -- we will do it for sure below
            myAnimator.ForgetExample( this, false );
        }

        // remove me from the group
        myGroup.Remove( this );

        // if I was not the last of my group, introduce another
        // member of the group to the animator
        if( amKnownToAnimator && myGroup.Count > 0 )
        {
            // will call Rescan
            myAnimator.ProvideExample( myGroup[0], true );
            // remember
            myGroup[0].amKnownToAnimator = true;
        }
        else
        {
            // removed an example, so still need to rescan
            myAnimator.RescanMyProvidedExamples();
        }
    }

    void TriggerGrabMoveInteractable.InformOfTemporaryMovement( Vector3 currentPosition )
    {
        // ignore temporary movement
    }

    void TriggerGrabMoveInteractable.FinalizeMovement( Vector3 endPosition )
    {
        // and tell my animator to update since I have a new position
        myAnimator.RescanProvidedExamples();
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

        // don't need to clone data -- it's shared
        // just pass a reference
        // false: not known to animator directly (but will be accessed through myGroup)
        cloned.Initialize( baseExamples, relativeExamples, newAnimator, myRecordingType, false );

        // add to group
        cloned.myGroup = myGroup;
        myGroup.Add( cloned );

        // start animating
        cloned.Animate( animationIntertime );

        // disable
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
        // tell my animator I exist, finally
        // I'm part of a group, so it will automatically find me if I:
        myAnimator.RescanProvidedExamples();
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

    public List<AnimationExample> Group()
    {
        return myGroup;
    }


    public static void ShowHints( AnimationByRecordedExampleController creature, float pauseTimeBeforeFade )
    {
        // null check
        if( creature == null ) { return; }
        
        // show a hint for all used examples, not just currently used ones
        foreach( AnimationExample g in creature.examples )
        {
            foreach( AnimationExample e in g.Group() )
            {
                e.ShowHint( pauseTimeBeforeFade );
            }
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

        // TODO: handle groups in serialization
        example.Initialize( serial.baseExamples, relativeExamples, animator, serial.recordingType, true );
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
    // TODO make list
    public Vector3 position;
    public List<AnimationByRecordedExampleController.ModelBaseDatum> baseExamples;
    public List<SerializableRelativeDatumList> relativeExamples;
    public float animationIntertime;
    public string prefab;
    // TODO make list
    public bool enabled;
    public AnimationByRecordedExampleController.RecordingType recordingType;
}

[System.Serializable]
public class SerializableRelativeDatumList
{
    public List<AnimationByRecordedExampleController.ModelRelativeDatum> examples;
}