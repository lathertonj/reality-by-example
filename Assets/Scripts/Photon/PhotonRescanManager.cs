using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonRescanManager : MonoBehaviour
{
    private static PhotonRescanManager theManager;
    public float lazyRescanTime = 2f;
    public float checkFrequency = 0.25f;

    private Dictionary< IPhotonExampleRescanner, float > rescanTimes;
    private List< IPhotonExampleRescanner > _toRemove;

    void Awake()
    {
        theManager = this;
        rescanTimes = new Dictionary<IPhotonExampleRescanner, float>();
        _toRemove = new List<IPhotonExampleRescanner>();
    }

    void Start()
    {
        StartCoroutine( CheckRescans() );
    }

    IEnumerator CheckRescans()
    {
        while( true )
        {
            // rescan ones whose time has been reached
            foreach( KeyValuePair< IPhotonExampleRescanner, float > pair in rescanTimes )
            {
                if( pair.Value <= Time.time )
                {
                    // rescan
                    pair.Key.RescanProvidedExamples();
                    // wait for rescan to finish
                    for( int i = 0; i < pair.Key.NumFramesToRescan(); i++ ) { yield return null; }
                    // forget this later
                    _toRemove.Add( pair.Key );
                }
            }

            // forget the ones that rescanned
            foreach( IPhotonExampleRescanner r in _toRemove )
            {
                rescanTimes.Remove( r );
            }
            _toRemove.Clear();

            yield return new WaitForSecondsRealtime( checkFrequency );
        }
    }

    // assign the rescanner to be rescanned in the future,
    // overwriting any possible earlier time
    public static void LazyRescan( IPhotonExampleRescanner r )
    {
        theManager.rescanTimes[r] = Time.time + theManager.lazyRescanTime;
    }
}
