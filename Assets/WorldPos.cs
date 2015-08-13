using UnityEngine;

public class WorldPos
{
	public int x, y, z;

    public WorldPos()
    {
        this.x = 0;
        this.y = 0;
        this.z = 0;
    }

	public WorldPos(int x, int y, int z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public Vector3 ToVector()
	{
		return new Vector3(x, y, z);
	}

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 47;

            hash = hash * 227 + x.GetHashCode();
            hash = hash * 227 + y.GetHashCode();
            hash = hash * 227 + z.GetHashCode();

            return hash;
        }
    }

    public override bool Equals(object obj)
    {
        if (GetHashCode() == obj.GetHashCode())
            return true;
        return false;
    }
}