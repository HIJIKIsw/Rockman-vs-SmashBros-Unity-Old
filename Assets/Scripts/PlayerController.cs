using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(BasicMovementController))]
public class PlayerController : MonoBehaviour
{
	private BasicMovementController BMController;   // BasicMovementController コンポーネント	
	private BoxCollider2D BoxCollider2D;            // BoxCollider2D コンポーネント
	private Animator Animator;                      // Animator コンポーネント
	private SpriteRenderer SpriteRenderer;          // SpriteRenderer コンポーネント

	private float WalkSpeed;                        // 左右キーによる移動の加速度
	private float WalkSpeedMax;                     // 左右キーによる移動の最大速度
	private float JumpSpeed;                        // ジャンプの初速

	/// <summary>
	/// コンストラクタ
	/// </summary>
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

	/// <summary>
	/// 描画毎に呼ばれる
	/// </summary>
	void Update()
	{
		// 入力受付
		Operation();

		// 移動量反映
		BMController.Calc(true, true);

		// アニメーション管理
		AnimationManagement();
	}

	/// <summary>
	/// 入力受付
	/// </summary>
	private void Operation()
	{
		// ジャンプ開始
		if (Input.GetButtonDown("Jump"))
		{
			BMController.Jump(JumpSpeed);
		}
		// 左右移動
		float Direction = Input.GetAxis("Horizontal");
		if (Direction != 0)
		{
			SpriteRenderer.flipX = Direction < 0;
			BMController.MoveDistance.x = Mathf.Min(WalkSpeedMax, Mathf.Max(-WalkSpeedMax, BMController.MoveDistance.x += WalkSpeed * Direction));
		}
		// 左右キーが入力されていない場合は移動をやめる
		else
		{
			BMController.MoveDistance.x = 0;
		}
	}

	/// <summary>
	/// アニメーション管理
	/// </summary>
	private void AnimationManagement()
	{
		bool IsJumping = BMController.IsAir;
		bool IsWalking = BMController.MoveDistance.x != 0f ? true : false;

		Animator.SetBool("IsJumping", IsJumping);
		Animator.SetBool("IsWalking", IsWalking);
	}
}