using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchToComponent : MonoBehaviour
{
    public enum InteractionType { PlaceTerrainImmediate, PlaceTerrainGrowth, PlaceTexture };
    public InteractionType switchTo;

    private void OnTriggerEnter( Collider other )
    {
        FlyingTeleporter maybeController = other.GetComponent<FlyingTeleporter>();
        if( maybeController )
        {
            DisableAllInteractors( maybeController.gameObject );
            switch( switchTo )
            {
                case InteractionType.PlaceTerrainImmediate:
                    maybeController.GetComponent<TerrainInteractor>().enabled = true;
                    maybeController.GetComponent<HeightExampleInteractor>().enabled = true;
                    break;
                case InteractionType.PlaceTerrainGrowth:
                    maybeController.GetComponent<TerrainGradualInteractor>().enabled = true;
                    maybeController.GetComponent<HeightExampleInteractor>().enabled = true;
                    break;
                case InteractionType.PlaceTexture:
                    maybeController.GetComponent<TerrainTextureInteractor>().enabled = true;
                    maybeController.GetComponent<TextureExampleInteractor>().enabled = true;
                    break;
                default:
                    break;
            }
        }
    }

    private void DisableAllInteractors( GameObject o )
    {
        o.GetComponent<TerrainInteractor>().enabled = false;
        o.GetComponent<TerrainGradualInteractor>().enabled = false;
        o.GetComponent<HeightExampleInteractor>().enabled = false;
        o.GetComponent<TerrainTextureInteractor>().enabled = false;
        o.GetComponent<TextureExampleInteractor>().enabled = false;
    }

    // stayCount allows the OnTriggerStay to be displayed less often
    // than it actually occurs.
    private float stayCount = 0.0f;
    private void OnTriggerStay( Collider other )
    {

    }

    private void OnTriggerExit( Collider other )
    {

    }
}
