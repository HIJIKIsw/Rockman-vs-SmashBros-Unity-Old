using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BasicMovementController))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerLadderController : MonoBehaviour
{
	private PlayerController PlayerController;              // PlayerController クラス
	private BasicMovementController BMController;           // BasicMovementController クラス
	private Animator Animator;								// Animator コンポーネント
	private BoxCollider2D LadderGrabCheck;                  // はしごが掴める範囲内にあるか調べる当たり判定
	private BoxCollider2D LadderDownGrabCheck;              // はしごが足元(立っているマス)の掴める位置にあるか調べる当たり判定

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

	public bool ControllEnable;                             // PlayerLadderController による操作受付が有効か

	[HideInInspector]
	public bool IsClimbing;                                 // はしごを掴んでいるかどうか

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
		if (IsClimbing)
		{
			//TODO: 以下は一時対処。次回のGitHubコミットまでに共通化する
			//TODO: 掴んだ瞬間のアニメーションがおかしい(一瞬ジャンプが再生される)のを修正する
			//		これに関しては、アニメーション管理を共通クラス化することで解決する？
			BMController.MoveDistance.y = 0.0f;
			Animator.SetBool("IsClimbing", true);
			// 上または下が押された場合
			if (Axis.y != 0.0f)
			{
				BMController.MoveDistance.y = 1.25f * Axis.y;
			}
			// ジャンプが押された場合
			if (Input.GetButtonDown("Jump"))
			{
				ReleaseLadder();
			}
			// 接地している場合
			if (!BMController.IsAir)
			{
				ReleaseLadder();
			}
		}
		// はしごを掴んでいない場合
		else
		{
			Animator.SetBool("IsClimbing", false);
			// 上または下が押された場合
			if (Axis.y != 0.0f)
			{
				// 接地していない場合
				if (BMController.IsAir)
				{
					// はしごに重なっている場合
					EdgeCollider2D LadderSpine = GrabCheck(LadderGrabCheck);
					if (LadderSpine != null)
					{
						GrabLadder(LadderSpine);
					}
				}
				// 接地している場合
				else
				{
					// 押されたのが上だった場合
					if (Axis.y == 1.0f)
					{
						// はしごに重なっている場合
						EdgeCollider2D LadderSpine = GrabCheck(LadderGrabCheck);
						if (LadderSpine != null)
						{
							GrabLadder(LadderSpine);
						}
					}
					// 押されたのが下だった場合
					else
					{
						// 足元にはしごがある場合
						EdgeCollider2D FootLadderSpine = GrabCheck(LadderDownGrabCheck);
						if (FootLadderSpine != null)
						{
							BMController.SetPosY(transform.position.y - 8.0f);
							GrabLadder(FootLadderSpine);
						}
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
		IsClimbing = true;
		BMController.MoveDistance.x = 0.0f;
		BMController.SetPosX(LadderSpine.points[0].x);

		PlayerController.ControllEnable = false;
		this.ControllEnable = true;
	}

	/// <summary>
	/// はしごから離れる
	/// </summary>
	void ReleaseLadder()
	{
		IsClimbing = false;
		PlayerController.ControllEnable = true;
		this.ControllEnable = false;
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
