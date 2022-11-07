using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleWaterFog : MonoBehaviour
{

    public int waterLayer = 11;
    public int groundLayer = 8;
    public float waterVisualOffset = 0.15f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // is this too often?
        // when underwater, turn on fog
        bool turnOnFog = UnderWater() && AboveLand();
        RenderSettings.fog = turnOnFog;
    }

    float largeDistance = 400;
    bool UnderWater()
    {
        // looking Vector3.up, do we see water?
        return TerrainUtility.BelowOneSidedLayer( transform.position - waterVisualOffset * Vector3.up, Vector3.up, largeDistance, waterLayer );
    }

    bool AboveLand()
    {
        // looking Vector3.down, do we see ground?
        return TerrainUtility.AboveLayer( transform.position, Vector3.down, largeDistance, groundLayer );
    }
}
