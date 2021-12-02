using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface ICustomSlider
{
	bool Interactable { get; }
	void AddInteractabilityChangeCallback(CustomSlider.InteractabilityChangeCallback callback);
	void RemoveInteractabilityChangeCallback(CustomSlider.InteractabilityChangeCallback callback);
}

