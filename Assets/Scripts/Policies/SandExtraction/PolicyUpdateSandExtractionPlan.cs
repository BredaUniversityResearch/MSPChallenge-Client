using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
    public class PolicyUpdateSandExtractionPlan : APolicyData
    {
        public int m_distanceValue; //The sand extraction distance value

        //Constructor for convenience
        public PolicyUpdateSandExtractionPlan(int distanceValue = 0)
        {
            m_distanceValue = distanceValue;
        }
    }
}