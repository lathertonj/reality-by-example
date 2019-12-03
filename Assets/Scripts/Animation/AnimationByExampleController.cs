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


    // predict the position of each modelRelativePoint, relative to modelBase
    // input features: y, steepness of terrain, quaternion of current rotation
    private List<ModelRelativeDatum>[] modelRelativePositionData;

    // and, have a maximum amount that each thing can actually move -- maybe a slew per frame.
    public float globalSlew = 0.1f;

    // and, specify a data collection rate and a prediction output rate.
    public float dataCollectionRate = 0.1f, predictionOutputRate = 0.1f;


    void Awake()
    {
        modelBasePositionData = new List<ModelBaseDatum>();
        modelRelativePositionData = new List<ModelRelativeDatum>[modelRelativePoints.Length];
        // not sure if necessary
        for( int i = 0; i < modelRelativePoints.Length; i++ )
        {
            modelRelativePositionData[i] = new List<ModelRelativeDatum>();
        }
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

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


    private IEnumerator CollectData( bool clearFirst = false )
    {
        Vector3 prevPosition = modelBase.position;
        Quaternion prevRotation = modelBase.rotation;
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
        }
    }

    private class ModelBaseDatum
    {
        public Vector3 positionDelta;
        public Quaternion rotationDelta;
        public float terrainHeight, terrainSteepness;
    }

    private class ModelRelativeDatum
    {
        public Vector3 positionRelativeToBase;
        public Quaternion baseRotation;
        public float terrainHeight, terrainSteepness;
    }
}


