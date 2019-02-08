using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hexasphere : MonoBehaviour
{
    Mesh mesh;
    MeshRenderer meshRenderer;
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        mesh = new Mesh();
        generateSphereMesh();
        Debug.Log(meshRenderer);
    }
    void generateSphereMesh()
    {

    }
}

public class HexagonSphere
{
    int size;
    int subdivisions;
    List<FinalFace> finalFaces;
    Face[] faces;
    public HexagonSphere(int size, int subdivisions)
    {
        this.size = size;
        this.subdivisions = subdivisions;
        finalFaces = new List<FinalFace>();

        Storage storage = new Storage();

        // continue translating the processing code.

    }
}
public class Storage
{

}
public class Line
{

}
public class FinalFace
{

}
public class Face
{

}