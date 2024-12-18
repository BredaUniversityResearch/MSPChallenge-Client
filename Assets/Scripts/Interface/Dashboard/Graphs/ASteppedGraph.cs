using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class ASteppedGraph : MonoBehaviour
	{
		public abstract void Initialise();
		public abstract void SetData(GraphDataStepped a_data);
	}
}
