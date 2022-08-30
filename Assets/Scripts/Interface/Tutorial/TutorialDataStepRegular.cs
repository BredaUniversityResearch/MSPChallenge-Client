using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialDataStepRegular : ATutorialDataStep
	{
		public bool m_alignTop;

		//===Progression requirements
		/* Scroll => register specific event
		 * Map drag => register specific event
		 * Create geometry complete => register specific event
		 * Press button => UI string reference
		 * Toggle state => UI string reference
		 * Press one of X specific buttons (ex: window more info ? buttons)  => UI string reference
		 * Press any button of type X (ex: layer category, layer) => button tags, generic button callback receiver InterfaceCanvas
		 */

		//===Graphics/animation
		//Sprite
		//Sprite sequence (cut) + fps playback

		//===Elements to highlight

		//===Automatic actions (are these needed?)
		/* Open/close windows => UI string reference toggles + state
		 */

		//Tutorial sequences are shown in series (e.g. making a plan), the system auto detects at what step in the sequence you are and will move you back if you mess up.
		//This would require tutorial step prerequisites, besides progression requirements:
		/* Window open => UI string reference
		 * In edit mode
		 * In create mode
		 */
	}
}