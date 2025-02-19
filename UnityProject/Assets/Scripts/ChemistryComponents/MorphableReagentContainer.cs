﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Items;
using UnityEngine;
using Mirror;

namespace Chemistry.Components
{
	/// <summary>
	/// Defines container that can change it appearance based on reagent inside
	/// Mostly used for drinking glass that changes depending on cocktails
	/// </summary>
	[RequireComponent(typeof(ReagentContainer))]
	public class MorphableReagentContainer : NetworkBehaviour
	{
		private static int NoMajorReagent = "".GetStableHashCode();

		public SpriteRenderer mainSpriteRender;
		public MorphableReagentContainerData data;

		private Chemistry.Reagent majorReagent;
		[SyncVar(hook = "OnMajorReagentChanged")]
		private int majorReagentNameHash = NoMajorReagent;

		private ReagentContainerFillVisualisation fillVisual;
		private ReagentContainer serverContainer;
		private ItemAttributesV2 item;
		private SpriteHandler mainSpriteHandler;

		private Sprite defaultSprite;

		private void Awake()
		{
			if (!data || !mainSpriteRender)
				return;
			mainSpriteHandler = mainSpriteRender.GetComponent<SpriteHandler>();

			fillVisual = GetComponent<ReagentContainerFillVisualisation>();
			serverContainer = GetComponent<ReagentContainer>();
			item = GetComponent<ItemAttributesV2>();

			// save default data
			defaultSprite = mainSpriteRender.sprite;

			if (serverContainer)
			{
				serverContainer.OnReagentMixChanged.AddListener(OnMixChanged);
			}
		}

		[Server]
		private void OnMixChanged()
		{
			// Check if major reagent changed in a mix
			var newMajorReagent = serverContainer.MajorMixReagent;
			if (newMajorReagent != majorReagent)
			{
				// get major reagent name
				var majorReagentName = newMajorReagent ? newMajorReagent.Name : "";

				// now send it to all clients as string hash
				majorReagentNameHash = majorReagentName.GetStableHashCode();
				majorReagent = newMajorReagent;
			}
		}

		[Client]
		private void OnMajorReagentChanged(int oldHash, int newHash)
		{
			if (newHash != NoMajorReagent)
			{
				// check if we have this reagent in overrides table
				var spriteData = data.Get(newHash);
				if (spriteData != null)
					ShowVisualisation(spriteData);
				else
					DisableVisualisation();
			}
			else
			{
				DisableVisualisation();
			}
		}

		/// <summary>
		/// Show visualisation of this reagent data
		/// </summary>
		/// <param name="spriteData"></param>
		[Client]
		private void ShowVisualisation(ContainerCustomSprite spriteData)
		{
			mainSpriteHandler.SetSprite(spriteData.MainSprite);
			if (fillVisual && fillVisual.fillSpriteRender)
				fillVisual.fillSpriteRender.gameObject.SetActive(false);

			if (item)
			{
				// set custom description from data (if avaliable)
				var customDesc = spriteData.CustomDescription;
				if (!string.IsNullOrEmpty(customDesc))
					item.ServerSetArticleDescription(customDesc);

				// set custom name from data (if avaliable)
				var customName = spriteData.CustomName;
				if (!string.IsNullOrEmpty(customName))
					item.ServerSetArticleName(customName);
			}


		}

		/// <summary>
		/// Disable all morphable overrides and show standard graphics
		/// </summary>
		[Client]
		private void DisableVisualisation()
		{
			mainSpriteHandler.SetSprite(defaultSprite);
			if (fillVisual && fillVisual.fillSpriteRender)
				fillVisual.fillSpriteRender.gameObject.SetActive(true);

			// return description to standard
			if (item)
			{
				// set default description
				item.ServerSetArticleDescription(item.InitialDescription);

				// set default name
				item.ServerSetArticleName(item.InitialName);
			}
		}
	}
}
