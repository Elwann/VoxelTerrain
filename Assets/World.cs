using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimplexNoise;

public class World : MonoBehaviour {

	public Chunk chunk;

	Chunk temp;

	List<WorldPos> pending = new List<WorldPos>();

	int sizeX = 6;
	int sizeY = 3;
	int sizeZ = 6;

	int spacing = 30;

	void Start()
	{
		for(int x = 0; x < sizeX; ++x)
		{
			for(int z = 0; z < sizeZ; ++z)
			{
				for(int y = 0; y < sizeY; ++y)
				{
					pending.Add (new WorldPos(x, y, z));
				}
			}
		}

		CreateChunk();
	}

	void Update()
	{
		if(pending.Count > 0)
		{
			if(temp.Complete())
			{
				CreateChunk();
			}
		}
	}

	void CreateChunk()
	{
		if(pending.Count > 0)
		{
			temp = Instantiate(chunk, pending[0].ToVector() * spacing - new Vector3(spacing/2f, spacing/2f, spacing/2f), new Quaternion()) as Chunk;
			temp.transform.parent = transform;
			pending.RemoveAt(0);
		}
	}
}
