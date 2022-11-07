using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultAnimationDataSources : MonoBehaviour
{
    public Transform baseDataSource;
    public Transform[] relativePointsDataSources;

    public static Transform theBaseDataSource;
    public static Transform[] theRelativePointsDataSources;

    private static Transform yRotationBaseDataSource;


    // Start is called before the first frame update
    void Awake()
    {
        theBaseDataSource = baseDataSource;
        theRelativePointsDataSources = relativePointsDataSources;
        GameObject _yRotation = new GameObject();
        _yRotation.name = "y rotation only version of base data source";
        yRotationBaseDataSource = _yRotation.transform;
    }
    
    public static Transform BaseDataSourceYTransformOnly()
    {
        yRotationBaseDataSource.position = theBaseDataSource.position;
        yRotationBaseDataSource.rotation = Quaternion.AngleAxis( theBaseDataSource.eulerAngles.y, Vector3.up );
        yRotationBaseDataSource.localScale = theBaseDataSource.localScale;
        return yRotationBaseDataSource;
    }
}
