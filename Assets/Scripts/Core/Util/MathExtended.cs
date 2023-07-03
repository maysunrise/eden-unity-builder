using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathExtended
{
    public static Vector2 ClampSquare(Vector2 vector, float maxLength)
    {
        vector.x = Mathf.Clamp(vector.x, -maxLength, maxLength);
        vector.y = Mathf.Clamp(vector.y, -maxLength, maxLength);
        return vector;
    }

	public static int twoToOne(int cx, int cz) // ported from obj-c
	{
		if (cx < 0 || cz < 0 || cx >= (1 << 15) || cz >= (1 << 15))
		{
			return 0;
		}
		int n = (cx << 15) + cz;
		if (n < 0)
		{
			return 0;
		}
		return n;
	}
}
