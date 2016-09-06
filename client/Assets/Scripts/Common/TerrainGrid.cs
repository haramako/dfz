using UnityEngine;
using System.Collections;
using System.Linq;

[ExecuteInEditMode]
[RequireComponent( typeof( MeshRenderer ) )]
[RequireComponent( typeof( MeshFilter ) )]
public class TerrainGrid : MonoBehaviour {

	public Terrain BaseTerrain;
	public float GridSize = 1;

	public int[,] StatMap;
	public float[,] HeightMap { get { return heightMap; } }

	MeshFilter meshFilter;
	float[,] heightMap;

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

	public int GetStat(int x, int z){
		if (StatMap == null) {
			return 1;
		}else if( x < 0 || z < 0 || x >= StatMap.GetLength(0) || z >= StatMap.GetLength(1) ){
			return 0;
		}else{
			return StatMap[x,z];
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
		for (int x = 0; x < gridWidth; x++) {
			for (int z = 0; z < gridLength; z++) {
				var ray = new Ray (new Vector3 (x * GridSize + GridSize * 0.5f, 1000, z * GridSize + GridSize*0.5f), new Vector3 (0, -1, 0));
				RaycastHit hit;
				if (Physics.Raycast (ray, out hit)) {
					heightMap [x, z] = hit.point.y;
					// heightMap [x, z] = data.GetInterpolatedHeight (x * GridSize / data.size.x, z * GridSize / data.size.z);
				}
			}
		}

		var mesh = new Mesh ();
		var vertices = new Vector3[gridWidth * gridLength * 4];
		var uvs = new Vector2 [gridWidth * gridLength * 4];
		var triangles = new int [gridWidth * gridLength * 3 * 2];

		for (int x = 0; x < gridWidth; x++) {
			for (int z = 0; z < gridLength; z++) {
				var idx = (x + z * gridWidth) * 4;
				for (int n = 0; n < 4; n++) {
					var cx = n % 2;
					var cz = n / 2;
					vertices [idx+n] = new Vector3 ((x+cx) * GridSize, heightMap [x, z], (z+cz) * GridSize);
					uvs [idx+n] = new Vector2 (x+cx, z+cz);
				}
			}
		}

		for (int x = 0; x < gridWidth-1; x++) {
			for (int z = 0; z < gridLength-1; z++) {
				var i = (x + z * gridWidth);
				var i4 = i * 4;
				if (GetStat(x,z) == 0 ) continue;
				triangles [i * 6 + 0] = i4;
				triangles [i * 6 + 1] = i4 + 3;// + gridWidth;
				triangles [i * 6 + 2] = i4 + 1;
				triangles [i * 6 + 3] = i4;
				triangles [i * 6 + 4] = i4 + 2;//;+ gridWidth;
				triangles [i * 6 + 5] = i4 + 3;//+ 1 + gridWidth;
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

    public void SetActiveGrids(Game.Point[] points_)
    {
		for (int x = 0; x < StatMap.GetLength (0); x++) {
			for (int y = 0; y < StatMap.GetLength (1); y++) {
				StatMap [x, y] = 0;
			}
		}
		foreach (var p in points_) {
			StatMap [p.X, p.Y] = 1;
		}
        Refresh();
    }

}
