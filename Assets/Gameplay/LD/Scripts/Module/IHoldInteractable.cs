using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
	public interface IHoldInteractable : IInteractable
	{
		void HoldStart(ulong clientId);
		void HoldEnd(ulong clientId);
	}
}
