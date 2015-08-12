using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimplexNoise;

public class Chunk : MonoBehaviour {
	
	public Material terrainMaterial;
	
	bool generated = false;
	bool spawned = false;
	int iterations = 4048;
	
	int x = 0;
	int y = 0;
	int z = 0;
	
	int originX = 0;
	int originY = 0;
	int originZ = 0;
	
	int w = 32;
	int h = 32;
	int p = 32;

	float floor = 16f;
	
	float[] datas;

	float time = 0f;
	int frame = 0;
	
	void Start ()
	{
		time = Time.time;

		// Get origin
		originX = Mathf.RoundToInt(transform.position.x);
		originY = Mathf.RoundToInt(transform.position.y);
		originZ = Mathf.RoundToInt(transform.position.z);
		
		// Snap position
		Vector3 position = new Vector3(originX, originY, originZ);
		transform.position = position;
		
		// Init Datas
		datas = new float[w*h*p];

		Debug.Log ("Generating Chunk "+"("+originX+","+originY+","+originZ+")");
	}
	
	void Update ()
	{
		if(!generated)
		{
			GenerateNoise();
			++frame;
		}
		else if(!spawned)
		{
			GenerateMesh();
			++frame;
		}
	}
	
	float TerrainGenerator(int x, int y, int z)
	{
		float density = -y + floor;

		density += Mathf.Clamp(GetNoise(x, -45, z, 0.006f, 64.0f) -8f, -16f, 48f);
		density += Mathf.Clamp((32 - y)*1f,-1f,1f)*2f; 

		//density += GetNoise(x, y, z, 2f, 0.4f);
		//density += GetNoise(x, y, z, 0.2f, 5f);
		density += GetNoise(x, y, z, 0.4f, 0.2f);
		density += GetNoise(x, y, z, 0.1f, 1.0f);
		//density += GetNoise(x, y, z, 0.04f, 4.0f);
		density += GetNoise(x, y, z, 0.02f, 16.0f);
		
		density += GetNoise(x, -100, z, 0.01f, 8.0f);
		//density += GetNoise(x, 100, z, 0.004f, 64.0f);

		density += Mathf.Clamp01((floor - y)*0.4f)*12f; 
		
		return density;
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
		
		//Debug.Log ("Generating");
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

		transform.gameObject.AddComponent<MeshCollider>();
		
		transform.GetComponent<MeshFilter> ().mesh = mesh;
		MeshCollider col = transform.GetComponent<MeshCollider>();
		col.sharedMesh = mesh;
		col.convex = true;
		
		mesh.name = "Chunk "+"("+originX+","+originY+","+originZ+")";
		
		mesh.vertices = m.vertices;
		mesh.triangles = m.triangles;
		//mesh.uv = UV_MaterialDisplay;
		
		mesh.RecalculateNormals ();
		mesh.Optimize ();
		transform.GetComponent<Renderer>().material = terrainMaterial;

		time = Time.time - time;
		
		Debug.Log ("Complete "+"("+originX+","+originY+","+originZ+") in "+time+" frame "+(time/frame)+" nbr "+frame);
		
		spawned = true;
	}
	
	public static float GetNoise(int x, int y, int z, float scale, float max)
	{
		//return (Noise.Generate(x * scale, y * scale, z * scale) + 1f) * (max / 2f);
		return Noise.Generate(x * scale, y * scale, z * scale) * max;
	}
	
	public bool Complete(){
		return spawned;
	}
}
