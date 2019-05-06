//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Valve.VR
{
    using System;
    using UnityEngine;
    
    
    public partial class SteamVR_Actions
    {
        
        private static SteamVR_Action_Boolean p_terraingenerate_TouchpadPressed;
        
        private static SteamVR_Action_Vector2 p_terraingenerate_Touchpad_XY;
        
        private static SteamVR_Action_Boolean p_terraingenerate_TriggerDown;
        
        private static SteamVR_Action_Pose p_terraingenerate_Pose;
        
        private static SteamVR_Action_Vibration p_terraingenerate_Haptic;
        
        public static SteamVR_Action_Boolean terraingenerate_TouchpadPressed
        {
            get
            {
                return SteamVR_Actions.p_terraingenerate_TouchpadPressed.GetCopy<SteamVR_Action_Boolean>();
            }
        }
        
        public static SteamVR_Action_Vector2 terraingenerate_Touchpad_XY
        {
            get
            {
                return SteamVR_Actions.p_terraingenerate_Touchpad_XY.GetCopy<SteamVR_Action_Vector2>();
            }
        }
        
        public static SteamVR_Action_Boolean terraingenerate_TriggerDown
        {
            get
            {
                return SteamVR_Actions.p_terraingenerate_TriggerDown.GetCopy<SteamVR_Action_Boolean>();
            }
        }
        
        public static SteamVR_Action_Pose terraingenerate_Pose
        {
            get
            {
                return SteamVR_Actions.p_terraingenerate_Pose.GetCopy<SteamVR_Action_Pose>();
            }
        }
        
        public static SteamVR_Action_Vibration terraingenerate_Haptic
        {
            get
            {
                return SteamVR_Actions.p_terraingenerate_Haptic.GetCopy<SteamVR_Action_Vibration>();
            }
        }
        
        private static void InitializeActionArrays()
        {
            Valve.VR.SteamVR_Input.actions = new Valve.VR.SteamVR_Action[] {
                    SteamVR_Actions.terraingenerate_TouchpadPressed,
                    SteamVR_Actions.terraingenerate_Touchpad_XY,
                    SteamVR_Actions.terraingenerate_TriggerDown,
                    SteamVR_Actions.terraingenerate_Pose,
                    SteamVR_Actions.terraingenerate_Haptic};
            Valve.VR.SteamVR_Input.actionsIn = new Valve.VR.ISteamVR_Action_In[] {
                    SteamVR_Actions.terraingenerate_TouchpadPressed,
                    SteamVR_Actions.terraingenerate_Touchpad_XY,
                    SteamVR_Actions.terraingenerate_TriggerDown,
                    SteamVR_Actions.terraingenerate_Pose};
            Valve.VR.SteamVR_Input.actionsOut = new Valve.VR.ISteamVR_Action_Out[] {
                    SteamVR_Actions.terraingenerate_Haptic};
            Valve.VR.SteamVR_Input.actionsVibration = new Valve.VR.SteamVR_Action_Vibration[] {
                    SteamVR_Actions.terraingenerate_Haptic};
            Valve.VR.SteamVR_Input.actionsPose = new Valve.VR.SteamVR_Action_Pose[] {
                    SteamVR_Actions.terraingenerate_Pose};
            Valve.VR.SteamVR_Input.actionsBoolean = new Valve.VR.SteamVR_Action_Boolean[] {
                    SteamVR_Actions.terraingenerate_TouchpadPressed,
                    SteamVR_Actions.terraingenerate_TriggerDown};
            Valve.VR.SteamVR_Input.actionsSingle = new Valve.VR.SteamVR_Action_Single[0];
            Valve.VR.SteamVR_Input.actionsVector2 = new Valve.VR.SteamVR_Action_Vector2[] {
                    SteamVR_Actions.terraingenerate_Touchpad_XY};
            Valve.VR.SteamVR_Input.actionsVector3 = new Valve.VR.SteamVR_Action_Vector3[0];
            Valve.VR.SteamVR_Input.actionsSkeleton = new Valve.VR.SteamVR_Action_Skeleton[0];
            Valve.VR.SteamVR_Input.actionsNonPoseNonSkeletonIn = new Valve.VR.ISteamVR_Action_In[] {
                    SteamVR_Actions.terraingenerate_TouchpadPressed,
                    SteamVR_Actions.terraingenerate_Touchpad_XY,
                    SteamVR_Actions.terraingenerate_TriggerDown};
        }
        
        private static void PreInitActions()
        {
            SteamVR_Actions.p_terraingenerate_TouchpadPressed = ((SteamVR_Action_Boolean)(SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/terraingenerate/in/TouchpadPressed")));
            SteamVR_Actions.p_terraingenerate_Touchpad_XY = ((SteamVR_Action_Vector2)(SteamVR_Action.Create<SteamVR_Action_Vector2>("/actions/terraingenerate/in/Touchpad_XY")));
            SteamVR_Actions.p_terraingenerate_TriggerDown = ((SteamVR_Action_Boolean)(SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/terraingenerate/in/TriggerDown")));
            SteamVR_Actions.p_terraingenerate_Pose = ((SteamVR_Action_Pose)(SteamVR_Action.Create<SteamVR_Action_Pose>("/actions/terraingenerate/in/Pose")));
            SteamVR_Actions.p_terraingenerate_Haptic = ((SteamVR_Action_Vibration)(SteamVR_Action.Create<SteamVR_Action_Vibration>("/actions/terraingenerate/out/Haptic")));
        }
    }
}
