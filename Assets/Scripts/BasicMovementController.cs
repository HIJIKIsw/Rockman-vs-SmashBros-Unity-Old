using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(BoxCollider2D))]
public class BasicMovementController : MonoBehaviour
{
    #region Inspector に表示するパラメータ
    public bool IsHitTerrain = true;            // 地形判定を行うかどうか
    public LayerMask PlatformLayerMask;         // 地形判定用のレイヤーマスク
    [Tooltip("下からのみすり抜け可能な当たり判定のレイヤーマスク")]
    public LayerMask OneWayPlatformLayerMask;   // 一方通行(下からのみすり抜け可能)
    [Tooltip("坂道地形の当たり判定のレイヤーマスク")]
    public LayerMask SlopeLayerMask;            // スロープ
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
    public Vector2 MoveDistance;                // 現在フレームで移動する量
    [HideInInspector]
    public bool IsAir;                          // 空中にいるかどうか
    #endregion

    #region 各コンポーネント
    BoxCollider2D BoxCollider2D;                // BoxCollider2D コンポーネント
    #endregion

    #region 構造体や定数
    private struct ColliderVertex               // 当たり判定の頂点 構造体
    {
        public Vector2 TopLeft;
        public Vector2 TopRight;
        public Vector2 BottomLeft;
        public Vector2 BottomRight;
    }
    #endregion

    /// <summary>
    /// コンストラクタ
    /// </summary>
    void Start()
    {
        BoxCollider2D = GetComponent<BoxCollider2D>();
    }

    /// <summary>
    /// ジャンプ開始
    /// </summary>
    public void Jump(float JumpSpeed)
    {
        // 頭上に地形があればジャンプしない
        ColliderVertex Vertex = GetColliderVertex();
        Ray2D HeadRay = new Ray2D(new Vector2(Vertex.TopLeft.x + ColliderSkin, Vertex.TopLeft.y + 0.5f), Vector2.right);
        float rayDistance = BoxCollider2D.size.x - (ColliderSkin * 2);
        RaycastHit2D RaycastHit = Physics2D.Raycast(HeadRay.origin, HeadRay.direction, rayDistance, SlopeLayerMask + PlatformLayerMask);
        if (!RaycastHit)
        {
            MoveDistance.y = JumpSpeed;
            IsAir = true;
        }
    }

    /// <summary>
    /// 左右の地形判定 (移動前にチェックし、壁に埋まりそうな場合は調整する)
    /// </summary>
    void HitCheckX()
    {
        ColliderVertex Vertex = GetColliderVertex();
        bool IsGoingRight = MoveDistance.x > 0;
        Ray2D[] Ray = new Ray2D[HorizontalRaycastNumber];
        float rayDistance = Mathf.Abs(MoveDistance.x);
        RaycastHit2D RaycastHit;
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
            // 通常の地形との接触
            RaycastHit = Physics2D.Raycast(Ray[i].origin, Ray[i].direction, rayDistance, PlatformLayerMask);
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
        // スロープの高さに合わせる
        if (!IsAir)
        {
            bool OnSlope = false;
            float NewBottomPosY = 0.0f;
            Ray2D[] SlopeRay = new Ray2D[2];
            RaycastHit2D[] SlopeRaycastHit = new RaycastHit2D[2];
            rayDistance = 8.0f;
            SlopeRay[0].direction = Vector2.down;            //左のRay
            SlopeRay[0].origin = new Vector2((Vertex.BottomLeft.x + ColliderSkin) + MoveDistance.x, Vertex.BottomLeft.y + 4.0f);
            SlopeRay[1].direction = Vector2.down;            //右のRay
            SlopeRay[1].origin = new Vector2((Vertex.BottomRight.x - ColliderSkin) + MoveDistance.x, Vertex.BottomRight.y + 4.0f);
            for (int i = 0; i < 2; i++)
            {
                SlopeRaycastHit[i] = Physics2D.Raycast(SlopeRay[i].origin, SlopeRay[i].direction, rayDistance, SlopeLayerMask + PlatformLayerMask);
                Debug.DrawRay(SlopeRay[i].origin, SlopeRay[i].direction * rayDistance, Color.cyan);
            }
            if (SlopeRaycastHit[0] && SlopeRaycastHit[1])
            {
                NewBottomPosY = Mathf.Max(SlopeRaycastHit[0].point.y, SlopeRaycastHit[1].point.y);
                OnSlope = true;
            }
            else if (SlopeRaycastHit[0])
            {
                NewBottomPosY = SlopeRaycastHit[0].point.y;
                OnSlope = true;
            }
            else if (SlopeRaycastHit[1])
            {
                NewBottomPosY = SlopeRaycastHit[1].point.y;
                OnSlope = true;
            }
            if (OnSlope)
            {
                float MoveDisY = NewBottomPosY - Vertex.BottomLeft.y;      //スロープに高さを合わせるために移動する量
                Vector2 NewTopLeft = new Vector2(Vertex.TopLeft.x + MoveDistance.x, Vertex.TopLeft.y + MoveDisY);
                Ray2D HeadRay = new Ray2D(NewTopLeft, Vector2.right);
                rayDistance = BoxCollider2D.size.x;
                RaycastHit = Physics2D.Raycast(HeadRay.origin, HeadRay.direction, rayDistance, SlopeLayerMask + PlatformLayerMask);
                if (!RaycastHit)
                {
                    SetPosY(NewBottomPosY + (transform.position.y - Vertex.BottomLeft.y));
                }
                else
                {
                    MoveDistance.x = 0.0f;
                }
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
                layerMask = PlatformLayerMask + OneWayPlatformLayerMask + SlopeLayerMask;
                Ray[i].origin = new Vector2(
                    (Vertex.BottomLeft.x + ColliderSkin) + (BoxCollider2D.size.x - ColliderSkin * 2) / (VerticalRaycastNumber - 1) * i,
                    Vertex.BottomLeft.y
                );
            }
            // 上方向の移動の場合
            else
            {
                layerMask = PlatformLayerMask + SlopeLayerMask;
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
        float rayDistance = 1.0f;
        ColliderVertex Vertex = GetColliderVertex();
        LayerMask layerMask;
        for (int i = 0; i < VerticalRaycastNumber; i++)
        {
            Ray[i].direction = Vector2.down;
            Ray[i].origin = new Vector2(
                (Vertex.BottomLeft.x + ColliderSkin) + (BoxCollider2D.size.x - ColliderSkin * 2) / (VerticalRaycastNumber - 1) * i,
                Vertex.BottomLeft.y
            );
            if (MoveDistance.y > 0.0f)
            {
                layerMask = PlatformLayerMask + SlopeLayerMask;
            }
            else
            {
                layerMask = PlatformLayerMask + OneWayPlatformLayerMask + SlopeLayerMask;
            }
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
    /// 指定した座標へ移動
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        transform.position = position;
    }

    /// <summary>
    /// 指定したX座標へ移動
    /// </summary>
    public void SetPosX(float posX)
    {
        transform.position = new Vector2(posX, transform.position.y);
    }

    /// <summary>
    /// 指定したY座標へ移動
    /// </summary>
    public void SetPosY(float posY)
    {
        transform.position = new Vector2(transform.position.x, posY);
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
        Vertex.BottomRight.y = BoxCollider2D.transform.position.y + BoxCollider2D.offset.y - BoxCollider2D.size.y / 2;
        return Vertex;
    }
}
