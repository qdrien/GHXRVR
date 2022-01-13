using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class UserPositionHandler : MonoBehaviour
{
    void Awake()
    {
        M2MQTTConnectionManager.OnUserPositionUpdateReceived += UserPositionUpdateReceived;
    }

    private void OnDisable()
    {
        M2MQTTConnectionManager.OnUserPositionUpdateReceived -= UserPositionUpdateReceived;
    }

    private void UserPositionUpdateReceived(string json)
    {
        UserPosition userPosition = JsonConvert.DeserializeObject<UserPosition>(json);
        Debug.Log("Received user position/orientation.");
        
        //TODO: adapt to lat/lon/hdg (the current code seems to work fine, but in Unity's coordinate system)

        Player rig = Player.instance; // (="Player" object in Unity, contains the camera as a grandchild)

        GameObject playerCamera = rig.transform.Find("SteamVRObjects/VRCamera").gameObject;
        Vector3 cameraPosition = playerCamera.transform.position;
        Vector3 rigPosition = rig.transform.position;
        Vector3 positionOffset = new Vector3(cameraPosition.x - rigPosition.x, cameraPosition.y - rigPosition.y, cameraPosition.z - rigPosition.z);
        Vector3 newRigPosition = new Vector3(userPosition.X - positionOffset.x,
            userPosition.Y,
            userPosition.Z - positionOffset.z);
        rig.transform.position = newRigPosition;
        
        Vector3 cameraRotation = playerCamera.transform.rotation.eulerAngles;
        Vector3 rigRotation = rig.transform.rotation.eulerAngles;
        Vector3 rotationOffset = cameraRotation - rigRotation;
        rig.transform.rotation = Quaternion.Euler(0,userPosition.Angle - rotationOffset.y,0);
        
        //todo: some fading would be better (there is probably something in the steamvr plugin for that)
    }
}
