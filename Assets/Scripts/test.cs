using UnityEngine;
using System.Collections;

public class test : MonoBehaviour
{

	[Range(2, 20)]
	public float Speed = 5;

	// Use this for initialization
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{
		// 右・左
		float x = Input.GetAxisRaw("Horizontal");

		// 上・下
		float y = Input.GetAxisRaw("Vertical");

		float sx = transform.position.x;
		float sy = transform.position.y;

		float dx = sx + x * Speed;
		float dy = sy + y * Speed;

		// 移動する向きを求める
		//Vector2 direction = new Vector2(x, y).normalized;

		GetComponent<Rigidbody2D>().MovePosition(new Vector2(dx, dy));

		//GetComponent<Rigidbody2D>().velocity = direction * Speed;
	}
}
