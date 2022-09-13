using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSP2050.Scripts
{
	public abstract class ATutorialRequirement
	{
		public abstract bool EvaluateRequirement();

		public virtual void ActivateRequirement()
		{ }

		public virtual void DeactivateRequirement()
		{ }
	}
}
