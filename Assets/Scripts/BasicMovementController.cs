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

	#region Inspector に表示するパラメータ
	public bool IsHitTerrain = true;            // 地形判定を行うかどうか
	public LayerMask PlatformLayerMask;         // 地形判定用のレイヤーマスク
	[Tooltip("下からのみすり抜け可能な当たり判定のレイヤーマスク")]
	public LayerMask OneWayPlatformLayerMask;	// 一方通行(下からのみすり抜け可能)
	[Range(-16.0f, 0.0f)]
	public float Gravity = -0.25f;              // 1フレーム毎にかかる重力
	[Range(2, 20)]
	public int HorizontalRaycastNumber = 4;     // 地形判定に使用する水平の Raycast の本数
	[Range(2, 20)]
	public int VerticalRaycastNumber = 3;       // 地形判定に使用する垂直の Raycast の本数
	[Range(0.001f, 1.0f)]
	public float ColliderSkin = 0.001f;         // ピクセルの縁同士で触れたことにならないための判定の遊び
	#endregion

	#region Inspector に表示しないパラメータ
	[HideInInspector]
	public Vector2 InternalPosition;            // 内部座標
	[HideInInspector]
	public Vector2 MoveDistance;                // 現在フレームで移動する量
	[HideInInspector]
	public bool IsAir;                          // 空中にいるかどうか
	#endregion

	/// <summary>
	/// コンストラクタ
	/// </summary>
	void Start()
	{
		BoxCollider2D = GetComponent<BoxCollider2D>();

		// 内部座標を初期化
		InternalPosition = transform.position;

		// 初期位置を整数に制限
		transform.SetPosX(Mathf.Round(InternalPosition.x));
		transform.SetPosY(Mathf.Round(InternalPosition.y));
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
			LayerMask layerMask;
			Ray[i].direction = IsGoingDown ? Vector2.down : Vector2.up;
			// 下方向の移動の場合
			if (IsGoingDown)
			{
				layerMask = PlatformLayerMask + OneWayPlatformLayerMask;
				Ray[i].origin = new Vector2(
					(Vertex.BottomLeft.x + ColliderSkin) + (BoxCollider2D.size.x - ColliderSkin * 2) / (VerticalRaycastNumber - 1) * i,
					Vertex.BottomLeft.y
				);
			}
			// 上方向の移動の場合
			else
			{
				layerMask = PlatformLayerMask;
				Ray[i].origin = new Vector2(
					(Vertex.TopLeft.x + ColliderSkin) + (BoxCollider2D.size.x - ColliderSkin * 2) / (VerticalRaycastNumber - 1) * i,
					Vertex.TopLeft.y
				);
			}
			RaycastHit2D RaycastHit = Physics2D.Raycast(Ray[i].origin, Ray[i].direction, rayDistance, layerMask);
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
		float rayDistance = 0.5f;
		ColliderVertex Vertex = GetColliderVertex();
		for (int i = 0; i < VerticalRaycastNumber; i++)
		{
			Ray[i].direction = Vector2.down;
			Ray[i].origin = new Vector2(
				(Vertex.BottomLeft.x + ColliderSkin) + (BoxCollider2D.size.x - ColliderSkin * 2) / (VerticalRaycastNumber - 1) * i,
				Vertex.BottomLeft.y
			);
			LayerMask layerMask = PlatformLayerMask + OneWayPlatformLayerMask;
			RaycastHit2D RaycastHit = Physics2D.Raycast(Ray[i].origin, Ray[i].direction, rayDistance, layerMask);
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
	public void Calc()
	{
		#region DebugCode: Unity エディター上での動作の場合、内部座標が本座標と異なった場合は取得しなおす
#if UNITY_EDITOR
		Vector2 RoundedPos = new Vector2(Mathf.Round(InternalPosition.x), Mathf.Round(InternalPosition.y));
		if( RoundedPos != (Vector2)transform.position)
		{
			RoundedPos = new Vector2(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y));
			InternalPosition = RoundedPos;
			transform.position = RoundedPos;
		}
#endif
		#endregion

		if (MoveDistance.x != 0.0f)
		{
			if (IsHitTerrain) { HitCheckX(); }
			MoveX();
		}
		if (MoveDistance.y != 0.0f)
		{
			if (IsHitTerrain) { HitCheckY(); }
			MoveY();
		}
		if (IsHitTerrain) { IsAirCheck(); }
		if (IsAir) { MoveDistance.y += Gravity; }
	}

	/// <summary>
	/// X座標の移動量を反映
	/// </summary>
	private void MoveX()
	{
		InternalPosition.x += MoveDistance.x;
		transform.SetPosX(Mathf.Round(InternalPosition.x));
	}

	/// <summary>
	/// Y座標の移動量を反映
	/// </summary>
	private void MoveY()
	{
		InternalPosition.y += MoveDistance.y;
		transform.SetPosY(Mathf.Round(InternalPosition.y));
	}

	/// <summary>
	/// 指定した座標へ移動
	/// </summary>
	public void SetPosition(Vector2 position)
	{
		InternalPosition = position;
		transform.position = new Vector2(Mathf.Round(InternalPosition.x), Mathf.Round(InternalPosition.y));
	}

	/// <summary>
	/// 指定したX座標へ移動
	/// </summary>
	public void SetPosX(float posX)
	{
		InternalPosition.x = posX;
		transform.position = new Vector2(Mathf.Round(InternalPosition.x), Mathf.Round(InternalPosition.y));
	}

	/// <summary>
	/// 指定したY座標へ移動
	/// </summary>
	public void SetPosY(float posY)
	{
		InternalPosition.y = posY;
		transform.position = new Vector2(Mathf.Round(InternalPosition.x), Mathf.Round(InternalPosition.y));
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
