using UnityEngine;

public class TerrainGrid : MonoBehaviour
{
    private Mesh _mesh;
    private Terrain _terrain;

    private void Awake()
    {
        _terrain = FindObjectOfType<Terrain>();
        _mesh = GetComponent<MeshFilter>().mesh;
    }

    private void Update()
    {
        BlockRotationVertical();
        DropOverlaySquare();
    }

    private void BlockRotationVertical()
    {
        var rot = transform.rotation;
        rot.eulerAngles = new Vector3(0, rot.eulerAngles.y, 0);
        transform.rotation = rot;
    }

    /*
        code assumes you have a 2d array of gameobjects in your class called overlayGrid and that it already sits above your terrain (<20 height)
    */
    private void DropOverlaySquare()
    {
        var vertices = _mesh.vertices;

        for (var i = 0; i < vertices.Length; i++)
        {
            var localPos = vertices[i];
            var worldPos = transform.TransformPoint(localPos);
            worldPos.y = _terrain.SampleHeight(worldPos) + 0.2f;
            vertices[i] = transform.InverseTransformPoint(worldPos);
        }

        _mesh.vertices = vertices;

        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();
    }
}