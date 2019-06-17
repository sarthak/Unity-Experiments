using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof ( MeshRenderer ))]
[RequireComponent(typeof ( MeshFilter ))]
public class TerrainGenerator : MonoBehaviour
{
	public enum GAlgo {
		RANDOM_SPRAY, FROM_TEXTURE, TEXTURE_SUPERIMPOSE
	}

	[Header("Terrain properties")]
	[Tooltip("Terrain will be of dimensions size X size")]
	public int size = 10;
	[Tooltip("Number of vertices per unit size")]
	public int verts = 1;
	[Tooltip("Max possible height/depression in terrain")]
	public float max_height;

	[Header("Spray Adjustments")]
	[Tooltip("Radius of spray tool")]
	public int radius;
	[Tooltip("Variation of spray change with distance")]
	public AnimationCurve spray_texture;
	[Tooltip("Curve of cumulative probability of height, flatter is more")]
	public AnimationCurve height_probability;

	[Header("Generator properties")]
	[Tooltip("Grayscale image representing the height map of the terrain")]
	public Texture2D heightmap;
	[Tooltip("Method to use during terrain generation")]
	public GAlgo algo;
	[Tooltip("Weight to give to texture if using TEXTURE_SUPERIMPOSE as algo")]
	[Range(0f, 1f)]
	public float weight = 0.5f;
	[Tooltip("How many sprays to hit if algo is not FROM_TEXTURE")]
	public int number_sprays;
	[Tooltip("Whether the RNG should be seeded by custom seed")]
	public bool seeded_generation = false;
	public Random.State rand_seed;
	[Tooltip("Do not modify, Read only")]
	public Random.State current_seed;
	

	//stores values of normalized signed height at each point in terrain 256*256
	float[,] heightmap_float;
	//Referene to mesh filter component
	MeshFilter filter;
	//Reference to mesh component
	Mesh mesh;

	void Start() {
		//initialize the setup
		filter = GetComponent<MeshFilter>();
		mesh = new Mesh();
		filter.mesh = mesh;
		heightmap_float = new float[256, 256];
		current_seed = Random.state;
	}

	float GetHeightFromTex (int x, int y) {
		int x256 = (x * 256)/(verts*size);
		int y256 = (y * 256)/(verts*size);
		float height = (heightmap.GetPixel(x256, y256).grayscale - 0.5f) * max_height;
		return height;
	}

	float GetHeightFromFloatMap (int x, int y) {
		int x256 = (x * 256)/(verts*size);
		int y256 = (y * 256)/(verts*size);
		float height = heightmap_float[x256, y256] * max_height;
		return height;
	}

	//Final height after superimposing both the texture and float-map together
	float GetHeight(int x, int y) {
		if (algo == GAlgo.RANDOM_SPRAY)
			return GetHeightFromFloatMap(x, y);
		else if (algo == GAlgo.FROM_TEXTURE)
			return GetHeightFromTex(x, y);

		//Superimpose
		return GetHeightFromTex(x, y) * weight + GetHeightFromFloatMap(x, y) * (1-weight);
	}

	//Debugging only !!
	Color GetColorFromFloatMap (int x, int y) {
		int x256 = (x * 256)/(verts*size);
		int y256 = (y * 256)/(verts*size);
		return Color.HSVToRGB(0, 0, 0.5f + heightmap_float[x256, y256]);
	}

	//Actual method responsible for generating mesh
	void GenerateMesh() {
		//Clear before start
		mesh.Clear();

		//initialize
		Vector3[] vertices = new Vector3[verts*size*verts*size];
		float delta = 1f/verts;
		int c=0;

		for (int y=0; y<verts*size; y++) {
			for (int x=0; x<verts*size; x++) {
				vertices[c++] = new Vector3(x*delta, GetHeight(x, y), y*delta);
			}
		}

		int[] triangles = new int[size*verts*size*verts*2*3];
		c = 0;
		for(int y=0; y<verts*size-1; y++) {
			for (int x=0; x<verts*size-1; x++) {
				triangles[c+0] = verts*size*y + x;
				triangles[c+1] = verts*size*y + verts*size + x;
				triangles[c+2] = verts*size*y + x+1;
				triangles[c+3] = verts*size*y + x+1;
				triangles[c+4] = verts*size*y + verts*size + x;
				triangles[c+5] = verts*size*y + verts*size + x+1;

				c+=6;
			}
		}

		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
	}


	//Mesh only for debugging
	void GenerateMeshForDebugSpray() {
		mesh.Clear();
		Vector3[] vertices = new Vector3[verts*size*verts*size];
		Color[] colors = new Color[vertices.Length];
		float delta = 1f/verts;
		int c=0;
		for (int y=0; y<verts*size; y++) {
			for (int x=0; x<verts*size; x++) {
				colors[c] = GetColorFromFloatMap(x, y);
				vertices[c++] = new Vector3(x*delta, 0, y*delta);
			}
		}

		int[] triangles = new int[size*verts*size*verts*2*3];
		c = 0;
		for(int y=0; y<verts*size-1; y++) {
			for (int x=0; x<verts*size-1; x++) {
				triangles[c+0] = verts*size*y + x;
				triangles[c+1] = verts*size*y + verts*size + x;
				triangles[c+2] = verts*size*y + x+1;
				triangles[c+3] = verts*size*y + x+1;
				triangles[c+4] = verts*size*y + verts*size + x;
				triangles[c+5] = verts*size*y + verts*size + x+1;

				c+=6;
			}
		}

		mesh.vertices = vertices;
		mesh.colors = colors;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
	}

	//Calculate the effect of spray centered at x,y at the point _x, _y
	void SprayPoint (int x, int y, int _x, int _y, float spray_color) {
		//Boundary value check
		if (x+_x > 255 || x+_x < 0)
			return;
		if (y+_y > 255 || y+_y < 0)
			return;

		float point = Mathf.Sqrt(_x*_x + _y*_y)/radius;
		point = point > 1? 1 : point;
		float spray = spray_color * spray_texture.Evaluate(point);
		heightmap_float[x+_x, y+_y] = Mathf.Clamp(heightmap_float[x+_x, y+_y]+spray, -0.5f, 0.5f);
	}

	//Calling this method gives 1 spray
	void Spray() {
		int x = (int)Random.Range(0, 255);
		int y = (int)Random.Range(0, 255);
		float spray_color = height_probability.Evaluate(Random.value);

		for (int _y=-radius; _y<=radius; _y++)
			for (int _x=-radius; _x<=radius; _x++)
				SprayPoint (x, y, _x, _y, spray_color);
	}

	void Update() {
		if (Input.GetKeyDown(KeyCode.Space)) {
			if (seeded_generation)
				Random.state = rand_seed;
			heightmap_float = new float[256, 256];
			for (int i=0; i<number_sprays; i++)
				Spray();
			GenerateMesh();
			if (seeded_generation)
				Random.state = current_seed;
		}
		if (Input.GetKeyDown(KeyCode.D)) {
			for (int i=0; i<number_sprays; i++)
				Spray();
			GenerateMeshForDebugSpray();
		}
	}
}
