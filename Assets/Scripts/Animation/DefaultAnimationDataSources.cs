using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultAnimationDataSources : MonoBehaviour
{
    public Transform baseDataSource;
    public Transform[] relativePointsDataSources;

    public static Transform theBaseDataSource;
    public static Transform[] theRelativePointsDataSources;


    // Start is called before the first frame update
    void Awake()
    {
        theBaseDataSource = baseDataSource;
        theRelativePointsDataSources = relativePointsDataSources;
    }
}
