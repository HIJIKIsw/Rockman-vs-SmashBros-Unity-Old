using UnityEngine;
using System.Collections;

public class SlidingEffectController : MonoBehaviour {

    #region Inspector に表示するパラメータ
    // null
    #endregion

    #region 各コンポーネント
    SpriteRenderer SpriteRenderer;
    #endregion

    /// <summary>
    /// コンストラクタ
    /// </summary>
    void Start () {
        SpriteRenderer = GetComponent<SpriteRenderer>();

        // プレイヤーと同じ方向を向く
        GameObject PlayerSprite = GameObject.Find("Player/Sprite");
        if( PlayerSprite)
        {
            SpriteRenderer PlayerSpriteRenderer = PlayerSprite.GetComponent<SpriteRenderer>();
            SpriteRenderer.flipX = PlayerSpriteRenderer.flipX;
        }

    }

    /// <summary>
    /// エフェクトオブジェクトを削除する
    /// </summary>
    void Delete()
    {
        Destroy(this.gameObject);
    }
}
