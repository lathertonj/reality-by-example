using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class AnimationByRecordedExampleController : MonoBehaviour
{
    public enum PredictionType { Classification, Regression };
    public PredictionType predictionType;

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean startStopDataCollection;

    public Transform modelBaseDataSource, modelBaseToAnimate;
    public Transform[] modelRelativePointsDataSource, modelRelativePointsToAnimate;

    // Basic idea:
    // predict the increment to modelBase location and rotation
    // input features: y, steepness of terrain; previous location / rotation.
    // --> 2 regressions for modelBase
    private List<List<ModelBaseDatum>> modelBasePositionData;
    private RapidMixClassifier myAnimationClassifier;
    private RapidMixRegression myAnimationRegression;


    // predict the position of each modelRelativePoint, relative to modelBase
    // input features: y, steepness of terrain, quaternion of current rotation
    private List<List<ModelRelativeDatum>>[] modelRelativePositionData;

    // and, have a maximum amount that each thing can actually move -- maybe a slew per frame.
    public float globalSlew = 0.1f;

    // and, specify a data collection rate and a prediction output rate.
    public float dataCollectionRate = 0.1f;

    // TODO use AngleMinify function?
    public Vector3 maxEulerAnglesChange;

    // other stuff
    bool haveTrained = false;
    bool runtimeMode = false;

    IEnumerator dataCollectionCoroutine = null, runtimeCoroutine = null;

    private AnimationSoundRecorderPlaybackController mySounder = null;


    void Awake()
    {
        modelBasePositionData = new List<List<ModelBaseDatum>>();
        if( predictionType == PredictionType.Classification )
        {
            myAnimationClassifier = gameObject.AddComponent<RapidMixClassifier>();
        }
        else
        {
            myAnimationRegression = gameObject.AddComponent<RapidMixRegression>();
        }
        modelRelativePositionData = new List<List<ModelRelativeDatum>>[modelRelativePointsDataSource.Length];
        goalRelativePositions = new Vector3[modelRelativePointsDataSource.Length];

        for( int i = 0; i < modelRelativePointsDataSource.Length; i++ )
        {
            modelRelativePositionData[i] = new List<List<ModelRelativeDatum>>();
        }

        mySounder = GetComponent<AnimationSoundRecorderPlaybackController>();
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if( runtimeMode )
        {
            // slew base
            // TODO: move in the forward direction, perhaps with speed according to limb movement or delayed from it
            // modelBaseToAnimate.position += globalSlew * ( goalBasePosition - modelBaseToAnimate.position );
            modelBaseToAnimate.position += 0.05f * modelBaseToAnimate.forward;
            modelBaseToAnimate.rotation = Quaternion.Slerp( modelBaseToAnimate.rotation, goalBaseRotation, globalSlew );

            // slew the relative positions
            for( int i = 0; i < modelRelativePointsToAnimate.Length; i++ )
            {
                Vector3 currentDifference = modelRelativePointsToAnimate[i].position - modelBaseToAnimate.position;
                Vector3 nextDifference = currentDifference + globalSlew * ( goalRelativePositions[i] - currentDifference );
                modelRelativePointsToAnimate[i].position = modelBaseToAnimate.position + nextDifference;
            }
        }
        else
        {
            // animate it as just following the data sources
            modelBaseToAnimate.position = modelBaseDataSource.position;
            modelBaseToAnimate.rotation = modelBaseDataSource.rotation;

            for( int i = 0; i < modelRelativePointsToAnimate.Length; i++ )
            {
                modelRelativePointsToAnimate[i].position = modelRelativePointsDataSource[i].position;
            }
        }

        // grips == have to start / stop coroutines BOTH for data collection AND for running
        if( startStopDataCollection.GetStateDown( handType ) )
        {
            // stop runtime if it's running
            if( runtimeCoroutine != null )
            {
                StopCoroutine( runtimeCoroutine );
                runtimeCoroutine = null;
                runtimeMode = false;
            }

            // start data collection
            dataCollectionCoroutine = CollectPhrase();
            StartCoroutine( dataCollectionCoroutine );
        }
        else if( startStopDataCollection.GetStateUp( handType ) )
        {
            // stop data collection if it's going
            if( dataCollectionCoroutine != null )
            {
                // animation
                StopCoroutine( dataCollectionCoroutine );
                dataCollectionCoroutine = null;

                // sound
                if( mySounder )
                {
                    mySounder.StopRecordingExamples();
                }
            }


            // train
            Train();

            // start runtime
            runtimeCoroutine = Run();
            StartCoroutine( runtimeCoroutine );
            runtimeMode = true;
        }

    }

    private Terrain FindTerrain()
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( transform.position + 400 * Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
        {
            return hit.transform.GetComponentInParent<Terrain>();
        }
        return null;
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

    Vector3[] goalRelativePositions;

    private IEnumerator Run()
    {
        int currentFrame = 0;
        // collect data and predict
        if( predictionType == PredictionType.Classification )
        {
            while( haveTrained )
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
                currentFrame = currentFrame % modelBasePositionData[whichAnimation].Count;

                goalBaseRotation = modelBasePositionData[whichAnimation][currentFrame].rotation;
                Debug.Log( "PREDICT: " + goalBaseRotation.eulerAngles.ToString() );

                // compute the relative position goals
                for( int i = 0; i < goalRelativePositions.Length; i++ )
                {
                    goalRelativePositions[i] = modelRelativePositionData[i][whichAnimation][currentFrame].positionRelativeToBase;
                }

                // sound
                if( mySounder )
                {
                    // TODO do we want different features?
                    // mySounder.Predict( baseInput );
                    mySounder.Predict( SoundInput( modelBaseToAnimate.rotation, (float) baseInput[0], (float) baseInput[1] ) );
                }

                // since we are playing back recorded animations,
                // playback rate == collection rate
                yield return new WaitForSecondsRealtime( dataCollectionRate );

                currentFrame++;
            }
        }
        else
        {
            while( haveTrained )
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
                for( int i = 0; i < o.Length; i++ ) { sum += o[i]; }
                for( int i = 0; i < o.Length; i++ ) { o[i] /= sum; }

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
                    GetBaseQuaternion( mostProminent, currentFrame ),
                    GetBaseQuaternion( secondMostProminent, currentFrame ),
                    slerpAmount
                );
                // Debug.Log( "PREDICT: " + goalBaseRotation.eulerAngles.ToString() );



                // weighted average of vectors:
                // compute the relative position goals
                for( int i = 0; i < goalRelativePositions.Length; i++ )
                {
                    goalRelativePositions[i] = Vector3.zero;

                    for( int whichAnimation = 0; whichAnimation < modelRelativePositionData[i].Count; whichAnimation++ )
                    {
                        // weighted sum
                        goalRelativePositions[i] += (float) o[whichAnimation] * GetRelativePosition( i, whichAnimation, currentFrame );
                        
                    }
                }

                // sound
                if( mySounder )
                {
                    // mySounder.Predict( baseInput );
                    mySounder.Predict( SoundInput( modelBaseToAnimate.rotation, (float) baseInput[0], (float) baseInput[1] ) );
                }

                // since we are playing back recorded animations,
                // playback rate == collection rate
                yield return new WaitForSecondsRealtime( dataCollectionRate );

                currentFrame++;
            }
        }

    }

    private Quaternion GetBaseQuaternion( int whichAnimation, int currentFrame )
    {
        return modelBasePositionData[whichAnimation][currentFrame % modelBasePositionData[whichAnimation].Count].rotation;
    }

    private Vector3 GetRelativePosition( int i, int whichAnimation, int currentFrame )
    {
        return modelRelativePositionData[i][whichAnimation][currentFrame % modelRelativePositionData[i][whichAnimation].Count].positionRelativeToBase;
    }


    private int nextLabel = 0;
    private IEnumerator CollectPhrase()
    {
        int currentLabel = nextLabel;
        nextLabel++;
        float startTime = Time.time;
        Vector3 prevPosition = modelBaseDataSource.position;
        Vector3 prevRotation = modelBaseDataSource.rotation.eulerAngles;

        List<ModelBaseDatum> basePhrase = new List<ModelBaseDatum>();
        List<ModelRelativeDatum>[] relativePhrases = new List<ModelRelativeDatum>[modelRelativePointsDataSource.Length];
        modelBasePositionData.Add( basePhrase );
        for( int i = 0; i < relativePhrases.Length; i++ )
        {
            relativePhrases[i] = new List<ModelRelativeDatum>();
            modelRelativePositionData[i].Add( relativePhrases[i] );
        }

        // start sound
        if( mySounder )
        {
            mySounder.StartRecordingExamples();
        }

        while( true )
        {
            yield return new WaitForSecondsRealtime( dataCollectionRate );

            // fetch terrain values
            float currentHeight = 0, currentSteepness = 0;
            Terrain currentTerrain = FindTerrain();
            if( currentTerrain )
            {
                Vector2 terrainCoords = CoordinatesToIndices( currentTerrain, modelBaseDataSource.position );
                currentHeight = currentTerrain.terrainData.GetInterpolatedHeight( terrainCoords.x, terrainCoords.y );
                currentSteepness = currentTerrain.terrainData.GetSteepness( terrainCoords.x, terrainCoords.y );
            }

            float timeElapsed = Time.time - startTime;

            // base datum
            ModelBaseDatum newDatum = new ModelBaseDatum();
            // newDatum.positionDelta = modelBaseDataSource.position - prevPosition;
            // newDatum.rotationDelta = AngleMinify( modelBaseDataSource.rotation.eulerAngles - prevRotation );
            // Debug.Log( "DATA: " + newDatum.rotationDelta.ToString() );
            newDatum.rotation = modelBaseDataSource.rotation;
            // Debug.Log( "DATA: " + newDatum.rotation.eulerAngles.ToString() );
            newDatum.terrainHeight = currentHeight;
            newDatum.terrainSteepness = currentSteepness;
            newDatum.label = currentLabel;

            basePhrase.Add( newDatum );

            // sound
            if( mySounder )
            {
                mySounder.ProvideExample( SoundInput( newDatum.rotation, newDatum.terrainHeight, newDatum.terrainSteepness ) );
            }

            // other data
            for( int i = 0; i < modelRelativePointsDataSource.Length; i++ )
            {
                ModelRelativeDatum newRelativeDatum = new ModelRelativeDatum();
                newRelativeDatum.positionRelativeToBase = modelRelativePointsDataSource[i].position - modelBaseDataSource.position;

                relativePhrases[i].Add( newRelativeDatum );
            }

            prevPosition = modelBaseDataSource.position;
            prevRotation = modelBaseDataSource.rotation.eulerAngles;
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
        _dummy.terrainHeight = 0;
        _dummy.terrainSteepness = 0;
        Terrain currentTerrain = FindTerrain();
        if( currentTerrain )
        {
            Vector2 terrainCoords = CoordinatesToIndices( currentTerrain, modelBaseToAnimate.position );
            _dummy.terrainHeight = currentTerrain.terrainData.GetInterpolatedHeight( terrainCoords.x, terrainCoords.y );
            _dummy.terrainSteepness = currentTerrain.terrainData.GetSteepness( terrainCoords.x, terrainCoords.y );
        }

        return BaseInput( _dummy );
    }

    string BaseOutput( ModelBaseDatum d )
    {
        return d.label.ToString();
    }

    void Train()
    {
        if( modelBasePositionData.Count <= 0 )
        {
            haveTrained = false;
            return;
        }

        // train the base 
        if( predictionType == PredictionType.Classification )
        {
            myAnimationClassifier.ResetClassifier();
            foreach( List<ModelBaseDatum> phrase in modelBasePositionData )
            {
                for( int i = 0; i < phrase.Count; i++ )
                {
                    myAnimationClassifier.RecordDataPoint( BaseInput( phrase[i] ), BaseOutput( phrase[i] ) );
                }
            }
            myAnimationClassifier.Train();
        }
        else
        {
            myAnimationRegression.ResetRegression();
            int numRecordings = modelBasePositionData.Count;
            foreach( List<ModelBaseDatum> phrase in modelBasePositionData )
            {
                for( int i = 0; i < phrase.Count; i++ )
                {
                    myAnimationRegression.RecordDataPoint( BaseInput( phrase[i] ), LabelToRegressionOutput( phrase[i].label, numRecordings ) );
                }
            }
            myAnimationRegression.Train();
        }

        haveTrained = true;
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

    double[] SoundInput( Quaternion baseRotation, float terrainHeight, float terrainSteepness )
    {
        return new double[] {
            baseRotation.x, baseRotation.y, baseRotation.z, baseRotation.w,
            terrainHeight, terrainSteepness
        };
    }

    private class ModelBaseDatum
    {
        public Quaternion rotation;
        public float terrainHeight, terrainSteepness;
        public int label;
    }

    private class ModelRelativeDatum
    {
        public Vector3 positionRelativeToBase;
    }
}
