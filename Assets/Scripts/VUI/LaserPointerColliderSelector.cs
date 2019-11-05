﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LaserPointerColliderSelector : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean preview;
    public SteamVR_Action_Boolean stopShowingLaser;
    private SteamVR_Behaviour_Pose controllerPose;

    public GameObject laserPrefab;
    private GameObject laser;
    private Transform laserTransform;
    private Vector3 hitPoint;
    private bool currentlyIntersecting;

    // Start is called before the first frame update
    void Awake()
    {
        laser = Instantiate(laserPrefab);
        laserTransform = laser.transform;
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        HideLaser();
    }

    // Update is called once per frame
    void Update()
    {
        if( preview.GetState( handType ) && !stopShowingLaser.GetState( handType ) )
        {
            RaycastHit hit;
            // show laser
            if( Physics.Raycast( controllerPose.transform.position, transform.forward, out hit, 1000 ) )
            {
                hitPoint = hit.point;
                ShowLaser( hit );
            }
            else
            {
                HideLaser();
            }
        }
        else
        {
            HideLaser();
        }
    }

    public Vector3 GetMostRecentIntersectionPoint()
    {
        return hitPoint;
    }

    public bool IsIntersecting()
    {
        return currentlyIntersecting;
    }


    private void ShowLaser( RaycastHit hit )
    {
        laser.SetActive( true );
        laserTransform.position = Vector3.Lerp( controllerPose.transform.position, hitPoint, .5f );
        laserTransform.LookAt( hitPoint );
        laserTransform.localScale = new Vector3(
            laserTransform.localScale.x,
            laserTransform.localScale.y,
            hit.distance
        );
        currentlyIntersecting = true;
    }

    public void HideLaser()
    {
        laser.SetActive( false ); 
        currentlyIntersecting = false;
    }
}