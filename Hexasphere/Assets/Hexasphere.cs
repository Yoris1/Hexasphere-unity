using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Hexasphere : MonoBehaviour
{
    public readonly float size = 60; // Recommended to not change, but change the offset and size in the transform. 
    [Tooltip("If the inside is bigger than the outside, increase this")]
    public float subtract = 1;
    public int subdivisions = 2;
    public float offset = 6;
    MeshFilter meshFilter;

    public Material insideMaterial;
    void Start()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = createHexasphere(size, offset, subdivisions);
        stopwatch.Stop();
        Debug.Log("Generating first mesh. operation took: " + stopwatch.ElapsedMilliseconds + "ms");

        stopwatch.Reset();

        stopwatch.Start();
        GameObject inside = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        inside.GetComponent<MeshFilter>().mesh = createHexasphere(size + offset - subtract, -0.1f, subdivisions);
        inside.transform.parent = this.transform;
        MeshRenderer insideRenderer = inside.GetComponent<MeshRenderer>();
        insideRenderer.material = insideMaterial;
        insideRenderer.receiveShadows = false; // configure to your liking.
        stopwatch.Stop();
        Debug.Log("Generating inside(second) mesh. operation took: " + stopwatch.ElapsedMilliseconds + "ms");
    }
    Mesh createHexasphere(float size, float offset, int subdivisions)
    { 
        Mesh mesh = new Mesh();
        mesh.name = "Hexasphere";
        HexagonSphere hex = new HexagonSphere((size - offset) / 2, subdivisions, offset);
        Vector3[] vertices = hex.getNewVertices();
        int[] triangles = hex.getNewTriangles(vertices.ToList());
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = hex.getNormals();
        return mesh;
    }
}

public class HexagonSphere
{
    float size;
    int subdivisions;
    List<FinalFace> finalFaces;
    Face[] faces;
    Vector3[] centroidPoints;
    public Vector3[] getNormals()
    {
        List<Vector3> normals = new List<Vector3>();
        foreach(FinalFace face in finalFaces)
        {
            for(int i = 0; i < face.getFaces().Length; i++)
            {
                normals.Add(face.getNormal());
            }
        }
        return normals.ToArray();
    }
    public HexagonSphere(float size, int subdivisions, float offset)
    {
        this.size = size;
        this.subdivisions = subdivisions;
        finalFaces = new List<FinalFace>();

        Storage storage = new Storage();

        FillFaces();

        subdivideFaces(subdivisions);
        foreach (Face face in faces)
        {
            face.findNeighbours(faces);
            face.fixRadius(size);
            face.storePoints(storage);
            face.setOffset(offset);
        }

        finalFaces = storage.findShapeFaces();
    }
    public Vector3[] getNewVertices() {
        List<Vector3> array = new List<Vector3>();
        foreach (FinalFace face in finalFaces)
        {
            foreach (Face f in face.getFaces())
            {
                array.Add(f.offsetCentroid(face));
            }
        }
        return array.ToArray();
    }
    public int[] getNewTriangles(List<Vector3> vertices)
    {
        List<int> array = new List<int>();
        foreach (FinalFace face in finalFaces)
        {
            foreach (Vector3 t in face.getTriangles())
            {
                array.Add(vertices.IndexOf(t));
            }
        }
        return array.ToArray();
    }
    void subdivideFaces(int subdivisionAmt)
    {
        for (int i = 0; i < subdivisionAmt; i++)
        {
            Face[] newFaces = new Face[(faces.Length * 4)];
            for (int f = 0; f < faces.Length; f++)
            {
                Face face = faces[f];
                Face[] subdivided = face.subdivide();
                for (int l = 0; l < subdivided.Length; l++)
                {
                    newFaces[(f * 4) + l] = subdivided[l];
                }
            }
            faces = newFaces;
        }
        centroidPoints = new Vector3[faces.Length];
        for (int i = 0; i < faces.Length; i++)
        {
            centroidPoints[i] = faces[i].getCentroidPoint();
        }
    }

    public void FillFaces()
    {
        Vector3[] vertices = new Vector3[12];
        float tau = 1.61803399f;
        vertices[0] = new Vector3(size, tau * size, 0);
        vertices[1] = new Vector3(-size, tau * size, 0);
        vertices[2] = new Vector3(size, -tau * size, 0);
        vertices[3] = new Vector3(-size, -tau * size, 0);
        vertices[4] = new Vector3(0, size, tau * size);
        vertices[5] = new Vector3(0, -size, tau * size);
        vertices[6] = new Vector3(0, size, -tau * size);
        vertices[7] = new Vector3(0, -size, -tau * size);
        vertices[8] = new Vector3(tau * size, 0, size);
        vertices[9] = new Vector3(-tau * size, 0, size);
        vertices[10] = new Vector3(tau * size, 0, -size);
        vertices[11] = new Vector3(-tau * size, 0, -size);

        faces = new Face[20];
        faces[0] = new Face(vertices[0], vertices[1], vertices[4]);
        faces[1] = new Face(vertices[1], vertices[9], vertices[4]);
        faces[2] = new Face(vertices[4], vertices[9], vertices[5]);
        faces[3] = new Face(vertices[5], vertices[9], vertices[3]);
        faces[4] = new Face(vertices[2], vertices[3], vertices[7]);
        faces[5] = new Face(vertices[3], vertices[2], vertices[5]);
        faces[6] = new Face(vertices[7], vertices[10], vertices[2]);
        faces[7] = new Face(vertices[0], vertices[8], vertices[10]);
        faces[8] = new Face(vertices[0], vertices[4], vertices[8]);
        faces[9] = new Face(vertices[8], vertices[2], vertices[10]);
        faces[10] = new Face(vertices[8], vertices[4], vertices[5]);
        faces[11] = new Face(vertices[8], vertices[5], vertices[2]);
        faces[12] = new Face(vertices[1], vertices[0], vertices[6]);
        faces[13] = new Face(vertices[11], vertices[1], vertices[6]);
        faces[14] = new Face(vertices[3], vertices[9], vertices[11]);
        faces[15] = new Face(vertices[6], vertices[10], vertices[7]);
        faces[16] = new Face(vertices[3], vertices[11], vertices[7]);
        faces[17] = new Face(vertices[11], vertices[6], vertices[7]);
        faces[18] = new Face(vertices[6], vertices[0], vertices[10]);
        faces[19] = new Face(vertices[9], vertices[1], vertices[11]);
    }
}
public class Storage
{
    private Dictionary<Vector3, List<Face>> data = new Dictionary<Vector3, List<Face>>();

    public void addPoint(Vector3 point, Face face)
    {
        if (data.ContainsKey(point))
        {
            data[point].Add(face);
        }
        else
        {
            List<Face> al = new List<Face>();
            al.Add(face);
            data.Add(point, al);
        }
    }

    public List<FinalFace> findShapeFaces()
    {
        List<FinalFace> finalFaces = new List<FinalFace>();

        foreach (KeyValuePair<Vector3, List<Face>> pair in data)
        {
            List<Face> list = data[pair.Key];
            int size = list.Count;
            if (size >= 5)
            {
                FinalFace finalFace = new FinalFace(list, pair.Key);
                finalFaces.Add(finalFace);
            }

        }
        return finalFaces;

    }
}
public class Line
{
    Vector3 s, e;

    public Line(Vector3 start, Vector3 end)
    {
        this.s = start;
        this.e = end;
    }

    public Vector3 getStart()
    {
        return s;
    }
    public Vector3 getEnd()
    {
        return e;
    }
}
public class FinalFace
{
    List<Face> faces = new List<Face>();

    Vector3 normal;
    public FinalFace(List<Face> faces, Vector3 centerPoint)
    {
        this.faces = faces;
        rearangeFaces();
        normal = new Vector3(centerPoint.x, centerPoint.y, centerPoint.z).normalized; // probably buggy
    }
    public Vector3 getNormal()
    {
        return normal;
    }
    public Face[] getFaces()
    {
        return faces.ToArray();
    }
    public Vector3[] getTriangles()
    {
        List<Vector3> array = new List<Vector3>();

        bool frontFace = false;
        // find squared triangle area 

        Vector3 P = faces[2].offsetCentroid(this);
        Vector3 Q = faces[1].offsetCentroid(this);
        Vector3 R = faces[4].offsetCentroid(this);

        Vector3 PR = R - P;
        Vector3 PQ = Q - P;

        Vector3 cross = Vector3.Cross(PR, PQ);

        
        frontFace = Vector3.Dot(cross, normal) <= 0.3f ? false : true;


        if (faces.Count == 5)
        {
            if (frontFace)
            {
                array.Add(faces[0].offsetCentroid(this));
                array.Add(faces[1].offsetCentroid(this));
                array.Add(faces[2].offsetCentroid(this)); // first triangle

                array.Add(faces[2].offsetCentroid(this));
                array.Add(faces[3].offsetCentroid(this));
                array.Add(faces[4].offsetCentroid(this)); // second triangle

                array.Add(faces[4].offsetCentroid(this));
                array.Add(faces[0].offsetCentroid(this));
                array.Add(faces[2].offsetCentroid(this)); // third triangle
            } else
            {
                array.Add(faces[0].offsetCentroid(this));
                array.Add(faces[4].offsetCentroid(this));
                array.Add(faces[1].offsetCentroid(this)); // first triangle

                array.Add(faces[4].offsetCentroid(this));
                array.Add(faces[3].offsetCentroid(this));
                array.Add(faces[2].offsetCentroid(this)); // second triangle

                array.Add(faces[2].offsetCentroid(this));
                array.Add(faces[1].offsetCentroid(this));
                array.Add(faces[4].offsetCentroid(this)); // third triangle
            }

            
        } else if (faces.Count == 6)
        {
            if (frontFace)
            {
                array.Add(faces[0].offsetCentroid(this));
                array.Add(faces[1].offsetCentroid(this));
                array.Add(faces[2].offsetCentroid(this)); // first triangle

                array.Add(faces[2].offsetCentroid(this));
                array.Add(faces[3].offsetCentroid(this));
                array.Add(faces[0].offsetCentroid(this)); // I really don't think these comments

                array.Add(faces[3].offsetCentroid(this));
                array.Add(faces[4].offsetCentroid(this));
                array.Add(faces[5].offsetCentroid(this)); // are necessary anymore

                array.Add(faces[5].offsetCentroid(this));
                array.Add(faces[0].offsetCentroid(this));
                array.Add(faces[3].offsetCentroid(this)); // fourth triangle
            }
            else
            {
                // second side
                array.Add(faces[0].offsetCentroid(this));
                array.Add(faces[5].offsetCentroid(this));
                array.Add(faces[1].offsetCentroid(this)); // first triangle

                array.Add(faces[5].offsetCentroid(this));
                array.Add(faces[4].offsetCentroid(this));
                array.Add(faces[3].offsetCentroid(this)); // second triangle

                array.Add(faces[3].offsetCentroid(this));
                array.Add(faces[2].offsetCentroid(this));
                array.Add(faces[1].offsetCentroid(this)); // third triangle

                array.Add(faces[1].offsetCentroid(this));
                array.Add(faces[5].offsetCentroid(this));
                array.Add(faces[3].offsetCentroid(this)); // fourth triangle
            }
        }

        return array.ToArray();

    }
    public void render()
    {
        // do unity line rendering for testing;
    }
    
    public void rearangeFaces()
    {
        List<Face> rearanged = new List<Face>();
        rearanged.Add(faces[0]);
        Face lastFace = faces[0];
        Face firstFace = lastFace;
        faces.Remove(lastFace);
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Face currentFace = lastFace.neighbours[j];
                if (faces.Contains(currentFace))
                {
                    faces.Remove(currentFace);
                    rearanged.Add(currentFace);
                    lastFace = currentFace;
                }
            }
        }
        faces = rearanged;
    }
    public Vector3 offsetToRadius(Vector3 p, float sphereRadius)
    {
        float currentDistance = (p.x * p.x) + (p.y * p.y) + (p.z * p.z);
        float adjustment = (sphereRadius * sphereRadius) / currentDistance;
        return new Vector3(p.x * adjustment, p.y * adjustment, p.z * adjustment);
    }
}
public class Face
{
    float offset = 5;
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
    Line[] lines = new Line[3];
    public Face[] neighbours = new Face[3];
    public void setOffset(float offset)
    {
        this.offset = offset;
    }
    public Face(Vector3 point1, Vector3 point2, Vector3 point3)
    {
        this.p1 = point1;
        this.p2 = point2;
        this.p3 = point3;

        lines[0] = new Line(p1, p2);
        lines[1] = new Line(p2, p3);
        lines[2] = new Line(p3, p1);
    }

    public Face[] subdivide()
    {
        Vector3 m1 = new Vector3((p1.x + p2.x) / 2, (p1.y + p2.y) / 2, (p1.z + p2.z) / 2);
        Vector3 m2 = new Vector3((p2.x + p3.x) / 2, (p2.y + p3.y) / 2, (p2.z + p3.z) / 2);
        Vector3 m3 = new Vector3((p3.x + p1.x) / 2, (p3.y + p1.y) / 2, (p3.z + p1.z) / 2);

        Face[] array = new Face[4];
        array[0] = new Face(m1, p1, m3);
        array[1] = new Face(m3, p3, m2);
        array[2] = new Face(m2, m1, m3);
        array[3] = new Face(p2, m1, m2);

        return array;
    }

    public Vector3 getCentroidPoint()
    {
        return multiplyVector(new Vector3((p1.x + p2.x + p3.x) / 3, (p1.y + p2.y + p3.y) / 3, (p1.z + p2.z + p3.z) / 3), 1.07f);
    }

    public Vector3 offsetCentroid(FinalFace f) 
    {
        return getCentroidPoint() + (f.getNormal()* offset);
    }

    public void findNeighbours(Face[] faces)
    {
        int neighboursSize = 0;
        foreach (Face face in faces)
        {
            foreach (Line line1 in face.lines)
            {
                foreach (Line line2 in this.lines)
                {
                    if (Comparer.compareLines(line1, line2) && face != this)
                    {
                        neighbours[neighboursSize++] = face;
                    }
                }
            }
        }
    }
    public float correctToRadius(float sphereRadius, Vector3 p)
    {
        float currentDistance = Mathf.Sqrt((p.x * p.x) + (p.y * p.y) + (p.z * p.z));
        float adjustment = sphereRadius / currentDistance;
        return adjustment;
    }
    public void fixRadius(float radius)
    {
        p1 = multiplyVector(p1, correctToRadius(radius, p1));
        p2 = multiplyVector(p2, correctToRadius(radius, p2));
        p3 = multiplyVector(p3, correctToRadius(radius, p3));
    }
    public Vector3 multiplyVector(Vector3 point, float multiplication)
    {
        return new Vector3(point.x * multiplication, point.y * multiplication, point.z * multiplication);
    }
    public void storePoints(Storage storage)
    {
        storage.addPoint(p1, this);
        storage.addPoint(p2, this);
        storage.addPoint(p3, this);
    }
}
static class Comparer
{
    public static bool compareLines(Line l1, Line l2)
    {
        Vector3 start1 = l1.getStart();
        Vector3 start2 = l2.getStart();

        Vector3 end1 = l1.getEnd();
        Vector3 end2 = l2.getEnd();

        if (areVectorsTheSame(start1, start2))
        {
            return areVectorsTheSame(end1, end2);
        }
        else if (areVectorsTheSame(start1, end2))
        {
            return areVectorsTheSame(end1, start2);
        }
        else
        {
            return false;
        }
    }
    public static bool areVectorsTheSame(Vector3 v1, Vector3 v2)
    {
        return (v1.x == v2.x && v1.y == v2.y && v1.z == v2.z);
    }
}