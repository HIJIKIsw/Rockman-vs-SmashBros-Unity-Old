using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BasicMovementController : MonoBehaviour
{
	BoxCollider2D BoxCollider2D;                // BoxCollider2D コンポーネント
	Rigidbody2D Rigidbody2D;                    // Rigidbody2D コンポーネント

	private struct ColliderVertex               // 当たり判定の頂点 構造体
	{
		public Vector2 TopLeft;
		public Vector2 TopRight;
		public Vector2 BottomLeft;
		public Vector2 BottomRight;
	}

	[HideInInspector]
	public bool IsAir;                          // 空中にいるかどうか
	[Range(-16.0f, 0.0f)]
	public float Gravity = -0.25f;               // 1フレーム毎にかかる重力
	[HideInInspector]
	public int PlatformLayerMask;               // 地形判定用のレイヤーマスク

	[HideInInspector]
	public Vector2 MoveDistance;                // 現在フレームで移動する量

	[Range(0.001f, 1.0f)]
	public float ColliderSkin;                  // ピクセルの縁同士で触れたことにならないための判定の遊び

	/// <summary>
	/// コンストラクタ
	/// </summary>
	void Start()
	{
		Rigidbody2D = GetComponent<Rigidbody2D>();
		BoxCollider2D = GetComponent<BoxCollider2D>();
		SetPlatformLayerMask(new string[] { "Platform" });
	}

	/// <summary>
	/// 計算処理
	/// </summary>
	public void Calc(bool HitCheckFlag, bool AddGravityFlag)
	{
		// 移動量反映
		Rigidbody2D.MovePosition(transform.position + (Vector3)MoveDistance);
		if (HitCheckFlag) { IsAirCheck(); }
		if (AddGravityFlag && IsAir) { MoveDistance.y += Gravity; }
	}

	/// <summary>
	/// ジャンプ開始
	/// </summary>
	public void Jump(float JumpSpeed)
	{
		MoveDistance.y = JumpSpeed;
		IsAir = true;
	}

	/// <summary>
	/// 足元に地形があるか判定
	/// </summary>
	private void IsAirCheck()
	{
		// X は ColliderSkin を加算し左右にはみ出さないように計算済み。なので Mathf.Round() してはいけない。
		// Y は Rigidbody2D の仕様上、地面から 0.015unit 程度(場合による)浮くため、Mathf.Round() してキリのいい数字にしてあげる。
		Vector2 rayOrigin = new Vector2(
			BoxCollider2D.transform.position.x + BoxCollider2D.offset.x - BoxCollider2D.size.x / 2 + ColliderSkin,
			Mathf.Round(BoxCollider2D.transform.position.y) + BoxCollider2D.offset.y - BoxCollider2D.size.y / 2
			);
		Vector2 rayDirection = Vector2.right;
		float rayDistance = BoxCollider2D.size.x - ColliderSkin * 2;
		RaycastHit2D RaycastHit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, PlatformLayerMask);
		if (RaycastHit)
		{
			Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red);
			MoveDistance.y = 0.0f;
			IsAir = false;
		}
		else
		{
			Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.blue);
			IsAir = true;
		}
	}

	/// <summary>
	/// 地形判定のレイヤーマスクを設定
	/// </summary>
	public void SetPlatformLayerMask(string[] LayerNames)
	{
		PlatformLayerMask = LayerMask.GetMask(LayerNames);
	}

	/// <summary>
	/// 当たり判定の頂点を取得
	/// </summary>
	private ColliderVertex GetColliderVertex()
	{
		ColliderVertex Vertex = new ColliderVertex();
		Vertex.TopLeft.x = BoxCollider2D.transform.position.x + BoxCollider2D.offset.x - BoxCollider2D.size.x / 2;
		Vertex.TopLeft.y = BoxCollider2D.transform.position.y + BoxCollider2D.offset.y + BoxCollider2D.size.y / 2;
		Vertex.TopRight.x = BoxCollider2D.transform.position.x + BoxCollider2D.offset.x + BoxCollider2D.size.x / 2;
		Vertex.TopRight.y = BoxCollider2D.transform.position.y + BoxCollider2D.offset.y + BoxCollider2D.size.y / 2;
		Vertex.BottomLeft.x = BoxCollider2D.transform.position.x + BoxCollider2D.offset.x - BoxCollider2D.size.x / 2;
		Vertex.BottomLeft.y = BoxCollider2D.transform.position.y + BoxCollider2D.offset.y - BoxCollider2D.size.y / 2;
		Vertex.BottomRight.x = BoxCollider2D.transform.position.x + BoxCollider2D.offset.x + BoxCollider2D.size.x / 2;
		Vertex.BottomRight.y = BoxCollider2D.transform.position.x + BoxCollider2D.offset.y - BoxCollider2D.size.y / 2;
		return Vertex;
	}
}
