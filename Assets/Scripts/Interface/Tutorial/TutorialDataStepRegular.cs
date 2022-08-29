using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialDataStepRegular : ATutorialDataStep
	{
		public bool m_alignTop;

		//===Progression requirements
		/* Scroll
		 * Press button
		 * Toggle state
		 * Press one of X specific buttons (ex: window more info ? buttons)
		 * Press any button of type X (ex: layer category, layer)
		 * Map drag
		 * Create geometry complete
		 */

		//===Graphics/animation
		//Sprite
		//Sprite sequence (cut) + fps playback

		//===Elements to highlight

		//Actions allowed / disallowed (not needed?)

		//===Automatic actions (are these needed?)
		/* Open/close windows
		 */

		//Going back should not unset requirement completion
		//What happens when going back to step about specific window, with the window closed (ex: planwiz / drawing)?
		//What happens when step requirements are closed (ex: planwiz / drawing)?

		//Alt idea: tutorial sequences are shown in series (e.g. making a plan), the system auto detects at what step in the sequence you are and will move you back if you mess up.
		//This would require tutorial step prerequisites, besides progression requirements:
		/* Window open
		 * In edit mode
		 */
	}
}