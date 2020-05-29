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

    bool UnderWater()
    {
        // problem: can't raycast upward to detect water
        // so instead, raycast down from above me and check if y value of hit point
        // is greater than mine
        Vector3 waterAboveOrBelow;
        if( TerrainUtility.AboveLayer( transform.position + 400 * Vector3.up, waterLayer, out waterAboveOrBelow ) )
        {
            return waterAboveOrBelow.y >= ( transform.position.y - waterVisualOffset );
        }
        return false;
    }

    bool AboveLand()
    {
        return TerrainUtility.AboveLayer( transform.position, groundLayer );
    }
}
