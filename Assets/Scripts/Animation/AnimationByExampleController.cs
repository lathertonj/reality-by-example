using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationByExampleController : MonoBehaviour
{

    public Transform modelBase;
    public Transform[] modelRelativePoints;

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

    // other stuff
    bool haveTrained = false;
    bool runtimeMode = false;


    void Awake()
    {
        modelBasePositionData = new List<ModelBaseDatum>();
        modelBaseRegression = gameObject.AddComponent<RapidMixRegression>();
        modelRelativePositionData = new List<ModelRelativeDatum>[modelRelativePoints.Length];
        modelRelativeRegressions = new RapidMixRegression[modelRelativePoints.Length];

        for( int i = 0; i < modelRelativePoints.Length; i++ )
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
            // slew
            modelBase.position += globalSlew * ( goalBasePosition - modelBase.position );
            modelBase.rotation = Quaternion.Slerp( modelBase.rotation, goalBaseRotation, globalSlew );

            // TODO: slew the relative positions
        }   
        else
        {

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

    private IEnumerator Run()
    {
        Vector3 prevPosition = modelBase.position;
        Quaternion prevRotation = modelBase.rotation;
        while( haveTrained )
        {
            Vector3 prevPositionDelta = modelBase.position - prevPosition;
            Quaternion prevRotationDelta = modelBase.rotation * Quaternion.Inverse( prevRotation );

            double[] o = modelBaseRegression.Run( FindBaseInput( modelBase.position, prevPositionDelta, prevRotationDelta ) );
            Vector3 nextGoalMovement = BaseOutputToPositionDelta( o );
            Quaternion nextGoalRotation = BaseOutputToRotationDelta( o );

            goalBasePosition = modelBase.position + nextGoalMovement;
            goalBaseRotation = nextGoalRotation * modelBase.rotation;

            prevPosition = modelBase.position;
            prevRotation = modelBase.rotation;

            // TODO: compute the relative position goals


            yield return new WaitForSecondsRealtime( predictionOutputRate );
        }
    }


    private IEnumerator CollectData( bool clearFirst = false )
    {
        Vector3 prevPosition = modelBase.position;
        Quaternion prevRotation = modelBase.rotation;
        Vector3 prevPositionDelta = Vector3.zero;
        Quaternion prevRotationDelta = Quaternion.identity;

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
                Vector2 terrainCoords = CoordinatesToIndices( currentTerrain, modelBase.position );
                currentHeight = currentTerrain.terrainData.GetInterpolatedHeight( terrainCoords.x, terrainCoords.y );
                currentSteepness = currentTerrain.terrainData.GetSteepness( terrainCoords.x, terrainCoords.y );
            }

            // base datum
            ModelBaseDatum newDatum = new ModelBaseDatum();
            newDatum.positionDelta = modelBase.position - prevPosition;
            newDatum.rotationDelta = modelBase.rotation * Quaternion.Inverse( prevRotation );
            newDatum.prevPositionDelta = prevPositionDelta;
            newDatum.prevRotationDelta = prevRotationDelta;
            newDatum.terrainHeight = currentHeight;
            newDatum.terrainSteepness = currentSteepness;

            modelBasePositionData.Add( newDatum );

            // other data
            for( int i = 0; i < modelRelativePoints.Length; i++ )
            {
                ModelRelativeDatum newRelativeDatum = new ModelRelativeDatum();
                newRelativeDatum.positionRelativeToBase = modelRelativePoints[i].position - modelBase.position;
                newRelativeDatum.baseRotation = modelBase.rotation;
                newRelativeDatum.terrainHeight = currentHeight;
                newRelativeDatum.terrainSteepness = currentSteepness;

                modelRelativePositionData[i].Add( newRelativeDatum );
            }

            prevPosition = modelBase.position;
            prevRotation = modelBase.rotation;
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
            d.prevPositionDelta.x, d.prevPositionDelta.y, d.prevPositionDelta.z,
            d.prevRotationDelta.w, d.prevRotationDelta.x, d.prevRotationDelta.y, d.prevRotationDelta.z
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
            Vector2 terrainCoords = CoordinatesToIndices( currentTerrain, modelBase.position );
            _dummy.terrainHeight = currentTerrain.terrainData.GetInterpolatedHeight( terrainCoords.x, terrainCoords.y );
            _dummy.terrainSteepness = currentTerrain.terrainData.GetSteepness( terrainCoords.x, terrainCoords.y );
        }

        _dummy.prevPositionDelta = prevPositionDelta;
        _dummy.prevRotationDelta = prevRotationDelta;
        return BaseInput( _dummy );
    }

    double[] BaseOutput( ModelBaseDatum d )
    {
        return new double[] {
            d.positionDelta.x, d.positionDelta.y, d.positionDelta.z,
            d.rotationDelta.w, d.rotationDelta.x, d.rotationDelta.y, d.rotationDelta.z
        };
    }

    Vector3 BaseOutputToPositionDelta( double[] o )
    {
        return new Vector3( (float) o[0], (float) o[1], (float) o[2] );
    }

    Quaternion BaseOutputToRotationDelta( double[] o )
    {
        return new Quaternion( (float) o[3], (float) o[4], (float) o[5], (float) o[6] );
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
        
        modelBaseRegression.ResetRegression();
        foreach( ModelBaseDatum d in modelBasePositionData )
        {
            modelBaseRegression.RecordDataPoint( BaseInput( d ), BaseOutput( d ) );
        }
        modelBaseRegression.Train();
    

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

    private class ModelBaseDatum
    {
        public Vector3 positionDelta, prevPositionDelta;
        public Quaternion rotationDelta, prevRotationDelta;
        public float terrainHeight, terrainSteepness;
    }

    private class ModelRelativeDatum
    {
        public Vector3 positionRelativeToBase;
        public Quaternion baseRotation;
        public float terrainHeight, terrainSteepness;
    }
}


