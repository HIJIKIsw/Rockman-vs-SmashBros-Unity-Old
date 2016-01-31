using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
	private float WalkSpeed;                    // 左右キーによる移動の加速度
	private float WalkSpeedMax;					// 左右キーによる移動の最大速度

	private Vector3 MovingDistance;             // 現在のフレームでの移動量

	private BoxCollider2D BoxCollider2D;        // BoxCollider2D コンポーネント
	private Animator Animator;                  // Animator コンポーネント
	private SpriteRenderer SpriteRenderer;      // SpriteRenderer コンポーネント

	// Use this for initialization
	void Start()
	{
		WalkSpeed = 1.25f;
		WalkSpeedMax = 1.25f;
		MovingDistance = new Vector3(0, 0);

		BoxCollider2D = GetComponent<BoxCollider2D>();
		Animator = GetComponent<Animator>();
		SpriteRenderer = GetComponent<SpriteRenderer>();
	}

	// Update is called once per frame
	void Update()
	{
		// 左右移動
		Walk();

		// 移動量反映
		Move();
	}

	// 左右移動
	void Walk()
	{
		// 左または右のキーが入力されている場合
		if (Input.GetKey(KeyCode.LeftArrow))
		{
			SpriteRenderer.flipX = true;
			Animator.SetBool("IsWalk", true);
			MovingDistance.x = Mathf.Min(WalkSpeedMax, Mathf.Max(-1.25f, MovingDistance.x -= WalkSpeed));
		}
		else if (Input.GetKey(KeyCode.RightArrow))
		{
			SpriteRenderer.flipX = false;
			Animator.SetBool("IsWalk", true);
			MovingDistance.x = Mathf.Min(WalkSpeedMax, Mathf.Max(-1.25f, MovingDistance.x += WalkSpeed));
		}

		// 左右のキーが入力されていない場合
		if (!Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
		{
			Animator.SetBool("IsWalk", false);
			MovingDistance.x = 0;
		}
	}

	// 移動量反映
	void Move()
	{
		transform.position += MovingDistance;

	}

	void OnCollisionStay2D(Collision2D coll)
	{
		GameObject obj = coll.gameObject;
		if (obj.tag == "Enemy") {
			Vector3 pos = new Vector3(obj.transform.position.x, obj.transform.position.y+1f);
			obj.transform.position = pos;
		}
	}

}