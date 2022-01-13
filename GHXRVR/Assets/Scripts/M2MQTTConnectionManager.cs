using M2MqttUnity;
using System;
using System.Collections;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine.Serialization;

public class M2MQTTConnectionManager : M2MqttUnityClient
{
    [Header("GHXRTable-specific configuration")]
    [Tooltip("Topic from which meshes are to be received.")]
    public string MeshesTopic = "ghxr/geometry/meshes";
    [Tooltip("Topic from which the position of the geometry is to be received.")]
    public string GeometryPositionsTopic = "ghxr/geometry/positions/share";
    [Tooltip("Topic from which the user position can be set externally.")]
    public string UserPositionTopic = "ghxr/coordinates";
    
    public delegate void MeshesUpdate(string json);
    public static event MeshesUpdate OnMeshesUpdateReceived;
    
    public delegate void GeometryPositionsUpdate(string json);
    public static event GeometryPositionsUpdate OnGeometryPositionsUpdateReceived;

    public delegate void UserPositionUpdate(string json);
    public static event UserPositionUpdate OnUserPositionUpdateReceived;

    private string clientId;

    //mostly copies the method from the parent M2MqttUnityClient class, with slight modifications to the MqttClient instantiation
    protected override IEnumerator DoConnect()
    {
        // wait for the given delay
        yield return new WaitForSecondsRealtime(connectionDelay / 1000f);
        // leave some time to Unity to refresh the UI
        yield return new WaitForEndOfFrame();

        // create client instance 
        if (client == null)
        {
            try
            {
#if (!UNITY_EDITOR && UNITY_WSA_10_0 && !ENABLE_IL2CPP)
                    client = new MqttClient(brokerAddress,brokerPort,isEncrypted, isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
#else
                client = new MqttClient(brokerAddress, brokerPort, isEncrypted, null, null, isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
                //System.Security.Cryptography.X509Certificates.X509Certificate cert = new System.Security.Cryptography.X509Certificates.X509Certificate();
                //client = new MqttClient(brokerAddress, brokerPort, isEncrypted, cert, null, MqttSslProtocols.TLSv1_0, MyRemoteCertificateValidationCallback);
#endif
            }
            catch (Exception e)
            {
                client = null;
                Debug.LogErrorFormat("CONNECTION FAILED! {0}", e.ToString());
                OnConnectionFailed(e.Message);
                yield break;
            }
        }
        else if (client.IsConnected)
        {
            yield break;
        }
        OnConnecting();

        // leave some time to Unity to refresh the UI
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        client.Settings.TimeoutOnConnection = timeoutOnConnection;
        clientId = Guid.NewGuid().ToString();
        try
        {
            client.Connect(clientId, mqttUserName, mqttPassword, false, 
                MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true, 
                "ghxr/status/VR-" + clientId, "offline", true, 60);
        }
        catch (Exception e)
        {
            client = null;
            Debug.LogErrorFormat("Failed to connect to {0}:{1}:\n{2}", brokerAddress, brokerPort, e.ToString());
            OnConnectionFailed(e.Message);
            yield break;
        }
        if (client.IsConnected)
        {
            client.ConnectionClosed += OnMqttConnectionClosed;
            // register to message received 
            client.MqttMsgPublishReceived += OnMqttMessageReceived;
            mqttClientConnected = true;
            OnConnected();
        }
        else
        {
            OnConnectionFailed("CONNECTION FAILED!");
        }
    }

    protected override void Start()
    {
        base.Start(); //this will auto connect if the corresponding boolean is set to true in the inspector 

        //TODO: see below (remove when no longer necessary)
        //Debug.LogWarning("Currently loading a basic rectangular box manually, following line should be removed once we can actually connect to a MQTT broker.");
        //OnMeshesUpdateReceived("[{\"Vertices\":[{\"X\":0.0,\"Y\":0.0,\"Z\":0.0},{\"X\":0.0,\"Y\":0.0,\"Z\":-4.0},{\"X\":0.0,\"Y\":4.0,\"Z\":0.0},{\"X\":0.0,\"Y\":4.0,\"Z\":-4.0},{\"X\":0.0,\"Y\":4.0,\"Z\":0.0},{\"X\":0.0,\"Y\":4.0,\"Z\":-4.0},{\"X\":8.0,\"Y\":4.0,\"Z\":0.0},{\"X\":8.0,\"Y\":4.0,\"Z\":-4.0},{\"X\":8.0,\"Y\":4.0,\"Z\":0.0},{\"X\":8.0,\"Y\":4.0,\"Z\":-4.0},{\"X\":8.0,\"Y\":0.0,\"Z\":0.0},{\"X\":8.0,\"Y\":0.0,\"Z\":-4.0},{\"X\":8.0,\"Y\":0.0,\"Z\":0.0},{\"X\":8.0,\"Y\":0.0,\"Z\":-4.0},{\"X\":0.0,\"Y\":0.0,\"Z\":0.0},{\"X\":0.0,\"Y\":0.0,\"Z\":-4.0},{\"X\":0.0,\"Y\":0.0,\"Z\":0.0},{\"X\":0.0,\"Y\":4.0,\"Z\":0.0},{\"X\":8.0,\"Y\":0.0,\"Z\":0.0},{\"X\":8.0,\"Y\":4.0,\"Z\":0.0},{\"X\":0.0,\"Y\":0.0,\"Z\":-4.0},{\"X\":8.0,\"Y\":0.0,\"Z\":-4.0},{\"X\":0.0,\"Y\":4.0,\"Z\":-4.0},{\"X\":8.0,\"Y\":4.0,\"Z\":-4.0}],\"Uvs\":[{\"X\":0.75,\"Y\":0.0},{\"X\":0.75,\"Y\":0.248535156},{\"X\":0.998535156,\"Y\":0.0},{\"X\":0.998535156,\"Y\":0.248535156},{\"X\":0.5,\"Y\":0.5},{\"X\":0.5,\"Y\":0.748535156},{\"X\":0.998535156,\"Y\":0.5},{\"X\":0.998535156,\"Y\":0.748535156},{\"X\":0.5,\"Y\":0.0},{\"X\":0.5,\"Y\":0.248535156},{\"X\":0.748535156,\"Y\":0.0},{\"X\":0.748535156,\"Y\":0.248535156},{\"X\":0.5,\"Y\":0.25},{\"X\":0.5,\"Y\":0.498535156},{\"X\":0.998535156,\"Y\":0.25},{\"X\":0.998535156,\"Y\":0.498535156},{\"X\":0.498535156,\"Y\":0.0},{\"X\":0.25,\"Y\":0.0},{\"X\":0.498535156,\"Y\":0.5},{\"X\":0.25,\"Y\":0.5},{\"X\":0.0,\"Y\":0.0},{\"X\":0.0,\"Y\":0.5},{\"X\":0.248535156,\"Y\":0.0},{\"X\":0.248535156,\"Y\":0.5}],\"Normals\":[{\"X\":-1.0,\"Y\":0.0,\"Z\":0.0},{\"X\":-1.0,\"Y\":0.0,\"Z\":0.0},{\"X\":-1.0,\"Y\":0.0,\"Z\":0.0},{\"X\":-1.0,\"Y\":0.0,\"Z\":0.0},{\"X\":0.0,\"Y\":1.0,\"Z\":0.0},{\"X\":0.0,\"Y\":1.0,\"Z\":0.0},{\"X\":0.0,\"Y\":1.0,\"Z\":0.0},{\"X\":0.0,\"Y\":1.0,\"Z\":0.0},{\"X\":1.0,\"Y\":0.0,\"Z\":0.0},{\"X\":1.0,\"Y\":0.0,\"Z\":0.0},{\"X\":1.0,\"Y\":0.0,\"Z\":0.0},{\"X\":1.0,\"Y\":0.0,\"Z\":0.0},{\"X\":0.0,\"Y\":-1.0,\"Z\":0.0},{\"X\":0.0,\"Y\":-1.0,\"Z\":0.0},{\"X\":0.0,\"Y\":-1.0,\"Z\":0.0},{\"X\":0.0,\"Y\":-1.0,\"Z\":0.0},{\"X\":0.0,\"Y\":0.0,\"Z\":1.0},{\"X\":0.0,\"Y\":0.0,\"Z\":1.0},{\"X\":0.0,\"Y\":0.0,\"Z\":1.0},{\"X\":0.0,\"Y\":0.0,\"Z\":1.0},{\"X\":0.0,\"Y\":0.0,\"Z\":-1.0},{\"X\":0.0,\"Y\":0.0,\"Z\":-1.0},{\"X\":0.0,\"Y\":0.0,\"Z\":-1.0},{\"X\":0.0,\"Y\":0.0,\"Z\":-1.0}],\"Faces\":[{\"IsQuad\":true,\"A\":1,\"B\":0,\"C\":2,\"D\":3},{\"IsQuad\":true,\"A\":5,\"B\":4,\"C\":6,\"D\":7},{\"IsQuad\":true,\"A\":9,\"B\":8,\"C\":10,\"D\":11},{\"IsQuad\":true,\"A\":13,\"B\":12,\"C\":14,\"D\":15},{\"IsQuad\":true,\"A\":17,\"B\":16,\"C\":18,\"D\":19},{\"IsQuad\":true,\"A\":21,\"B\":20,\"C\":22,\"D\":23}]}]");
    }

    protected override void DecodeMessage(string topic, byte[] bytes)
    {
        base.DecodeMessage(topic, bytes);
        string message = System.Text.Encoding.UTF8.GetString(bytes);

        if (MeshesTopic.Equals(topic))
        {
            if (OnMeshesUpdateReceived == null)
                Debug.LogError("No one subscribed to the meshes update events but we just got an update.");
            else
                OnMeshesUpdateReceived(message);
        }
        else if (GeometryPositionsTopic.Equals(topic))
        {
            if (OnGeometryPositionsUpdateReceived == null)
                Debug.LogError("No one subscribed to the geometry positions update events but we just got an update.");
            else
                OnGeometryPositionsUpdateReceived(message);
        }
        else if (UserPositionTopic.Equals(topic))
        {
            if (OnUserPositionUpdateReceived == null)
                Debug.LogError("No one subscribed to the user position update events but we just got an update.");
            else
                OnUserPositionUpdateReceived(message);
        }
    }

    protected override void OnConnected()
    {
        base.OnConnected();

        client.Publish("ghxr/status/VR-" + clientId, System.Text.Encoding.UTF8.GetBytes("online"));
    }

    protected override void OnConnecting()
    {
        base.OnConnecting();
    }

    protected override void OnConnectionFailed(string errorMessage)
    {
        base.OnConnectionFailed(errorMessage);
    }

    protected override void OnConnectionLost()
    {
        base.OnConnectionLost();
    }

    protected override void OnDisconnected()
    {
        base.OnDisconnected();
    }

    protected override void SubscribeTopics()
    {
        client.Subscribe(new string[] { MeshesTopic, GeometryPositionsTopic, UserPositionTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
    }

    protected override void UnsubscribeTopics()
    {
        client.Unsubscribe(new string[] { MeshesTopic, GeometryPositionsTopic, UserPositionTopic });
    }
    
}
