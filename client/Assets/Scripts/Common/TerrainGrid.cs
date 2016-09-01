using UnityEngine;
using System.Collections;
using System.Linq;

[ExecuteInEditMode]
[RequireComponent( typeof( MeshRenderer ) )]
[RequireComponent( typeof( MeshFilter ) )]
public class TerrainGrid : MonoBehaviour {

	public Terrain BaseTerrain;
	public float GridSize = 1;

	MeshFilter meshFilter;
	float[,] heightMap;
    Rogue.Point[] points;

	bool dirty = true;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		if (dirty) {
			Refresh ();
		}
	}

	public void Refresh(){
		dirty = false;

		if (BaseTerrain == null) {
			Debug.LogWarning ("BaseTerrain must not be null");
			return;
		}

		meshFilter = GetComponent<MeshFilter> ();
		if (meshFilter == null) {
			return;
		}

		var data = BaseTerrain.terrainData;
		var gridWidth = Mathf.FloorToInt ((data.size.x + GridSize) / GridSize);
		var gridLength = Mathf.FloorToInt ((data.size.z + GridSize) / GridSize);

		heightMap = new float[gridWidth, gridLength];
		var statMap = new int[gridWidth, gridLength];
		for (int x = 0; x < gridWidth; x++) {
			for (int z = 0; z < gridLength; z++) {
				heightMap [x, z] = data.GetInterpolatedHeight (x * GridSize / data.size.x, z * GridSize / data.size.z);
				statMap [x, z] = (data.GetInterpolatedNormal (
					(x * GridSize + GridSize * 0.5f) / data.size.x, 
					(z * GridSize + GridSize * 0.5f) / data.size.z).y > 0.95f) ? 1: 0;

                if( points != null && !points.Any(p => (p.X == x && p.Y == z))){
                    statMap[x, z] = 0;
                }
			}
		}

		var mesh = new Mesh ();
		var vertices = new Vector3[gridWidth * gridLength];
		var uvs = new Vector2 [gridWidth * gridLength];
		var triangles = new int [gridWidth * gridLength * 3 * 2];

		for (int x = 0; x < gridWidth; x++) {
			for (int z = 0; z < gridLength; z++) {
				vertices [x + z * gridWidth] = new Vector3 (x * GridSize, heightMap [x, z], z * GridSize);
				uvs [x + z * gridWidth] = new Vector2 (x, z);
			}
		}

		for (int x = 0; x < gridWidth-1; x++) {
			for (int z = 0; z < gridLength-1; z++) {
				var i = x + z * gridWidth;
				if (statMap [x, z] == 0) continue;
				triangles [i * 6 + 0] = i;
				triangles [i * 6 + 1] = i + 1 + gridWidth;
				triangles [i * 6 + 2] = i + 1;
				triangles [i * 6 + 3] = i;
				triangles [i * 6 + 4] = i + gridWidth;
				triangles [i * 6 + 5] = i + 1 + gridWidth;
			}
		}

		mesh.vertices = vertices;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
        //mesh.Optimize();

        mesh.hideFlags = HideFlags.HideAndDontSave;

		meshFilter.sharedMesh = mesh;
		meshFilter.sharedMesh.name = "Terrain Grid";

        var collider = GetComponent<MeshCollider>();
        if( collider != null)
        {
            collider.sharedMesh = mesh;
        }
	}

    public void SetActiveGrids(Rogue.Point[] points_)
    {
        points = points_;
        Refresh();
    }

}
