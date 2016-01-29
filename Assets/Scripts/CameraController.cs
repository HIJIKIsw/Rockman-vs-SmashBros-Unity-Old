﻿using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
	private int SizeW = 256;
	private int SizeH = 240;

	private GameObject Player;						// Player オブジェクト

	// Use this for initialization
	void Start()
	{
		Screen.SetResolution(SizeW, SizeH, false);
		Player = GameObject.FindWithTag("Player");
	}

	// Update is called once per frame
	void Update()
	{
		if(Player != null)
		{
			this.transform.position = Player.transform.position;
		}
		
	}
}
