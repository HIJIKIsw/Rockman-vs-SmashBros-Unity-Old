using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(BasicMovementController))]
public class PlayerController : MonoBehaviour
{
    #region Inspector に表示するパラメータ
    // なし
    #endregion

    #region Inspector に表示しないパラメータ
    private float WalkSpeed;                            // 左右キーによる移動の加速度
    private float WalkSpeedMax;                         // 左右キーによる移動の最大速度
    private float JumpSpeed;                            // ジャンプの初速
    private float SlidingSpeed;                         // スライディングの速度
    private StateName OperationState;					// 動作の状態
    #endregion

    #region 各コンポーネント
    private BasicMovementController BMController;       // BasicMovementController クラス	
    private BoxCollider2D BoxCollider2D;                // BoxCollider2D コンポーネント
    private SpriteRenderer SpriteRenderer;              // SpriteRenderer コンポーネント
    private Animator Animator;                          // Animator コンポーネント
    #endregion

    #region 子オブジェクト
    private BoxCollider2D LadderGrabCheck;              // はしごが掴める範囲内にあるか調べる当たり判定
    private BoxCollider2D LadderDownGrabCheck;          // はしごが足元(立っているマス)の掴める位置にあるか調べる当たり判定
    private BoxCollider2D LadderBendCheck;              // はしごを掴んでいるとき、登りかけであるか調べる当たり判定
    private BoxCollider2D LadderFinishClimbingCheck;    // はしごを登り切ったときに、はしご上辺の座標を取得するための当たり判定
    #endregion

    #region 構造体や定数
    enum StateName
    {
        Neutral = 0,
        Walking,
        Jump,
        Sliding,
        LadderClimbing
    };
    private struct ColliderFourSide                     // 当たり判定の四辺 構造体
    {
        public float TopY;
        public float BottomY;
        public float LeftX;
        public float RightX;
    }
    private struct LayerMasks                           // はしごの各当たり判定のレイヤーマスク 構造体
    {
        public LayerMask Ladders;                       // はしごのレイヤーマスク
        public LayerMask LadderTops;                    // はしご上辺のレイヤーマスク
        public LayerMask LadderSpines;                  // はしご脊椎のレイヤーマスク
        public LayerMask LadderBottoms;                 // はしご下辺のレイヤーマスク
    }
    private LayerMasks layerMasks;
    #endregion

    /// <summary>
    /// コンストラクタ
    /// </summary>
    void Start()
    {
        BMController = GetComponent<BasicMovementController>();
        Animator = transform.FindChild("Sprite").GetComponent<Animator>();
        SpriteRenderer = transform.FindChild("Sprite").GetComponent<SpriteRenderer>();
        BoxCollider2D = GetComponent<BoxCollider2D>();

        layerMasks.Ladders = LayerMask.GetMask(new string[] { "Ladders" });
        layerMasks.LadderTops = LayerMask.GetMask(new string[] { "LadderTops" });
        layerMasks.LadderSpines = LayerMask.GetMask(new string[] { "LadderSpines" });
        layerMasks.LadderBottoms = LayerMask.GetMask(new string[] { "LadderBottoms" });

        LadderGrabCheck = transform.Find("LadderGrabCheck").GetComponent<BoxCollider2D>();
        LadderDownGrabCheck = transform.Find("LadderDownGrabCheck").GetComponent<BoxCollider2D>();
        LadderBendCheck = transform.Find("LadderBendCheck").GetComponent<BoxCollider2D>();
        LadderFinishClimbingCheck = transform.Find("LadderFinishClimbingCheck").GetComponent<BoxCollider2D>();

        OperationState = StateName.Neutral;
        WalkSpeed = 1.25f;
        WalkSpeedMax = 1.25f;
        JumpSpeed = 5.0f;
        SlidingSpeed = 3.0f;
    }

    /// <summary>
    /// 描画毎に呼ばれる
    /// </summary>
    void Update()
    {
        // 通常の操作
        if (OperationState == StateName.Neutral || OperationState == StateName.Walking || OperationState == StateName.Jump)
        {
            StandardOperation();
        }
        // スライディング中の操作
        else if (OperationState == StateName.Sliding)
        {
            SlidingOperation();
        }
        // はしご掴まり中の操作
        else if (OperationState == StateName.LadderClimbing)
        {
            LadderOperation();
        }

        // 当たり判定管理
        ColliderManagement();

        // 移動量反映
        BMController.Calc();

        // アニメーション管理
        AnimationManagement();
    }

    /// <summary>
    /// 通常の操作受付
    /// </summary>
    /// 左右移動, ジャンプ開始, スライディング開始, はしごに掴まる
    void StandardOperation()
    {
        // 十字キー入力状態の取得
        Vector2 Axis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        // スライディング開始
        if (Axis.y == -1.0f && Input.GetButtonDown("Jump") && !BMController.IsAir)
        {
            OperationState = StateName.Sliding;
        }
        else
        {
            // ジャンプ開始
            if (Input.GetButtonDown("Jump") && !BMController.IsAir)
            {
                BMController.Jump(JumpSpeed);
                OperationState = StateName.Jump;
            }
            // 左右移動
            if (Axis.x != 0)
            {
                float Direction = Axis.x;
                SpriteRenderer.flipX = Direction < 0;
                BMController.MoveDistance.x = Mathf.Min(WalkSpeedMax, Mathf.Max(-WalkSpeedMax, BMController.MoveDistance.x += WalkSpeed * Direction));
                OperationState = StateName.Walking;
            }
            // 左右キーが入力されていない場合は移動をやめる
            else
            {
                BMController.MoveDistance.x = 0;
                OperationState = StateName.Neutral;
            }
            // 接地していない場合
            if (BMController.IsAir)
            {
                OperationState = StateName.Jump;
                // 上または下が押された場合
                if (Axis.y != 0.0f)
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
                        BMController.SetPosY(transform.position.y + 2.0f);
                        GrabLadder(LadderSpine);
                    }
                }
                // 下が押された場合
                else if (Axis.y == -1.0f)
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

    /// <summary>
    /// スライディング中の操作受付
    /// </summary>
    void SlidingOperation()
    {
        // 向いている方向に進み続ける
        float direction = SpriteRenderer.flipX ? -1.0f : 1.0f;
        BMController.MoveDistance.x = SlidingSpeed * direction;

        // 壁に衝突した場合
        if(BMController.HitTerrain.Left || BMController.HitTerrain.Right)
        {
            OperationState = StateName.Neutral;
        }

        //足元に地形がなくなった場合
        if (BMController.IsAir)
        {
            OperationState = StateName.Jump;
        }
    }

    /// <summary>
    /// はしご掴まり中の操作受付
    /// </summary>
    /// 上下移動, はしごを離す
    void LadderOperation()
    {
        // 十字キー入力状態の取得
        Vector2 Axis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        BMController.MoveDistance.y = 0.0f;
        Animator.speed = Mathf.Abs(Axis.y);
        // 昇降移動
        BMController.MoveDistance.y = 1.25f * Axis.y;
        // 上が押されている、かつ 一定範囲にはしごの上辺が触れた場合
        if (Axis.y == 1.0f && LadderTopsCheck(LadderFinishClimbingCheck))
        {
            FinishClimbingLadder(LadderFinishClimbingCheck);
        }
        // 接地した場合
        else if (!BMController.IsAir)
        {
            LandingFromLadder(LadderFinishClimbingCheck);
        }
        // はしごが掴める範囲から存在しなくなった場合
        else if (GrabCheck(LadderGrabCheck) == null)
        {
            ReleaseLadder();
        }
        // 上または下が押されていない、かつ ジャンプが押された場合
        else if (Axis.y == 0.0f && Input.GetButtonDown("Jump"))
        {
            ReleaseLadder();
        }
    }

    /// <summary>
    /// はしごに掴まる
    /// </summary>
    void GrabLadder(EdgeCollider2D LadderSpine)
    {
        BMController.MoveDistance.y = 0.0f;
        BMController.MoveDistance.x = 0.0f;
        BMController.SetPosX(LadderSpine.points[0].x);

        OperationState = StateName.LadderClimbing;
    }

    /// <summary>
    /// はしごから離れる
    /// </summary>
    void ReleaseLadder()
    {
        OperationState = StateName.Jump;
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
        OperationState = StateName.Neutral;
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
        OperationState = StateName.Neutral;
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
    /// アニメーション管理
    /// </summary>
    void AnimationManagement()
    {
        bool IsWalking = OperationState == StateName.Walking ? true : false;
        bool IsJumping = OperationState == StateName.Jump ? true : false;
        bool IsLadderClimbing = OperationState == StateName.LadderClimbing ? true : false;
        bool IsLadderBend = LadderTopsCheck(LadderBendCheck);
        bool IsSliding = OperationState == StateName.Sliding ? true : false;

        Animator.SetBool("IsWalking", IsWalking);
        Animator.SetBool("IsJumping", IsJumping);
        Animator.SetBool("IsLadderClimbing", IsLadderClimbing);
        Animator.SetBool("IsLadderBend", IsLadderBend);
        Animator.SetBool("IsSliding", IsSliding);
    }

    /// <summary>
    /// 当たり判定のサイズおよび位置管理
    /// </summary>
    void ColliderManagement()
    {
        Vector2 size = new Vector2(),
                offset = new Vector2();

        // 立ち、歩き、ジャンプ、はしご掴まり
        if (OperationState == StateName.Neutral || OperationState == StateName.Walking || OperationState == StateName.Jump || OperationState == StateName.LadderClimbing)
        {
            size = new Vector2(16.0f, 24.0f);
            offset = new Vector2(0.0f, -4.0f);
        }
        // スライディング中
        else if (OperationState == StateName.Sliding)
        {
            size = new Vector2(16.0f, 12.0f);
            offset = new Vector2(0.0f, -10.0f);
        }

        BoxCollider2D.size = size;
        BoxCollider2D.offset = offset;
    }

    /// <summary>
    /// 当たり判定の4辺座標を取得する
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