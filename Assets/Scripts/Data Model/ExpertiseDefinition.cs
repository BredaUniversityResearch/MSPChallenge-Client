using System;
using System.Collections.Generic;

public class ExpertiseDefinition
{
    public string name;
    public List<string> visible_layers; //Names of layers that are visible at the start of the game
    public List<string> selected_layers; //Names of layers that are selected in the active layer window but not visible at the start of the game
}

