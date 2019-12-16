using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class AnimationByExampleController : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean startStopDataCollection;

    public Transform modelBaseDataSource, modelBaseToAnimate;
    public Transform[] modelRelativePointsDataSource, modelRelativePointsToAnimate;

    // Basic idea:
    // predict the increment to modelBase location and rotation
    // input features: y, steepness of terrain; previous location / rotation.
    // --> 2 regressions for modelBase
    private List<ModelBaseDatum> modelBasePositionData;
    private RapidMixRegression modelBaseRegression;


    // predict the position of each modelRelativePoint, relative to modelBase
    // input features: y, steepness of terrain, quaternion of current rotation
    private List<ModelRelativeDatum>[] modelRelativePositionData;
    private RapidMixRegression[] modelRelativeRegressions;

    // and, have a maximum amount that each thing can actually move -- maybe a slew per frame.
    public float globalSlew = 0.1f;

    // and, specify a data collection rate and a prediction output rate.
    public float dataCollectionRate = 0.1f, predictionOutputRate = 0.1f;

    public Vector3 maxEulerAnglesChange;

    // other stuff
    bool haveTrained = false;
    bool runtimeMode = false;

    IEnumerator dataCollectionCoroutine = null, runtimeCoroutine = null;


    void Awake()
    {
        modelBasePositionData = new List<ModelBaseDatum>();
        modelBaseRegression = gameObject.AddComponent<RapidMixRegression>();
        modelRelativePositionData = new List<ModelRelativeDatum>[modelRelativePointsDataSource.Length];
        modelRelativeRegressions = new RapidMixRegression[modelRelativePointsDataSource.Length];
        goalRelativePositions = new Vector3[modelRelativePointsDataSource.Length];

        for( int i = 0; i < modelRelativePointsDataSource.Length; i++ )
        {
            modelRelativePositionData[i] = new List<ModelRelativeDatum>();
            modelRelativeRegressions[i] = gameObject.AddComponent<RapidMixRegression>();
        }

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
            modelBaseToAnimate.position += globalSlew * ( goalBasePosition - modelBaseToAnimate.position );
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
            dataCollectionCoroutine = CollectData();
            StartCoroutine( dataCollectionCoroutine );
        }
        else if( startStopDataCollection.GetStateUp( handType ) )
        {
            // stop data collection if it's going
            if( dataCollectionCoroutine != null )
            {
                StopCoroutine( dataCollectionCoroutine );
                dataCollectionCoroutine = null;
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
    Vector3 goalBasePosition;
    Quaternion goalBaseRotation;

    Vector3[] goalRelativePositions;

    private IEnumerator Run()
    {
        // initialize with current values
        Vector3 prevPosition = modelBaseToAnimate.position;
        Quaternion prevRotation = modelBaseToAnimate.rotation;
        Vector3[] prevRelativePositions = new Vector3[goalRelativePositions.Length];
        for( int i = 0; i < prevRelativePositions.Length; i++ )
        {
            prevRelativePositions[i] = modelRelativePointsToAnimate[i].position - prevPosition;
        }

        // collect data and predict
        while( haveTrained )
        {
            Vector3 prevPositionDelta = modelBaseToAnimate.position - prevPosition;
            Quaternion prevRotationDelta = modelBaseToAnimate.rotation * Quaternion.Inverse( prevRotation );

            double[] o = modelBaseRegression.Run( FindBaseInput( modelBaseToAnimate.position, prevPositionDelta, prevRotationDelta ) );
            Vector3 nextGoalMovement = BaseOutputToPositionDelta( o );
            Quaternion nextGoalRotation = BaseOutputToRotationDelta( o );
            Debug.Log( "PREDICT: " + nextGoalRotation.eulerAngles.ToString() );

            goalBasePosition = modelBaseToAnimate.position + nextGoalMovement;
            goalBaseRotation = nextGoalRotation * modelBaseToAnimate.rotation;

            prevPosition = modelBaseToAnimate.position;
            prevRotation = modelBaseToAnimate.rotation;

            // compute the relative position goals
            for( int i = 0; i < goalRelativePositions.Length; i++ )
            {
                double[] output = modelRelativeRegressions[i].Run( FindRelativeInput( goalBaseRotation ) );
                goalRelativePositions[i] = RelativeOutputToOffset( output );
            }


            yield return new WaitForSecondsRealtime( predictionOutputRate );
        }
    }


    private IEnumerator CollectData( bool clearFirst = false )
    {
        Vector3 prevPosition = modelBaseDataSource.position;
        Vector3 prevRotation = modelBaseDataSource.rotation.eulerAngles;
        Vector3 prevPositionDelta = Vector3.zero;
        Vector3 prevRotationDelta = Quaternion.identity.eulerAngles;

        if( clearFirst )
        {
            modelBasePositionData.Clear();
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

            // base datum
            ModelBaseDatum newDatum = new ModelBaseDatum();
            newDatum.positionDelta = modelBaseDataSource.position - prevPosition;
            newDatum.rotationDelta = AngleMinify( modelBaseDataSource.rotation.eulerAngles - prevRotation );
            Debug.Log( "DATA: " + newDatum.rotationDelta.ToString() );
            newDatum.prevPositionDelta = prevPositionDelta;
            newDatum.prevRotationDelta = prevRotationDelta;
            newDatum.terrainHeight = currentHeight;
            newDatum.terrainSteepness = currentSteepness;

            modelBasePositionData.Add( newDatum );

            // other data
            for( int i = 0; i < modelRelativePointsDataSource.Length; i++ )
            {
                ModelRelativeDatum newRelativeDatum = new ModelRelativeDatum();
                newRelativeDatum.positionRelativeToBase = modelRelativePointsDataSource[i].position - modelBaseDataSource.position;
                newRelativeDatum.baseRotation = modelBaseDataSource.rotation;
                newRelativeDatum.terrainHeight = currentHeight;
                newRelativeDatum.terrainSteepness = currentSteepness;

                modelRelativePositionData[i].Add( newRelativeDatum );
            }

            prevPosition = modelBaseDataSource.position;
            prevRotation = modelBaseDataSource.rotation.eulerAngles;
            prevPositionDelta = newDatum.positionDelta;
            prevRotationDelta = newDatum.rotationDelta;
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
            //d.prevPositionDelta.x, d.prevPositionDelta.y, d.prevPositionDelta.z,
            d.prevRotationDelta.x, d.prevRotationDelta.y, d.prevRotationDelta.z
        };
    }

    ModelBaseDatum _dummy = new ModelBaseDatum();
    double[] FindBaseInput( Vector3 worldPos, Vector3 prevPositionDelta, Quaternion prevRotationDelta )
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

        _dummy.prevPositionDelta = prevPositionDelta;
        _dummy.prevRotationDelta = prevRotationDelta.eulerAngles;
        return BaseInput( _dummy );
    }

    double[] BaseOutput( ModelBaseDatum d )
    {
        return new double[] {
            d.positionDelta.x, d.positionDelta.y, d.positionDelta.z,
            d.rotationDelta.x, d.rotationDelta.y, d.rotationDelta.z
        };
    }

    Vector3 BaseOutputToPositionDelta( double[] o )
    {
        return new Vector3( (float) o[0], (float) o[1], (float) o[2] );
    }

    Quaternion BaseOutputToRotationDelta( double[] o )
    {
        return Quaternion.Euler( (float) o[3], (float) o[4], (float) o[5] );
    }

    // predict the position of each modelRelativePoint, relative to modelBase
    // input features: y, steepness of terrain, quaternion of current rotation
    double[] RelativeInput( ModelRelativeDatum d )
    {
        return new double[] {
            d.terrainHeight,
            d.terrainSteepness,
            d.baseRotation.w, d.baseRotation.x, d.baseRotation.y, d.baseRotation.z
        };
    }

    ModelRelativeDatum _dummy2 = new ModelRelativeDatum();
    double[] FindRelativeInput( Quaternion baseRotation )
    {
        _dummy2.baseRotation = baseRotation;
        _dummy2.terrainHeight = 0;
        _dummy2.terrainSteepness = 0;
        Terrain currentTerrain = FindTerrain();
        if( currentTerrain )
        {
            Vector2 terrainCoords = CoordinatesToIndices( currentTerrain, modelBaseToAnimate.position );
            _dummy2.terrainHeight = currentTerrain.terrainData.GetInterpolatedHeight( terrainCoords.x, terrainCoords.y );
            _dummy2.terrainSteepness = currentTerrain.terrainData.GetSteepness( terrainCoords.x, terrainCoords.y );
        }

        return RelativeInput( _dummy2 );
    }

    double[] RelativeOutput( ModelRelativeDatum d )
    {
        return new double[] {
            d.positionRelativeToBase.x, d.positionRelativeToBase.y, d.positionRelativeToBase.z
        };
    }

    Vector3 RelativeOutputToOffset( double[] o )
    {
        return new Vector3( (float) o[0], (float) o[1], (float) o[2] );
    }

    void Train()
    {
        if( modelBasePositionData.Count <= 0 )
        {
            haveTrained = false;
            return;
        }
        
        // train the base regression
        modelBaseRegression.ResetRegression();
        foreach( ModelBaseDatum d in modelBasePositionData )
        {
            modelBaseRegression.RecordDataPoint( BaseInput( d ), BaseOutput( d ) );
        }
        modelBaseRegression.Train();
    

        // train each of the relative regressions
        for( int i = 0; i < modelRelativeRegressions.Length; i++ )
        {
            modelRelativeRegressions[i].ResetRegression();
            foreach( ModelRelativeDatum d in modelRelativePositionData[i] )
            {
                modelRelativeRegressions[i].RecordDataPoint( RelativeInput( d ), RelativeOutput( d ) );
            }
            modelRelativeRegressions[i].Train();
        }
        
        haveTrained = true;
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

    private class ModelBaseDatum
    {
        public Vector3 positionDelta, prevPositionDelta;
        public Vector3 rotationDelta, prevRotationDelta;
        public float terrainHeight, terrainSteepness;
    }

    private class ModelRelativeDatum
    {
        public Vector3 positionRelativeToBase;
        public Quaternion baseRotation;
        public float terrainHeight, terrainSteepness;
    }
}


