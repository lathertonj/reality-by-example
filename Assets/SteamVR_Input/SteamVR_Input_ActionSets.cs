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
        
        private static SteamVR_Input_ActionSet_rbe p_rbe;
        
        public static SteamVR_Input_ActionSet_rbe rbe
        {
            get
            {
                return SteamVR_Actions.p_rbe.GetCopy<SteamVR_Input_ActionSet_rbe>();
            }
        }
        
        private static void StartPreInitActionSets()
        {
            SteamVR_Actions.p_rbe = ((SteamVR_Input_ActionSet_rbe)(SteamVR_ActionSet.Create<SteamVR_Input_ActionSet_rbe>("/actions/rbe")));
            Valve.VR.SteamVR_Input.actionSets = new Valve.VR.SteamVR_ActionSet[] {
                    SteamVR_Actions.rbe};
        }
    }
}
