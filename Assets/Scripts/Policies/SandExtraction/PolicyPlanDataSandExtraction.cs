using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
    public class PolicyPlanDataSandExtraction : APolicyPlanData
    {
        public int m_value;

        public PolicyPlanDataSandExtraction(APolicyLogic a_logic) : base(a_logic)
        {
            m_value = 0; // Default value
        }
    }
}