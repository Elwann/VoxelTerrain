using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IsoTerrain : MonoBehaviour {

	public Material terrainMaterial;

	bool generated = false;
	bool spawned = false;
	int iterations = 2048;
    int height = 128;

	int x = 0;
	int y = 0;
	int z = 0;

	int originX = 0;
	int originY = 0;
	int originZ = 0;
	
	int w = 64;
	int h = 64;
	int p = 64;

	float[] datas;

	void Start ()
	{
		// Get origin
		originX = Mathf.RoundToInt(transform.position.x);
		originY = Mathf.RoundToInt(transform.position.y);
		originZ = Mathf.RoundToInt(transform.position.z);

		// Snap position
		Vector3 position = new Vector3(originX, originY, originZ);
		transform.position = position;

		// Init Datas
		datas = new float[w*h*p];
	}

	void Update ()
	{
		if(!generated)
		{
			GenerateNoise();
		}
		else if(!spawned)
		{
			GenerateMesh();
		}
	}

	float TerrainGenerator(int x, int y, int z)
	{
        float noise = 0;
        noise += (Mathf.PerlinNoise(x / 60f + 50f, z / 50f + 15f) * Mathf.PerlinNoise(x / 30f + 28f, z / 1000f + 143f) * Mathf.PerlinNoise(x / 100f + 65f, z / 30f + 124f) * height - y) / height;
		noise += (1f + ImprovedNoise.Noise((x*2f+5f)/40f,(y*2f+3f)/80f,(z*2f+0.6f)/40f)) / 8f;
        noise += Mathf.Max(0f, (Mathf.PerlinNoise(x / 115f + 54f, z / 95f + 61f) / 2f * height - y) / height);

        return noise / height;
	}

    float lerp(float t, float a, float b) { return a + t * (b - a); }

	void GenerateNoise()
	{
		for(int i = 0; i < iterations; ++i)
		{
			datas[z * h * w + y * w + x] = TerrainGenerator(originX + x, originY + y, originZ + z);
			
			++x;
			
			if(x >= w){
				x = 0;
				++y;
			}
			
			if(y >= h){
				y = 0;
				++z;
			}
			
			if(z >= p){
				generated = true;
				break;
			}
		}

		Debug.Log ("Generating");
	}

	void GenerateMesh()
	{
		SurfaceNets.Mesh m = SurfaceNets.Run(datas, new int[]{w, h, p});

		Mesh mesh = new Mesh ();
		
		if(!transform.GetComponent<MeshFilter> () ||  !transform.GetComponent<MeshRenderer> () ) //If you will havent got any meshrenderer or filter
		{
			transform.gameObject.AddComponent<MeshFilter>();
			transform.gameObject.AddComponent<MeshRenderer>();
		}
		
		transform.GetComponent<MeshFilter> ().mesh = mesh;
		
		mesh.name = "Terrain";
		
		mesh.vertices = m.vertices;
        mesh.triangles = m.triangles;
		//mesh.uv = UV_MaterialDisplay;
		
		mesh.RecalculateNormals ();
		mesh.Optimize ();
		transform.GetComponent<Renderer>().material = terrainMaterial;

		Debug.Log ("Complete");

		spawned = true;
	}
}
