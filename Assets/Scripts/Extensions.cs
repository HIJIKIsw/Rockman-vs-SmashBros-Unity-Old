using UnityEngine;
using System.Collections;

public static class Extensions
{

	public static void SetPosX(this Transform me, float PosX)
	{

		var newPos = me.transform.position;
		newPos.x = PosX;
		me.transform.position = newPos;

	}

	public static void SetPosY(this Transform me, float PosY)
	{

		var newPos = me.transform.position;
		newPos.y = PosY;
		me.transform.position = newPos;

	}

	public static void SetPosZ(this Transform me, float PosZ)
	{

		var newPos = me.transform.position;
		newPos.z = PosZ;
		me.transform.position = newPos;

	}

	public static void Set2D(this Transform me, float PosX, float PosY)
	{
		var newPos = me.transform.position;
		newPos.x = PosX;
		newPos.y = PosY;
		me.transform.position = newPos;

	}

	public static void Set2D(this Transform me, Vector2 Pos)
	{
		me.Set2D(Pos.x, Pos.y);
	}

}