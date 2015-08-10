using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IsoTerrain : MonoBehaviour {

	public Material terrainMaterial;

	bool generated = false;
	bool spawned = false;
	int iterations = 512;

	int x = 0;
	int y = 0;
	int z = 0;

	int originX = 0;
	int originY = 0;
	int originZ = 0;
	
	int w = 32;
	int h = 16;
	int p = 32;

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

		float height = Mathf.PerlinNoise(x/20f,z/10f) * Mathf.PerlinNoise(x/10f,z/60f) * Mathf.PerlinNoise(x/100f,z/30f) * h;
		float noise = 1;

		if(y > height){
			noise = -1;
		}

		noise *= ImprovedNoise.Noise((x*2f+5f)/20f,(y*2f+3f)/40f,(z*2f+0.6f)/20f);

		return noise;
	}

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

		Vector3[] vertices = new Vector3[m.vertices.Count];
		int[] triangles = new int[m.faces.Count * 6];

		for(int v = 0, vl = m.vertices.Count; v < vl; ++v)
		{
			float[] vertice = m.vertices[v];
			vertices[v] = new Vector3(vertice[0], vertice[1], vertice[2]);
		}

		for(int f = 0, fl = m.faces.Count * 6; f < fl; f += 6)
		{
			int[] face = m.faces[f / 6];

			triangles[f + 0] = face[0];
			triangles[f + 1] = face[3];
			triangles[f + 2] = face[1];
			
			triangles[f + 3] = face[3];
			triangles[f + 4] = face[2];
			triangles[f + 5] = face[1];
		}

		Mesh mesh = new Mesh ();
		
		if(!transform.GetComponent<MeshFilter> () ||  !transform.GetComponent<MeshRenderer> () ) //If you will havent got any meshrenderer or filter
		{
			transform.gameObject.AddComponent<MeshFilter>();
			transform.gameObject.AddComponent<MeshRenderer>();
		}
		
		transform.GetComponent<MeshFilter> ().mesh = mesh;
		
		mesh.name = "Terrain";
		
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		//mesh.uv = UV_MaterialDisplay;
		
		mesh.RecalculateNormals ();
		mesh.Optimize ();
		transform.GetComponent<Renderer>().material = terrainMaterial;

		Debug.Log ("Complete");

		spawned = true;
	}
}
