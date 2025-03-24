using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
    public class PolicyUpdateSandExtractionPlan : APolicyData
    {
        //The sand extraction distance value
        public int m_distanceValue;

        //Constructor for convenience
        public PolicyUpdateSandExtractionPlan()
        {
            m_distanceValue = 0;
        }
        
        public PolicyUpdateSandExtractionPlan(int distanceValue)
        {
            m_distanceValue = distanceValue;
        }
    }
}