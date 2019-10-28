using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestClassifier : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        RapidMixClassifier myClassifier = GetComponent<RapidMixClassifier>();

        myClassifier.RecordDataPoint( new double[] {0.0}, "middle" );
        myClassifier.RecordDataPoint( new double[] {0.1}, "middle" );
        myClassifier.RecordDataPoint( new double[] {-0.1}, "middle" );
        myClassifier.RecordDataPoint( new double[] {1.1}, "right" );
        myClassifier.RecordDataPoint( new double[] {0.9}, "right" );
        myClassifier.RecordDataPoint( new double[] {1.0}, "right" );
        myClassifier.RecordDataPoint( new double[] {-1.0}, "left" );
        myClassifier.RecordDataPoint( new double[] {-1.1}, "left" );
        myClassifier.RecordDataPoint( new double[] {-0.9}, "left" );

        myClassifier.Train();

        Debug.Log( myClassifier.Run( new double[] {-1.0} ) );
        Debug.Log( myClassifier.Run( new double[] {0.0} ) );
        Debug.Log( myClassifier.Run( new double[] {1.0} ) );

        Debug.Log( myClassifier.Run( new double[] {0.3} ) );
        Debug.Log( myClassifier.Run( new double[] {-0.3} ) );
        Debug.Log( myClassifier.Run( new double[] {0.7} ) );
        Debug.Log( myClassifier.Run( new double[] {-0.7} ) );
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
