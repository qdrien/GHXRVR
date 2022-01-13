using System.Collections.Generic;

public class Mesh
{
    public List<Vertex> Vertices;
    public List<Uv> Uvs;
    public List<Normal> Normals;
    public List<Face> Faces;
        
    public class Vertex
    {
        public float X;
        public float Y;
        public float Z;

        public Vertex(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }

    public class Uv
    {
        public float X;
        public float Y;

        public Uv(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    public class Normal
    {
        public float X;
        public float Y;
        public float Z;

        public Normal(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }

    public class Face
    {
        public bool IsQuad;
        public int A;
        public int B;
        public int C;
        public int D;

        public Face(bool isQuad, int a, int b, int c, int d)
        {
            this.IsQuad = isQuad;
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }
    }
}
