﻿using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(BasicMovementController))]
public class PlayerController : MonoBehaviour
{
	private PlayerLadderController PLController;    // PlayerLadderController クラス
	private BasicMovementController BMController;   // BasicMovementController クラス	
	private Animator Animator;                      // Animator コンポーネント
	private SpriteRenderer SpriteRenderer;          // SpriteRenderer コンポーネント

	private float WalkSpeed;                        // 左右キーによる移動の加速度
	private float WalkSpeedMax;                     // 左右キーによる移動の最大速度
	private float JumpSpeed;                        // ジャンプの初速

	public bool ControlEnable;                 // PlayerController による操作受付が有効か

	/// <summary>
	/// コンストラクタ
	/// </summary>
	void Start()
	{
		ControlEnable = true;
		PLController = GetComponent<PlayerLadderController>();
		BMController = GetComponent<BasicMovementController>();
		Animator = transform.FindChild("Sprite").GetComponent<Animator>();
		SpriteRenderer = transform.FindChild("Sprite").GetComponent<SpriteRenderer>();

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
		if (ControlEnable)
		{
			Operation();
		}

		// 移動量反映
		BMController.Calc();

		// アニメーション管理
		AnimationManagement();
	}

	/// <summary>
	/// 入力受付
	/// </summary>
	private void Operation()
	{
		// ジャンプ開始
		if (Input.GetButtonDown("Jump") && !BMController.IsAir)
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
		bool IsLadderClimbing = PLController.IsLadderClimbing;
		bool IsLadderBend = PLController.IsLadderBend;
		bool IsJumping = BMController.IsAir;
		bool IsWalking = BMController.MoveDistance.x != 0f ? true : false;

		Animator.SetBool("IsLadderClimbing", IsLadderClimbing);
		Animator.SetBool("IsLadderBend", IsLadderBend);
		Animator.SetBool("IsJumping", IsJumping);
		Animator.SetBool("IsWalking", IsWalking);
	}
}