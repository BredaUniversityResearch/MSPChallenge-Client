using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityEngine.UI
{
	public interface IUIScaleChangeReceiver
	{
		void OnUIScaleChange(int a_newScale);
	}
}
