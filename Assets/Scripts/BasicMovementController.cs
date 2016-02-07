using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(BoxCollider2D))]
public class BasicMovementController : MonoBehaviour
{
	BoxCollider2D BoxCollider2D;                // BoxCollider2D コンポーネント

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

	[Range(2, 20)]
	public int HorizontalRaycastNumber = 3;     // 地形判定に使用する水平の Raycast の本数
	[Range(2, 20)]
	public int VerticalRaycastNumber = 2;       // 地形判定に使用する垂直の Raycast の本数

	private float ColliderSkin = 0.001f;        // ピクセルの縁同士で触れたことにならないための判定の遊び

	// コンストラクタ
	void Start()
	{
		BoxCollider2D = GetComponent<BoxCollider2D>();
		SetPlatformLayerMask(new string[] { "Platform" });
	}

	// 描画ごとに呼ばれる
	void Update()
	{
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
	/// 左右の地形判定 (移動前にチェックし、壁に埋まりそうな場合は調整する)
	/// </summary>
	void HitCheckX()
	{
		bool IsGoingRight = MoveDistance.x > 0;
		Ray2D[] Ray = new Ray2D[HorizontalRaycastNumber];
		float rayDistance = Mathf.Abs(MoveDistance.x);
		ColliderVertex Vertex = GetColliderVertex();
		for (int i = 0; i < HorizontalRaycastNumber; i++)
		{
			Ray[i].direction = IsGoingRight ? Vector2.right : Vector2.left;
			// 右方向の移動の場合
			if (IsGoingRight)
			{
				Ray[i].origin = new Vector2(
					Vertex.TopRight.x,
					(Vertex.TopRight.y - ColliderSkin) - (BoxCollider2D.size.y - ColliderSkin * 2) / (HorizontalRaycastNumber - 1) * i
				);
			}
			else
			{
				Ray[i].origin = new Vector2(
					Vertex.TopLeft.x,
					(Vertex.TopLeft.y - ColliderSkin) - (BoxCollider2D.size.y - ColliderSkin * 2) / (HorizontalRaycastNumber - 1) * i
				);

			}
			RaycastHit2D RaycastHit = Physics2D.Raycast(Ray[i].origin, Ray[i].direction, rayDistance, PlatformLayerMask);
			if (RaycastHit)
			{
				Debug.DrawRay(Ray[i].origin, Ray[i].direction * rayDistance, Color.red);
				// 触れた判定の中でより近いものと接触したことにする
				if (Mathf.Abs(MoveDistance.x) > Mathf.Abs(RaycastHit.point.x - Ray[i].origin.x))
				{
					MoveDistance.x = RaycastHit.point.x - Ray[i].origin.x;
				}
			}
			else
			{
				Debug.DrawRay(Ray[i].origin, Ray[i].direction * rayDistance, Color.blue);
			}
		}
	}

	/// <summary>
	/// 上下の地形判定 (移動前にチェックし、壁に埋まりそうな場合は調整する)
	/// </summary>
	void HitCheckY()
	{
		IsAir = true;
		bool IsGoingDown = MoveDistance.y < 0;
		Ray2D[] Ray = new Ray2D[VerticalRaycastNumber];
		float rayDistance = Mathf.Abs(MoveDistance.y);
		ColliderVertex Vertex = GetColliderVertex();
		for (int i = 0; i < VerticalRaycastNumber; i++)
		{
			Ray[i].direction = IsGoingDown ? Vector2.down : Vector2.up;
			// 下方向の移動の場合
			if (IsGoingDown)
			{
				Ray[i].origin = new Vector2(
					(Vertex.BottomLeft.x + ColliderSkin) + (BoxCollider2D.size.x - ColliderSkin * 2) / (VerticalRaycastNumber - 1) * i,
					Vertex.BottomLeft.y
				);
			}
			else
			{
				Ray[i].origin = new Vector2(
					(Vertex.TopLeft.x + ColliderSkin) + (BoxCollider2D.size.x - ColliderSkin * 2) / (VerticalRaycastNumber - 1) * i,
					Vertex.TopLeft.y
				);
			}
			RaycastHit2D RaycastHit = Physics2D.Raycast(Ray[i].origin, Ray[i].direction, rayDistance, PlatformLayerMask);
			if (RaycastHit)
			{
				Debug.DrawRay(Ray[i].origin, Ray[i].direction * rayDistance, Color.red);
				// 触れた判定の中でより近いものと接触したことにする
				if (Mathf.Abs(MoveDistance.y) > Mathf.Abs(RaycastHit.point.y - Ray[i].origin.y))
				{
					MoveDistance.y = RaycastHit.point.y - Ray[i].origin.y;
				}
				// 下方向の移動だった場合空中フラグをOFF
				if (IsGoingDown)
				{
					IsAir = false;
				}
			}
			else
			{
				Debug.DrawRay(Ray[i].origin, Ray[i].direction * rayDistance, Color.blue);
			}
		}
	}

	/// <summary>
	/// 足元に地形があるか判定
	/// </summary>
	void IsAirCheck()
	{
		IsAir = true;
		Ray2D[] Ray = new Ray2D[VerticalRaycastNumber];
		float rayDistance = 1.0f;
		ColliderVertex Vertex = GetColliderVertex();
		for (int i = 0; i < VerticalRaycastNumber; i++)
		{
			Ray[i].direction = Vector2.down;
			Ray[i].origin = new Vector2(
				(Vertex.BottomLeft.x + ColliderSkin) + (BoxCollider2D.size.x - ColliderSkin * 2) / (VerticalRaycastNumber - 1) * i,
				Vertex.BottomLeft.y
			);
			RaycastHit2D RaycastHit = Physics2D.Raycast(Ray[i].origin, Ray[i].direction, rayDistance, PlatformLayerMask);
			if (RaycastHit)
			{
				Debug.DrawRay(Ray[i].origin, Ray[i].direction * rayDistance, Color.red);
				IsAir = false;
				break;
			}
			else
			{
				Debug.DrawRay(Ray[i].origin, Ray[i].direction * rayDistance, Color.blue);
			}
		}
	}

	/// <summary>
	/// 計算処理
	/// </summary>
	public void Calc(bool HitCheckFlag, bool AddGravityFlag)
	{
		if (MoveDistance.x != 0.0f)
		{
			if (HitCheckFlag) { HitCheckX(); }
			MoveX();
		}
		if (MoveDistance.y != 0.0f)
		{
			if (HitCheckFlag) { HitCheckY(); }
			MoveY();
		}
		if (HitCheckFlag && !IsAir) { IsAirCheck(); }
		if (AddGravityFlag && IsAir) { MoveDistance.y += Gravity; }
	}

	/// <summary>
	/// X座標の移動量を反映
	/// </summary>
	private void MoveX()
	{
		transform.SetPosX(transform.position.x + MoveDistance.x);
	}

	/// <summary>
	/// Y座標の移動量を反映
	/// </summary>
	private void MoveY()
	{
		transform.SetPosY(transform.position.y + MoveDistance.y);
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
