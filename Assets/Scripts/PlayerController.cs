using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(BasicMovementController))]
public class PlayerController : MonoBehaviour
{
	private BasicMovementController BMController;	// BasicMovementController コンポーネント	
	private BoxCollider2D BoxCollider2D;			// BoxCollider2D コンポーネント
	private Animator Animator;						// Animator コンポーネント
	private SpriteRenderer SpriteRenderer;			// SpriteRenderer コンポーネント

	private float WalkSpeed;						// 左右キーによる移動の加速度
	private float WalkSpeedMax;						// 左右キーによる移動の最大速度
	private float JumpSpeed;						// ジャンプの初速

	// コンストラクタ
	void Start()
	{
		BMController = GetComponent<BasicMovementController>();
		BoxCollider2D = GetComponent<BoxCollider2D>();
		Animator = GetComponent<Animator>();
		SpriteRenderer = GetComponent<SpriteRenderer>();

		WalkSpeed = 1.25f;
		WalkSpeedMax = 1.25f;
		JumpSpeed = 5.0f;
	}

	// 描画ごとに毎フレーム呼ばれる
	void Update()
	{
		// 入力受付
		Operation();

		// 移動量反映
		BMController.Calc(true, true);
	}

	// 入力受付
	void Operation()
	{
		// ジャンプ開始
		if (Input.GetButtonDown("Jump") && !BMController.IsAir)
		{
			BMController.Jump(JumpSpeed);
		}
		//TODO:連続で移動を開始した場合に、移動遷移アニメーションがきちんと再生されない不具合を修正する
		// 左右移動
		float Direction = Input.GetAxis("Horizontal");
		if (Direction != 0)
		{
			SpriteRenderer.flipX = Direction < 0;
			Animator.SetBool("IsWalk", true);
			BMController.MoveDistance.x = Mathf.Min(WalkSpeedMax, Mathf.Max(-WalkSpeedMax, BMController.MoveDistance.x += WalkSpeed * Direction));
		}
		// 左右キーが入力されていない場合に止める
		else
		{
			Animator.SetBool("IsWalk", false);
			BMController.MoveDistance.x = 0;
		}

	}

}