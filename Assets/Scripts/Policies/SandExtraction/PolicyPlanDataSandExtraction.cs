using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
    public class PolicyPlanDataSandExtraction : APolicyPlanData
    {
        public int m_value;

        public PolicyPlanDataSandExtraction(APolicyLogic a_logic) : base(a_logic)
        {
            m_value = 0;
        }

        public void AddUnchangedValues(int value)
        {
            //Add the values that are not changed by the player here
            if (m_value == 0)
                return;
            value = m_value;
        }
    }
}