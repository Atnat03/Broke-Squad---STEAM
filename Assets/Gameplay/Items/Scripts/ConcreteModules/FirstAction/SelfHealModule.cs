using Gameplay.PlayerData;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.Items
{
	public class SelfHealModule : ActionModule, IFirstAction, IServerAction
	{
		public override string Emoji => "💉";
		public ICondition Condition => ConditionParent;

		[SerializeField] private float _amont = 10f;

		public void OnNotInHand()
		{
			
		}

		public void OnThrow()
		{
			
		}

		
		public void StartFirstAction()
		{
			if (!CanUse()) return;
			PlayerHealth player = Context.Inventory.Controller.Health;

			Context.SendToServer(this, new OnModuleDoAction_EVENT
			{
				ClientId = Context.Inventory.OwnerClientId,
				Target = player.NetworkObject,
				ValueF = _amont
			});

			ConsumeCondition(new OnModuleDoAction_EVENT());

		}

		public void StopFirstAction()
		{
			
		}

		public void ServerExecute(OnModuleDoAction_EVENT data)
		{
			PlayerHealth player = data.GetTarget().GetComponentInChildren<PlayerHealth>();

			if(player)
			{ 
				player.Heal(data.ValueF);
			}
		}
	}
}
