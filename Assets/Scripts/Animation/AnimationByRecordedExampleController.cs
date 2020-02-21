using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class AnimationByRecordedExampleController : MonoBehaviour , GripPlaceDeleteInteractable
{
    private static List< AnimationByRecordedExampleController > allCreatures = new List< AnimationByRecordedExampleController >();
    private List< AnimationByRecordedExampleController > myGroup = null;
    [HideInInspector] public Transform prefabThatCreatedMe;
    public enum PredictionType { Classification, Regression };
    public PredictionType predictionType;

    public enum AnimationAction { RecordAnimation, DoNothing };

    public enum RecordingType { ConstantTime, MusicTempo };
    public RecordingType currentRecordingAndPlaybackMode = RecordingType.ConstantTime;
    private static RecordingType recordingTypeForNewBirds = RecordingType.ConstantTime;
    public AnimationAction nextAction = AnimationAction.DoNothing;

    public AnimationExample examplePrefab;

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean startStopDataCollection;

    public Transform modelBaseDataSource, modelBaseToAnimate;
    public Transform[] modelRelativePointsDataSource, modelRelativePointsToAnimate;

    // Basic idea:
    // predict the increment to modelBase location and rotation
    // input features: y, steepness of terrain; previous location / rotation.
    // --> 2 regressions for modelBase
    public List<AnimationExample> examples = null, currentlyUsedExamples;
    //private List<List<ModelBaseDatum>> modelBasePositionData;
    private RapidMixClassifier myAnimationClassifier;
    private RapidMixRegression myAnimationRegression;


    // predict the position of each modelRelativePoint, relative to modelBase
    // input features: y, steepness of terrain, quaternion of current rotation
    //private List<List<ModelRelativeDatum>>[] modelRelativePositionData;

    // and, have a maximum amount that each thing can actually move -- maybe a slew per frame.
    public float globalSlew = 0.1f;

    public float motionSpeedupSlew = 0.08f;
    public float motionSlowdownSlew = 0.03f;
    public float maxSpeed = 1;
    public float avoidTerrainAngle = 30;
    public float avoidTerrainDetection = 2f;
    public float avoidTerrainMinheight = 1.5f;
    public float boidSlew = 0.01f;

    public float maxDistanceFromAnyExample = 15f;
    public float distanceRampUpRange = 10f;
    public float rampUpSeverity = 1.5f;

    // and, specify a data collection rate and a prediction output rate.
    public float dataCollectionRate = 0.1f;

    // TODO use AngleMinify function?
    public Vector3 maxEulerAnglesChange;

    // other stuff
    bool haveTrained = false;
    bool runtimeMode = false;

    bool currentlyRecording = false;

    IEnumerator dataCollectionCoroutine = null, runtimeCoroutine = null;

    private AnimationSoundRecorderPlaybackController mySounder = null;


    void Awake()
    {
        allCreatures.Add( this );
        currentRecordingAndPlaybackMode = recordingTypeForNewBirds;
        InitializeIndependently();

        currentlyUsedExamples = new List<AnimationExample>();
        if( predictionType == PredictionType.Classification )
        {
            myAnimationClassifier = gameObject.AddComponent<RapidMixClassifier>();
        }
        else
        {
            myAnimationRegression = gameObject.AddComponent<RapidMixRegression>();
        }
        goalLocalPositions = new Vector3[modelRelativePointsDataSource.Length];

        mySounder = GetComponent<AnimationSoundRecorderPlaybackController>();

        // free the dummy points
        modelBaseToAnimate.parent = null;
        for( int i = 0; i < modelRelativePointsToAnimate.Length; i++ )
        {
            modelRelativePointsToAnimate[i].parent = null;
        }
    }

    public void AddToGroup( AnimationByRecordedExampleController groupLeader )
    {
        myGroup = groupLeader.myGroup;
        myGroup.Add( this );
        examples = groupLeader.examples;
    }

    private void InitializeIndependently()
    {
        myGroup = new List< AnimationByRecordedExampleController >();
        myGroup.Add( this );
        examples = new List<AnimationExample>();
    }

    public void CloneAudioSystem( AnimationByRecordedExampleController toCloneFrom, bool shareSamples )
    {
        if( mySounder != null && toCloneFrom.mySounder != null )
        {
            mySounder.CloneFrom( toCloneFrom.mySounder, shareSamples );
        }
    }

    

    // Update is called once per frame
    void Update()
    {
        if( runtimeMode )
        {
            // keep track of how much movement happens
            float movementThisFrame = 0;

            // slew the relative positions while computing movement 
            for( int i = 0; i < modelRelativePointsToAnimate.Length; i++ )
            {
                Vector3 goalPosition = modelBaseToAnimate.TransformPoint( goalLocalPositions[i] );
                Vector3 oldPosition = modelRelativePointsToAnimate[i].position;
                Vector3 currentPosition = oldPosition + globalSlew * ( goalPosition - oldPosition );
                modelRelativePointsToAnimate[i].position = currentPosition;

                movementThisFrame += ( currentPosition - oldPosition ).magnitude;
            }

            // derive ideal movement speed from movement of the limbs
            movementThisFrame /= modelRelativePointsToAnimate.Length;
            goalSpeedMultiplier = 10 * Mathf.Clamp( movementThisFrame, 0, 0.1f );

            // current speed is delayed from ideal
            if( currentSpeedMultiplier < goalSpeedMultiplier )
            {
                currentSpeedMultiplier += motionSpeedupSlew * ( goalSpeedMultiplier - currentSpeedMultiplier );
            }
            else
            {
                currentSpeedMultiplier += motionSlowdownSlew * ( goalSpeedMultiplier - currentSpeedMultiplier );
            }

            // slew base
            modelBaseToAnimate.rotation = Quaternion.Slerp( modelBaseToAnimate.rotation, goalBaseRotation, globalSlew );

            // get base velocity
            Vector3 baseVelocity = modelBaseToAnimate.forward;

            // boids
            Vector3 groundAvoidance = ProcessBoidsGroundAvoidance();
            Vector3 examplesAttraction = ProcessBoidsExamplesAttraction();
            Vector3 boidAvoidance = ProcessBoidsOthersAvoidance();

            // overall velocity
            Vector3 velocity = baseVelocity + groundAvoidance + examplesAttraction + boidAvoidance;

            // move in the forward direction, with speed according to delayed limb movement
            modelBaseToAnimate.position += maxSpeed * currentSpeedMultiplier * Time.deltaTime * velocity;

            // change the rotation to be looking in that direction
            // TODO: does this negatively impact animation overall?
            modelBaseToAnimate.rotation = Quaternion.LookRotation( velocity, modelBaseToAnimate.up );


        }
        else if( nextAction == AnimationAction.RecordAnimation )
        {
            // animate it as just following the data sources, only if we're recording data or might again soon
            modelBaseToAnimate.position = modelBaseDataSource.position;
            modelBaseToAnimate.rotation = modelBaseDataSource.rotation;

            for( int i = 0; i < modelRelativePointsToAnimate.Length; i++ )
            {
                modelRelativePointsToAnimate[i].position = modelRelativePointsDataSource[i].position;
            }
        }

        // grips == have to start / stop coroutines BOTH for data collection AND for running
        if( nextAction == AnimationAction.RecordAnimation && startStopDataCollection.GetStateDown( handType ) )
        {
            // stop runtime if it's running
            if( runtimeCoroutine != null )
            {
                StopCoroutine( runtimeCoroutine );
                runtimeCoroutine = null;
                // need to set this ourselves due to 
                // artificially stopping runtimeCoroutine
                runtimeMode = false;
            }

            // start data collection
            dataCollectionCoroutine = CollectPhrase();
            StartCoroutine( dataCollectionCoroutine );

            // remember
            currentlyRecording = true;
        }
        else if( currentlyRecording && startStopDataCollection.GetStateUp( handType ) )
        {
            // stop data collection if it's going
            if( dataCollectionCoroutine != null )
            {
                // animation
                StopCoroutine( dataCollectionCoroutine );
                dataCollectionCoroutine = null;

                // finished collecting data --> tell the new phrase to animate itself
                examples[ examples.Count - 1 ].Animate( dataCollectionRate );

                // sound
                if( mySounder )
                {
                    mySounder.StopRecordingExamples();
                    // copy new examples into others 
                    foreach( AnimationByRecordedExampleController creature in myGroup )
                    {
                        creature.mySounder.CatchUpToGroup();
                    }
                }
            }


            // train and run
            RescanProvidedExamples();

            // remember
            currentlyRecording = false;
        }

    }

    

    Vector3 mostRecentTerrainFoundPoint;
    private Terrain FindTerrain( Vector3 nearPoint )
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( nearPoint + 400 * Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
        {
            mostRecentTerrainFoundPoint = hit.point;
            return hit.transform.GetComponentInParent<Terrain>();
        }
        return null;
    }

    private void FindTerrainInformation( out float height, out float steepness, out float distanceAbove )
    {
        FindTerrainInformation( transform.position, out height, out steepness, out distanceAbove );
    }

    public void FindTerrainInformation( Vector3 fromPosition, out float height, out float steepness, out float distanceAbove )
    {
        Terrain currentTerrain = FindTerrain( fromPosition );
        if( currentTerrain )
        {
            Vector2 terrainCoords = CoordinatesToIndices( currentTerrain, modelBaseDataSource.position );
            height = currentTerrain.terrainData.GetInterpolatedHeight( terrainCoords.x, terrainCoords.y );
            steepness = currentTerrain.terrainData.GetSteepness( terrainCoords.x, terrainCoords.y );
            distanceAbove = transform.position.y - mostRecentTerrainFoundPoint.y;
        }
        else
        {
            height = steepness = distanceAbove = 0;
        }
    }

    private Vector2 CoordinatesToIndices( Terrain t, Vector3 worldPos )
    {
        Vector2 indices = Vector2Int.zero;
        Vector3 localPos = worldPos - t.transform.position;
        indices.x = localPos.x / t.terrainData.size.x;
        indices.y = localPos.z / t.terrainData.size.z;

        return indices;
    }

    // TODO: 
    // 0. Set the previous translation to be current position - previous position
    // 1. Set the previous rotation to be current rotation * inv( prev rotation ) // AND DOUBLE CHECK THIS
    // 2. Set the goalBasePosition based on currentPosition + output of regression
    // 3. Set the goalRotation based on output of regression * currentRotation
    // Vector3 goalBasePosition;
    Quaternion goalBaseRotation;

    Vector3[] goalLocalPositions;


    private Quaternion seamHideRotation;
    int currentRuntimeFrame = 0;
    private IEnumerator Run()
    {
        runtimeMode = true;
        seamHideRotation = Quaternion.identity;
        currentRuntimeFrame = 0;
        // collect data and predict
        if( predictionType == PredictionType.Classification )
        {
            while( haveTrained )
            {
                RunOneFrameClassifier();

                switch( currentRecordingAndPlaybackMode )
                {
                    case RecordingType.ConstantTime:
                        // since we are playing back recorded animations,
                        // playback rate == collection rate
                        yield return new WaitForSecondsRealtime( dataCollectionRate );
                        break;
                    case RecordingType.MusicTempo:
                        // play back at 16th note rate
                        yield return new WaitForSecondsRealtime( SoundEngine.GetQuarterNoteTime() / 4 );
                        break;
                }

            }
        }
        else
        {
            while( haveTrained )
            {
            
                RunOneFrameRegression();
                switch( currentRecordingAndPlaybackMode )
                {
                    case RecordingType.ConstantTime:
                        // since we are playing back recorded animations,
                        // playback rate == collection rate
                        yield return new WaitForSecondsRealtime( dataCollectionRate );
                        break;
                    case RecordingType.MusicTempo:
                        // play back at 16th note rate
                        yield return new WaitForSecondsRealtime( SoundEngine.GetQuarterNoteTime() / 4 );
                        break;
                }

            }
        }
        runtimeMode = false;
    }

    private void RunOneFrameRegression()
    {
            // 1. Run regression and normalize to get relative levels of each animation
            // 2. Average together all the results of that frame offset
            // 3. Use that as the next goal position / rotation
            double[] baseInput = FindBaseInput( modelBaseToAnimate.position );

            // animation
            double[] o = myAnimationRegression.Run( baseInput );
            // normalize o
            double sum = 0;
            // clamp to [0, inf)
            for( int i = 0; i < o.Length; i++ ) { o[i] = Mathf.Clamp( (float) o[i], 0, float.MaxValue ); }
            // compute sum
            for( int i = 0; i < o.Length; i++ ) { sum += o[i]; }
            // divide by sum
            for( int i = 0; i < o.Length; i++ ) { o[i] /= sum; }

            // show activation
            for( int i = 0; i < currentlyUsedExamples.Count; i++ ) 
            {
                currentlyUsedExamples[i].SetActivation( (float) o[i] );
            }

            // TODO: how to do a weighted average of Quaternion? maybe with slerp?
            // see: https://stackoverflow.com/questions/12374087/average-of-multiple-quaternions
            // average of many requires finding eigenvectors
            // --> just slerp between the largest 2 -- we can still do a weighted avg of 2

            // find top two indices
            int mostProminent = 0, secondMostProminent = 0;
            double mpAmount = 0;
            for( int i = 0; i < o.Length; i++ )
            {
                if( o[i] > mpAmount )
                {
                    mpAmount = o[i];
                    secondMostProminent = mostProminent;
                    mostProminent = i;
                }
            }

            // re normalize 
            float slerpAmount = (float) o[secondMostProminent] / (float) ( o[mostProminent] + o[secondMostProminent] );

            // goal rotation is weighted average between the two most prominent animations
            goalBaseRotation = Quaternion.Slerp(
                GetBaseQuaternion( mostProminent, currentRuntimeFrame ),
                GetBaseQuaternion( secondMostProminent, currentRuntimeFrame ),
                slerpAmount
            );

            // rotate the animation by whatever angle we were at when we started
            // this loop
            goalBaseRotation = seamHideRotation * goalBaseRotation;


            // weighted average of vectors:
            // compute the relative position goals
            for( int i = 0; i < goalLocalPositions.Length; i++ )
            {
                goalLocalPositions[i] = Vector3.zero;

                // TODO: number of examples not necessarily same as number of animations..??? some examples may reuse the same animation
                // but in different positions. I smell a redesign!
                for( int whichAnimation = 0; whichAnimation < currentlyUsedExamples.Count; whichAnimation++ )
                {
                    // weighted sum
                    goalLocalPositions[i] += (float) o[whichAnimation] * GetLocalPosition( i, whichAnimation, currentRuntimeFrame );

                }
            }

            // sound
            if( mySounder )
            {
                // compute features
                float currentHeight = 0, currentSteepness = 0, heightAboveTerrain = 0;
                FindTerrainInformation( out currentHeight, out currentSteepness, out heightAboveTerrain );
                mySounder.Predict( SoundInput(
                    modelBaseToAnimate.rotation,
                    currentHeight,
                    currentSteepness,
                    heightAboveTerrain
                ) );
            }

            currentRuntimeFrame++;

            // update seam hiding rotation:
            // if the next frame represents "restarting" the most prominent animation
            if( mostProminent < currentlyUsedExamples.Count && currentRuntimeFrame % currentlyUsedExamples[ mostProminent ].baseExamples.Count == 0 )
            {
                seamHideRotation = Quaternion.AngleAxis( 
                    goalBaseRotation.eulerAngles.y - currentlyUsedExamples[ mostProminent ].baseExamples[0].rotation.eulerAngles.y, 
                    Vector3.up
                );
            }
        
    }

    private void RunOneFrameClassifier()
    {
        // 1. Run classifier to see which animation we should pull from
        // 2. Use a time offset to see which point in the animation we should be in
        //    (OR enforce that the animation offset is the same as recording offset (duh)
        //     and use an incrementing frame number -- computationally simpler.
        //     don't forget to i % len(animation) in case we just changed animations)
        // 3. Use that as the next goal position
        double[] baseInput = FindBaseInput( modelBaseToAnimate.position );
        // animation
        string o = myAnimationClassifier.Run( baseInput );
        int whichAnimation = System.Convert.ToInt32( o );

        goalBaseRotation = GetBaseQuaternion( whichAnimation, currentRuntimeFrame );

        // compute the relative position goals
        for( int i = 0; i < goalLocalPositions.Length; i++ )
        {
            goalLocalPositions[i] = GetLocalPosition( i, whichAnimation, currentRuntimeFrame );
        }

        // sound
        if( mySounder )
        {
            // compute features
            float currentHeight = 0, currentSteepness = 0, heightAboveTerrain = 0;
            FindTerrainInformation( out currentHeight, out currentSteepness, out heightAboveTerrain );
            mySounder.Predict( SoundInput(
                modelBaseToAnimate.rotation,
                currentHeight,
                currentSteepness,
                heightAboveTerrain
            ) );
        }

        currentRuntimeFrame++;
    }

    private Quaternion GetBaseQuaternion( int whichAnimation, int currentFrame )
    {
        int numFrames = currentlyUsedExamples[whichAnimation].baseExamples.Count;
        return currentlyUsedExamples[whichAnimation].baseExamples[ currentFrame % numFrames ].rotation;
    }

    private Vector3 GetLocalPosition( int i, int whichAnimation, int currentFrame )
    {
        int numFrames = currentlyUsedExamples[whichAnimation].relativeExamples[i].Count;
        return currentlyUsedExamples[whichAnimation].relativeExamples[i][ currentFrame % numFrames ].positionRelativeToBase;
    }


    
    private IEnumerator CollectPhrase()
    {
        float startTime = Time.time;
        Vector3 prevPosition = modelBaseDataSource.position;
        Vector3 prevRotation = modelBaseDataSource.rotation.eulerAngles;

        List<ModelBaseDatum> basePhrase = new List<ModelBaseDatum>();
        List<ModelRelativeDatum>[] relativePhrases = new List<ModelRelativeDatum>[modelRelativePointsDataSource.Length];
        for( int i = 0; i < relativePhrases.Length; i++ )
        {
            relativePhrases[i] = new List<ModelRelativeDatum>();
        }

        // store the data in the example!
        AnimationExample newExample = Instantiate( examplePrefab, modelBaseDataSource.position, Quaternion.identity );
        examples.Add( newExample );
        // TODO ensure this is a shallow copy and that the lists are identical
        newExample.Initiate( basePhrase, relativePhrases, this, currentRecordingAndPlaybackMode );


        // start sound
        if( mySounder )
        {
            mySounder.StartRecordingExamples();
        }

        while( true )
        {
            switch( currentRecordingAndPlaybackMode )
            {
                case RecordingType.ConstantTime:
                    // collect at the data collection rate
                    yield return new WaitForSecondsRealtime( dataCollectionRate );
                    break;
                case RecordingType.MusicTempo:
                    // collect at 16th note rate
                    yield return new WaitForSecondsRealtime( SoundEngine.GetQuarterNoteTime() / 4 );
                    break;
            }

            // fetch terrain values
            float currentHeight = 0, currentSteepness = 0, heightAboveTerrain = 0;
            FindTerrainInformation( out currentHeight, out currentSteepness, out heightAboveTerrain );

            float timeElapsed = Time.time - startTime;

            // base datum
            ModelBaseDatum newDatum = new ModelBaseDatum();
            // direct way
            newDatum.rotation = modelBaseDataSource.rotation;
            
            // indirect way: from the hand positions
            // This can be used if we don't have a base data source,
            // but it's not great
            // Vector3 averageForward = Vector3.zero;
            // Vector3 averageUp = Vector3.zero;
            // for( int i = 0; i < modelRelativePointsDataSource.Length; i++ )
            // {
            //     averageForward += modelRelativePointsDataSource[i].forward;
            //     averageUp += modelRelativePointsDataSource[i].up;
            // }
            // averageForward.Normalize();
            // averageUp.Normalize();
            // newDatum.rotation = Quaternion.LookRotation( averageForward, averageUp );

            newDatum.terrainHeight = currentHeight;
            newDatum.terrainSteepness = currentSteepness;

            basePhrase.Add( newDatum );

            // sound
            if( mySounder )
            {
                double[] input = SoundInput( newDatum.rotation, newDatum.terrainHeight, newDatum.terrainSteepness, heightAboveTerrain );
                double[] output = mySounder.ProvideExample( input );
                foreach( AnimationByRecordedExampleController creature in myGroup )
                {
                    if( creature == this ) { continue; }
                    creature.mySounder.ProvideExample( input, output );
                }
            }

            // other data
            for( int i = 0; i < modelRelativePointsDataSource.Length; i++ )
            {
                ModelRelativeDatum newRelativeDatum = new ModelRelativeDatum();
                // find local position: local position of X relative to B is B.inverseTransformPoint(X.position);
                Vector3 localPosition = modelBaseDataSource.InverseTransformPoint( modelRelativePointsDataSource[i].position );
                newRelativeDatum.positionRelativeToBase = localPosition;

                relativePhrases[i].Add( newRelativeDatum );
            }

            prevPosition = modelBaseDataSource.position;
            prevRotation = modelBaseDataSource.rotation.eulerAngles;
        }
    }

    public void ProvideExample( AnimationExample e, bool shouldRescan = true )
    {
        examples.Add( e );

        if( shouldRescan )
        {
            RescanProvidedExamples();
        }
    }

    public void ForgetExample( AnimationExample e )
    {
        if( examples.Remove( e ) )
        {
            // successfully removed example --> rescan remaining ones
            RescanProvidedExamples();
        }
    }
    

    public void SwitchRecordingMode( RecordingType newMode )
    {
        // don't do anything if there's no change
        if( newMode == currentRecordingAndPlaybackMode ) { return; }

        // store
        currentRecordingAndPlaybackMode = newMode;

        // re-filter examples and retrain
        RescanProvidedExamples();
    }

    public void SwitchRecordingMode( AnimationByRecordedExampleController matchCreature )
    {
        SwitchRecordingMode( matchCreature.currentRecordingAndPlaybackMode );
    }


    public static void SwitchGlobalRecordingMode( RecordingType newMode )
    {
        foreach( AnimationByRecordedExampleController creature in allCreatures )
        {
            creature.SwitchRecordingMode( newMode );
        }
        recordingTypeForNewBirds = newMode;
    }


    public void RescanProvidedExamples()
    {
        foreach( AnimationByRecordedExampleController creature in myGroup )
        {
            creature.RescanMyProvidedExamples();
        }
    }


    public void RescanMyProvidedExamples()
    {
        // just reset the training... hopefully if we're in runtime already everything 
        // will be fine
        Train();

        // start runtime
        if( !runtimeMode )
        {
            runtimeCoroutine = Run();
            StartCoroutine( runtimeCoroutine );
        }
    }

    // base:
    // predict the increment to modelBase location and rotation
    // input features: y, steepness of terrain; previous location / rotation.
    double[] BaseInput( ModelBaseDatum d )
    {
        return new double[] {
            d.terrainHeight,
            d.terrainSteepness,
        };
    }



    ModelBaseDatum _dummy = new ModelBaseDatum();
    double[] FindBaseInput( Vector3 worldPos )
    {
        float h, s, da;
        FindTerrainInformation( out h, out s, out da );
        _dummy.terrainHeight = h;
        _dummy.terrainSteepness = s;

        return BaseInput( _dummy );
    }

    string BaseOutput( int label )
    {
        return label.ToString();
    }

    public void UpdateBaseDatum( ModelBaseDatum d, float newHeight, float newSteepness, Quaternion spinRotation )
    {
        d.terrainHeight = newHeight;
        d.terrainSteepness = newSteepness;
        d.rotation = spinRotation * d.rotation;
    }

    void Train()
    {
        currentlyUsedExamples.Clear();
        foreach( AnimationExample e in examples )
        {
            if( e.IsEnabled() && currentRecordingAndPlaybackMode == e.myRecordingType ) 
            { 
                currentlyUsedExamples.Add( e );
            }
        }
        if( currentlyUsedExamples.Count <= 0 )
        {
            haveTrained = false;
            return;
        }

        // compute boids features
        ComputeStaticBoidsFeatures();

        // train the base 
        if( predictionType == PredictionType.Classification )
        {
            myAnimationClassifier.ResetClassifier();
            for( int j = 0; j < currentlyUsedExamples.Count; j++ )
            {
                AnimationExample e = currentlyUsedExamples[j];
                if( !e.IsEnabled() )
                {
                    // skip it
                    continue;
                }
                List<ModelBaseDatum> phrase = e.baseExamples;
                for( int i = 0; i < phrase.Count; i++ )
                {
                    myAnimationClassifier.RecordDataPoint( BaseInput( phrase[i] ), BaseOutput( j ) );
                }
            }
            myAnimationClassifier.Train();
        }
        else
        {
            myAnimationRegression.ResetRegression();
            
            for( int j = 0; j < currentlyUsedExamples.Count; j++ )
            {
                AnimationExample e = currentlyUsedExamples[j];
                List<ModelBaseDatum> phrase = e.baseExamples;
                for( int i = 0; i < phrase.Count; i++ )
                {
                    myAnimationRegression.RecordDataPoint( BaseInput( phrase[i] ), LabelToRegressionOutput( j, currentlyUsedExamples.Count ) );
                }
            }
            myAnimationRegression.Train();
        }

        haveTrained = true;
    }

    Vector3 averageExamplePosition;
    private void ComputeStaticBoidsFeatures()
    {
        Vector3 sum = Vector3.zero;
        for( int i = 0; i < currentlyUsedExamples.Count; i++ ) { sum += currentlyUsedExamples[i].transform.position; }
        averageExamplePosition = sum / currentlyUsedExamples.Count;
    }

    private bool WillCollideWithTerrainSoon()
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // check if the model will hit anything in its forward direction
        return ( Physics.Raycast( modelBaseToAnimate.position, modelBaseToAnimate.forward, out hit, avoidTerrainDetection, layerMask ) );
    }

    private bool TooLowAboveTerrain()
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // check if the model will hit anything in its forward direction
        return ( Physics.Raycast( modelBaseToAnimate.position, Vector3.down, out hit, avoidTerrainMinheight, layerMask ) );
    }

    float goalSpeedMultiplier = 0, currentSpeedMultiplier = 0;
    float goalAvoidanceAngle = 0, currentAvoidanceAngle = 0;
    Vector3 ProcessBoidsGroundAvoidance()
    {
        // if the forward direction has land, avoid it
        if( WillCollideWithTerrainSoon() || TooLowAboveTerrain() )
        {
            goalAvoidanceAngle = avoidTerrainAngle;
        }
        else
        {
            goalAvoidanceAngle = 0;
        }
        currentAvoidanceAngle += boidSlew * ( goalAvoidanceAngle - currentAvoidanceAngle );
        
        return Mathf.Sin( (Mathf.PI / 2) * currentAvoidanceAngle / avoidTerrainAngle ) * Vector3.up;
    }

    Vector3 ProcessBoidsExamplesAttraction()
    {
        // if we are too far away from any of the examples, steer toward the middle of the examples
        float minDistance = float.MaxValue;
        for( int i = 0; i < currentlyUsedExamples.Count; i++ )
        {
            float d = ( modelBaseToAnimate.position - currentlyUsedExamples[i].transform.position ).magnitude;
            if( d < maxDistanceFromAnyExample )
            {
                // we don't have to do anything
                return Vector3.zero;
            }
            else if( d < minDistance )
            {
                minDistance = d;
            }
        }

        float severity = minDistance.PowMapClamp( maxDistanceFromAnyExample, maxDistanceFromAnyExample + distanceRampUpRange, 0, 1, rampUpSeverity );
        Vector3 correctionVelocity = ( averageExamplePosition - modelBaseToAnimate.position ).normalized;

        return severity * correctionVelocity;
    }

    List<Transform> nearOtherBoids = new List<Transform>();
    Vector3 ProcessBoidsOthersAvoidance()
    {
        // remove junk 
        nearOtherBoids.RemoveAll( other => other == null );

        // compute on what's left
        Vector3 desiredMovement = Vector3.zero;
        foreach( Transform other in nearOtherBoids )
        {
            Vector3 direction = transform.position - other.position;
            float intensity = direction.magnitude.PowMapClamp( 0, 2, 0.5f, 0, 0.6f );
            desiredMovement += intensity * direction.normalized;
        }
        return desiredMovement;
    }

    void OnTriggerEnter( Collider other )
    {
        if( other.gameObject.CompareTag( "BoidAvoidance" ) ) 
        {
            nearOtherBoids.Add( other.transform );
        }
    }

    void OnTriggerExit( Collider other )
    {
        if( other.gameObject.CompareTag( "BoidAvoidance" ) )
        {
            nearOtherBoids.Remove( other.transform );
        }
    }

    private double[] LabelToRegressionOutput( int label, int maxLabel )
    {
        double[] ret = new double[maxLabel];
        ret[label] = 1;
        return ret;
    }

    private Vector3 AngleMinify( Vector3 i )
    {
        // x
        if( i.x < -180 )
        {
            i.x += 360;
        }
        if( i.x > 180 )
        {
            i.x -= 360;
        }

        // y
        if( i.y < -180 )
        {
            i.y += 360;
        }
        if( i.y > 180 )
        {
            i.y -= 360;
        }

        // z
        if( i.z < -180 )
        {
            i.z += 360;
        }
        if( i.z > 180 )
        {
            i.z -= 360;
        }

        i.x = Mathf.Clamp( i.x, -maxEulerAnglesChange.x, maxEulerAnglesChange.x );
        i.y = Mathf.Clamp( i.y, -maxEulerAnglesChange.y, maxEulerAnglesChange.y );
        i.z = Mathf.Clamp( i.z, -maxEulerAnglesChange.z, maxEulerAnglesChange.z );

        return i;
    }

    double[] SoundInput( Quaternion baseRotation, float terrainHeight, float terrainSteepness, float heightAboveTerrain )
    {
        return new double[] {
            baseRotation.x, baseRotation.y, baseRotation.z, baseRotation.w,
            terrainHeight, terrainSteepness, heightAboveTerrain
        };
    }

    void GripPlaceDeleteInteractable.JustPlaced()
    {
        // do nothing
    }

    void GripPlaceDeleteInteractable.AboutToBeDeleted()
    {
        // stop tracking
        allCreatures.Remove( this );
        myGroup.Remove( this );

        // delete my animation points, which are now outside my transform
        Destroy( modelBaseToAnimate.gameObject );
        for( int i = 0; i < modelRelativePointsToAnimate.Length; i++ )
        {
            Destroy( modelRelativePointsToAnimate[i].gameObject );
        }

        if( myGroup.Count == 0 )
        {
            // delete all my examples, only if I'm the last one left
            for( int i = 0; i < examples.Count; i++ )
            {
                Destroy( examples[i].gameObject );
            }
        }
        else
        {
            // make sure all my examples have an animator,
            // in case their example was me
            for( int i = 0; i < examples.Count; i++ )
            {
                examples[i].ResetAnimator( myGroup[0] );
            } 
        }
    }

    public void HideExamples()
    {
        foreach( AnimationExample e in examples )
        {
            e.gameObject.SetActive( false );
        }
    }

    public void ShowExamples()
    {
        foreach( AnimationExample e in examples )
        {
            e.gameObject.SetActive( true );
        }
    }

    public class ModelBaseDatum
    {
        public Quaternion rotation;
        public float terrainHeight, terrainSteepness;

        public ModelBaseDatum Clone()
        {
            ModelBaseDatum c = new ModelBaseDatum();
            c.rotation = rotation;
            c.terrainHeight = terrainHeight;
            c.terrainSteepness = terrainSteepness;
            return c;
        }
    }

    public class ModelRelativeDatum
    {
        public Vector3 positionRelativeToBase;

        public ModelRelativeDatum Clone()
        {
            ModelRelativeDatum c = new ModelRelativeDatum();
            c.positionRelativeToBase = positionRelativeToBase;
            return c;
        }
    }
}
