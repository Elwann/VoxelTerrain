using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IsoTerrain : MonoBehaviour {

	public Material terrainMaterial;

	bool generated = false;
	bool spawned = false;
	int iterations = 2048;
    int height = 186;

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

		//float noise = /*y/h/0.05f +*/ ( ImprovedNoise.Noise((x*2f+5f)/20f,(y*2f+3f)/40f,(z*2f+0.6f)/20f) - ImprovedNoise.Noise(x/10f,y/5f,z/10f) ) / 2f;
		//Debug.Log (noise);
		/*if(y > h/2){
			noise -= (y - h/2)/(h/2);
		} else {
			noise += (h/2 - y)/(h/2);
		}*/

		/*if(y > h / 2 && y / h > ImprovedNoise.Noise(x/30f,0f,z/30f)){
			noise = -1;
		} else if(y > h - h / 8) {
			noise = -1;
		}*/

        //float noise = Mathf.PerlinNoise(x / 60f, z / 60f) * Mathf.PerlinNoise(x / 30f + 28f, z / 1000f + 143f) * Mathf.PerlinNoise(x / 100f + 65f, z / 30f + 124f) * 2 - (height - y) / height;
        float mountain = (y / height) - Mathf.PerlinNoise(x / 60f, z / 60f) - 1f /* (y / height)*/;

		float noise = 0;

        if (y > mountain)
			noise = lerp(0.1f, y / height, 1f);
        else
            noise = lerp(0.1f, 1f - y / height, 0f);

		//noise *= (1.0f-ImprovedNoise.Noise((x*2f+5f)/20f,(y*2f+3f)/40f,(z*2f+0.6f)/20f)) * 0.8f;

        return noise * 2f - 1f;
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
