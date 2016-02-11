using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BasicMovementController))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerLadderController : MonoBehaviour
{
	private PlayerController PlayerController;              // PlayerController クラス
	private BasicMovementController BMController;           // BasicMovementController クラス
	private Animator Animator;                              // Animator コンポーネント
	private BoxCollider2D LadderGrabCheck;                  // はしごが掴める範囲内にあるか調べる当たり判定
	private BoxCollider2D LadderDownGrabCheck;              // はしごが足元(立っているマス)の掴める位置にあるか調べる当たり判定
	private BoxCollider2D LadderBendCheck;                  // はしごを掴んでいるとき、登りかけであるか調べる当たり判定
	private BoxCollider2D LadderFinishClimbingCheck;        // はしごを登り切ったときに、はしご上辺の座標を取得するための当たり判定

	private struct LayerMasks                               // はしごの各当たり判定のレイヤーマスク 構造体
	{
		public LayerMask Ladders;                           // はしごのレイヤーマスク
		public LayerMask LadderTops;                        // はしご上辺のレイヤーマスク
		public LayerMask LadderSpines;                      // はしご脊椎のレイヤーマスク
		public LayerMask LadderBottoms;                     // はしご下辺のレイヤーマスク
	}
	private LayerMasks layerMasks;

	private struct ColliderFourSide                         // 当たり判定の四辺 構造体
	{
		public float TopY;
		public float BottomY;
		public float LeftX;
		public float RightX;
	}

	public bool ControlEnable;                             // PlayerLadderController による操作受付が有効か

	[HideInInspector]
	public bool IsLadderClimbing;                           // はしごを掴んでいるかどうか

	[HideInInspector]
	public bool IsLadderBend;                                       // はしごを登りかけかどうか

	/// <summary>
	/// コンストラクタ
	/// </summary>
	void Start()
	{
		PlayerController = GetComponent<PlayerController>();
		BMController = GetComponent<BasicMovementController>();
		Animator = GetComponent<Animator>();

		layerMasks.Ladders = LayerMask.GetMask(new string[] { "Ladders" });
		layerMasks.LadderTops = LayerMask.GetMask(new string[] { "LadderTops" });
		layerMasks.LadderSpines = LayerMask.GetMask(new string[] { "LadderSpines" });
		layerMasks.LadderBottoms = LayerMask.GetMask(new string[] { "LadderBottoms" });

		LadderGrabCheck = transform.Find("LadderGrabCheck").GetComponent<BoxCollider2D>();
		LadderDownGrabCheck = transform.Find("LadderDownGrabCheck").GetComponent<BoxCollider2D>();
		LadderBendCheck = transform.Find("LadderBendCheck").GetComponent<BoxCollider2D>();
		LadderFinishClimbingCheck = transform.Find("LadderFinishClimbingCheck").GetComponent<BoxCollider2D>();
	}

	/// <summary>
	/// 描画ごとに呼ばれる
	/// </summary>
	void Update()
	{
		Operation();
	}

	/// <summary>
	/// 操作受付
	/// </summary>
	void Operation()
	{
		Vector2 Axis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
		// はしごを掴んでいる場合
		if (IsLadderClimbing)
		{
			//TODO: 以下は一時対処。次回のGitHubコミットまでに共通化する
			BMController.MoveDistance.y = 0.0f;
			Animator.speed = Mathf.Abs(Axis.y);
			// はしごを登りかけか調べる
			IsLadderBend = LadderTopsCheck(LadderBendCheck);
			// 昇降移動
			BMController.MoveDistance.y = 1.25f * Axis.y;
			// はしごが掴める範囲から存在しなくなった場合
			if (GrabCheck(LadderGrabCheck) == null)
			{
				FinishClimbingLadder(LadderFinishClimbingCheck);
			}
			// 下が押された、かつ 接地した場合
			else if (Axis.y < 0.0f && !BMController.IsAir)
			{
				LandingFromLadder(LadderFinishClimbingCheck);
			}
			// 上または下が押されていない、かつ ジャンプが押された場合
			else if (Axis.y == 0.0f && Input.GetButtonDown("Jump"))
			{
				ReleaseLadder();
			}
		}
		// はしごを掴んでいない場合
		else
		{
			// 接地していない場合
			if (BMController.IsAir)
			{
				// 上が押された場合
				if (Axis.y == 1.0f)
				{
					// はしごに重なっている場合
					EdgeCollider2D LadderSpine = GrabCheck(LadderGrabCheck);
					if (LadderSpine != null)
					{
						GrabLadder(LadderSpine);
					}
				}
			}
			// 接地している場合
			else
			{
				// 上が押された場合
				if (Axis.y == 1.0f)
				{
					// はしごに重なっている場合
					EdgeCollider2D LadderSpine = GrabCheck(LadderGrabCheck);
					if (LadderSpine != null)
					{
						GrabLadder(LadderSpine);
					}
				}
				// 下が押された場合
				else if(Axis.y == -1.0f)
				{
					// 足元にはしごがある場合
					EdgeCollider2D FootLadderSpine = GrabCheck(LadderDownGrabCheck);
					if (FootLadderSpine != null)
					{
						BMController.SetPosY(transform.position.y - 8.0f);
						IsLadderBend = true;
						GrabLadder(FootLadderSpine);
					}
				}
			}
		}
	}

	/// <summary>
	/// はしごに掴まる
	/// </summary>
	void GrabLadder(EdgeCollider2D LadderSpine)
	{
		IsLadderClimbing = true;
		IsLadderBend = LadderTopsCheck(LadderBendCheck);
		BMController.MoveDistance.y = 0.0f;
		BMController.MoveDistance.x = 0.0f;
		BMController.SetPosX(LadderSpine.points[0].x);

		PlayerController.ControlEnable = false;
		this.ControlEnable = true;
	}

	/// <summary>
	/// はしごから離れる
	/// </summary>
	void ReleaseLadder()
	{
		IsLadderClimbing = false;
		IsLadderBend = false;

		PlayerController.ControlEnable = true;
		this.ControlEnable = false;
		Animator.speed = 1.0f;
	}

	/// <summary>
	/// はしごを昇り切る
	/// </summary>
	/// このとき、指定した collider にはしごの上辺が触れている場合、その高さにちょうど立つように座標をリストアする。
	void FinishClimbingLadder(BoxCollider2D collider)
	{
		ColliderFourSide f = GetColliderFourSide(collider);
		Vector2 pointA = new Vector2(f.LeftX, f.TopY);
		Vector2 pointB = new Vector2(f.RightX, f.BottomY);
		Collider2D[] LadderTopColliders = new Collider2D[1];
		if (Physics2D.OverlapAreaNonAlloc(pointA, pointB, LadderTopColliders, layerMasks.LadderTops) > 0)
		{
			EdgeCollider2D LadderTop = (EdgeCollider2D)LadderTopColliders[0];
			BMController.SetPosY(LadderTop.points[0].y + 16.0f);
		}
		BMController.MoveDistance.y = 0.0f;
		IsLadderClimbing = false;
		IsLadderBend = false;
		PlayerController.ControlEnable = true;
		this.ControlEnable = false;
	}

	/// <summary>
	/// はしごを降り切る
	/// </summary>
	/// このとき、指定した collider にはしごの下辺が触れている場合、その高さにちょうど立つように座標をリストアする。
	void LandingFromLadder(BoxCollider2D collider)
	{
		ColliderFourSide f = GetColliderFourSide(collider);
		Vector2 pointA = new Vector2(f.LeftX, f.TopY);
		Vector2 pointB = new Vector2(f.RightX, f.BottomY);
		Collider2D[] LadderBottomColliders = new Collider2D[1];
		if (Physics2D.OverlapAreaNonAlloc(pointA, pointB, LadderBottomColliders, layerMasks.LadderBottoms) > 0)
		{
			EdgeCollider2D LadderBottom = (EdgeCollider2D)LadderBottomColliders[0];
			BMController.SetPosY(LadderBottom.points[0].y + 16.0f);
		}
		IsLadderClimbing = false;
		IsLadderBend = false;
		PlayerController.ControlEnable = true;
		this.ControlEnable = false;
	}

	/// <summary>
	/// 指定した collider にはしごの上辺が触れているかを返す
	/// </summary>
	bool LadderTopsCheck(BoxCollider2D collider)
	{
		ColliderFourSide f = GetColliderFourSide(collider);
		Vector2 pointA = new Vector2(f.LeftX, f.TopY);
		Vector2 pointB = new Vector2(f.RightX, f.BottomY);
		Collider2D[] LadderTopColliders = new Collider2D[1];
		return Physics2D.OverlapAreaNonAlloc(pointA, pointB, LadderTopColliders, layerMasks.LadderTops) > 0;
	}

	/// <summary>
	/// 指定した collider にはしごの下辺が触れているかを返す
	/// </summary>
	bool LadderBottomsCheck(BoxCollider2D collider)
	{
		ColliderFourSide f = GetColliderFourSide(collider);
		Vector2 pointA = new Vector2(f.LeftX, f.TopY);
		Vector2 pointB = new Vector2(f.RightX, f.BottomY);
		Collider2D[] LadderBottomColliders = new Collider2D[1];
		return Physics2D.OverlapAreaNonAlloc(pointA, pointB, LadderBottomColliders, layerMasks.LadderBottoms) > 0;
	}

	/// <summary>
	/// 指定した collider に触れているはしごの脊髄 (LadderSpine) の EdgeCollider2D を返す
	/// </summary>
	EdgeCollider2D GrabCheck(BoxCollider2D collider)
	{
		ColliderFourSide f = GetColliderFourSide(collider);
		Vector2 pointA = new Vector2(f.LeftX, f.TopY);
		Vector2 pointB = new Vector2(f.RightX, f.BottomY);
		Collider2D[] LadderGrabColliders = new Collider2D[1];
		if (Physics2D.OverlapAreaNonAlloc(pointA, pointB, LadderGrabColliders, layerMasks.LadderSpines) > 0)
		{
			return (EdgeCollider2D)LadderGrabColliders[0];
		}
		return null;
	}

	/// <summary>
	/// 当たり判定の4辺を取得する
	/// </summary>
	ColliderFourSide GetColliderFourSide(BoxCollider2D Collider)
	{
		ColliderFourSide FourSide = new ColliderFourSide();
		FourSide.TopY = Collider.transform.position.y + Collider.offset.y + Collider.size.y / 2;
		FourSide.BottomY = Collider.transform.position.y + Collider.offset.y - Collider.size.y / 2;
		FourSide.LeftX = Collider.transform.position.x + Collider.offset.x - Collider.size.x / 2;
		FourSide.RightX = Collider.transform.position.x + Collider.offset.x + Collider.size.x / 2;
		return FourSide;
	}
}
