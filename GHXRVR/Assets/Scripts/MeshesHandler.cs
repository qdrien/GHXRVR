using Newtonsoft.Json;
using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class MeshesHandler : MonoBehaviour
{
    public GameObject MeshPrefab;
    
    private List<GameObject> _meshObjects;
    private Vector2 _gpsOrigin = Vector2.zero;
    private List<PositionData> _lastGpsPositions;

    void Awake()
    {
        _meshObjects = new List<GameObject>();
        M2MQTTConnectionManager.OnMeshesUpdateReceived += MeshesUpdateReceived;
        M2MQTTConnectionManager.OnGeometryPositionsUpdateReceived += GeometryPositionsReceived;
    }

    private void OnDisable()
    {
        M2MQTTConnectionManager.OnMeshesUpdateReceived -= MeshesUpdateReceived;
        M2MQTTConnectionManager.OnGeometryPositionsUpdateReceived -= GeometryPositionsReceived;
    }

    private void GeometryPositionsReceived(string json)
    {
        List<PositionData> gpsPositions = JsonConvert.DeserializeObject<List<PositionData>>(json);

        if (_gpsOrigin.Equals(Vector2.zero))
        {
            _gpsOrigin.Set(gpsPositions[0].lat, gpsPositions[0].lon);
        }

        _lastGpsPositions = gpsPositions;
        
        UpdateMeshesPosition();
    }

    private void MeshesUpdateReceived(string json)
    {
        List<Mesh> meshes = JsonConvert.DeserializeObject<List<Mesh>>(json);
        Debug.Log("Received " + meshes.Count + " meshes.");
        ConstructAndUpdateMeshes(meshes);
        UpdateMeshesPosition();
    }

    private void UpdateMeshesPosition()
    {
        //todo: could use a parent object (for scaling on the hololens and for easier position update of all the meshes here)
        //-> should then make sure to set the world position

        if (_lastGpsPositions == null)
        {
            Debug.LogError("We currently have received no PositionData, skipping the UpdateMeshesPosition method.");
            return;
        }
        
        if (_lastGpsPositions.Count > _meshObjects.Count)
            Debug.LogWarning("We currently have more positions/headings than meshes. " +
                             "If intended, make sure the positions/headings you want to use are first on the list.");
            
        for (int i = 0; i < _meshObjects.Count; i++)
        {
            if (_lastGpsPositions.Count <= i)
            {
                Debug.LogError("We currently have more meshes ("+_meshObjects.Count+") than we have " +
                               "positions/headings ("+_lastGpsPositions.Count+"). Stopped moving meshes at index " + i);
                return;
            }
            GameObject mesh = _meshObjects[i];
            
            Vector3 position = new Vector3(_lastGpsPositions[i].lat - _gpsOrigin.x, 0, _lastGpsPositions[i].lon - _gpsOrigin.y);
            float heading = _lastGpsPositions[i].hdg;

            mesh.transform.position = position;
            mesh.transform.rotation = Quaternion.identity; //reset rotation to 0,0,0 before RotateAround() (relative)
            //rotate the object around the "upwards" axis, using the center as pivot
            mesh.transform.RotateAround(mesh.GetComponent<Renderer>().bounds.center, Vector3.up, heading);
            //If this ever proves to be a performance issue, we should "cache" the renderer
            //(or the bounds, or even the center point itself), but this would have to be updated when the mesh changes
        }
    }

    /// <summary>
    /// Constructs Unity meshes from a given list of Mesh instances, to replace the old meshes if any.
    /// </summary>
    /// <param name="meshes">The list of Mesh instances to build Unity meshes for.</param>
    private void ConstructAndUpdateMeshes(List<Mesh> meshes)
    {
        //GameObject[] meshReceiver = GameObject.FindGameObjectsWithTag("MeshReceiver"); //Retrieves current meshes

        for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
        {
            Mesh mesh = meshes[meshIndex];
            MeshDraft meshDraft = ConstructMesh(mesh);

            //Assign the constructed mesh to an object:
            if (meshIndex + 1 > _meshObjects.Count
            ) //If we currently don't have enough mesh objects, instantiate a new one
            {
                GameObject receivedMeshInstance = Instantiate(MeshPrefab);
                receivedMeshInstance.GetComponent<MeshFilter>().mesh = meshDraft.ToMesh();
                _meshObjects.Add(receivedMeshInstance);
            }
            else //reuse an old one (simply change its mesh data)
            {
                _meshObjects[meshIndex].GetComponent<MeshFilter>().mesh = meshDraft.ToMesh();
            }

            if (_meshObjects.Count > meshes.Count)
            {
                for (int i = meshes.Count; i < _meshObjects.Count; i++)
                {
                    Destroy(_meshObjects[i]); //Destroy unused mesh objects (if # of meshes has decreased)
                    _meshObjects.RemoveAt(i);
                }

                Resources.UnloadUnusedAssets();
            }
        }
    }

    /// <summary>
    /// Constructs a procedural mesh from the given GHXRTable.Mesh (essentially maps data from one format to the other).
    /// </summary>
    /// <param name="mesh">The given GHXRTable.Mesh that contains the mesh data.</param>
    /// <returns>a procedural mesh (MeshDraft class) to be used with Unity.</returns>
    private MeshDraft ConstructMesh(Mesh mesh)
    {
        List<Vector3> unityVertices = new List<Vector3>();
        foreach (Mesh.Vertex vertex in mesh.Vertices)
        {
            unityVertices.Add(new Vector3(vertex.X, vertex.Y, vertex.Z));
        }

        List<int> unityTriangles = new List<int>();
        foreach (Mesh.Face face in mesh.Faces)
        {
            if (face.IsQuad)
            {
                unityTriangles.Add(face.A);
                unityTriangles.Add(face.B);
                unityTriangles.Add(face.C);

                unityTriangles.Add(face.A);
                unityTriangles.Add(face.C);
                unityTriangles.Add(face.D);
            }
            else
            {
                unityTriangles.Add(face.A);
                unityTriangles.Add(face.B);
                unityTriangles.Add(face.C);
            }
        }

        List<Vector2> unityUvs = new List<Vector2>();
        foreach (Mesh.Uv uv in mesh.Uvs)
        {
            unityUvs.Add(new Vector2(uv.X, uv.Y));
        }

        List<Vector3> unityNormals = new List<Vector3>();
        foreach (Mesh.Normal normal in mesh.Normals)
        {
            unityNormals.Add(new Vector3(normal.X, normal.Y, normal.Z));
        }

        MeshDraft meshDraft = new MeshDraft
        {
            vertices = unityVertices,
            triangles = unityTriangles,
            uv = unityUvs,
            normals = unityNormals
        };

        return meshDraft;
    }
}
