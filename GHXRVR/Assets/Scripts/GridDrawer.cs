using UnityEngine;

public class GridDrawer : MonoBehaviour
{
    [Tooltip("The material to use for drawing lines, should probably be a simple white material")]
    public Material GLMat;
    [Tooltip("The depth of the grid (half the length of one side)")]
    public int GridDepth = 100;
    [Tooltip("The size of a tile in the grid (in meters)")]
    public float TileLength = 1.0f;
    [Tooltip("The Z position of the grid; tests resulted in a default value of -0.038")]
    public float GridZPosition = -0.038f;
    [Tooltip("The color (including alpha=opacity) of the grid.")]
    public Color GridColor;

    private Vector3 _initialPosition;

    void Start()
    {
        Camera headCamera = GetComponent<Camera>();
        if (headCamera == null)
        {
            Debug.LogError("No camera found on this object, will not be able to draw the grid");
        }
        _initialPosition = new Vector3(headCamera.transform.position.x, GridZPosition, headCamera.transform.position.z);
    }

    void OnPostRender()
    {
        //TODO: could check whether the user is too far from the initial position (and react accordingly)

        GL.PushMatrix();
        GLMat.SetPass(0);
        GL.Begin(GL.LINES);

        GL.Color(Color.white);
        //GL.Color(new Color(1,1,1,.01f));

        //Major x line
        GL.Vertex(_initialPosition + new Vector3(GridDepth * TileLength, 0, 0));
        GL.Vertex(_initialPosition + new Vector3(-GridDepth * TileLength, 0, 0));
        //Major z line
        GL.Vertex(_initialPosition + new Vector3(0, 0, GridDepth * TileLength));
        GL.Vertex(_initialPosition + new Vector3(0, 0, -GridDepth * TileLength));

        GL.Color(GridColor);

        for (int i = 1; i < GridDepth + 1; i++)
        {
            //positive x lines
            GL.Vertex(_initialPosition + new Vector3(i * TileLength, 0, GridDepth * TileLength));
            GL.Vertex(_initialPosition + new Vector3(i * TileLength, 0, -GridDepth * TileLength));
            //negative x lines
            GL.Vertex(_initialPosition + new Vector3(-i * TileLength, 0, GridDepth * TileLength));
            GL.Vertex(_initialPosition + new Vector3(-i * TileLength, 0, -GridDepth * TileLength));
            //positive z lines
            GL.Vertex(_initialPosition + new Vector3(GridDepth * TileLength, 0, i * TileLength));
            GL.Vertex(_initialPosition + new Vector3(-GridDepth * TileLength, 0, i * TileLength));
            //negative z lines
            GL.Vertex(_initialPosition + new Vector3(GridDepth * TileLength, 0, -i * TileLength));
            GL.Vertex(_initialPosition + new Vector3(-GridDepth * TileLength, 0, -i * TileLength));
        }

        GL.End();
        GL.PopMatrix();
    }
}