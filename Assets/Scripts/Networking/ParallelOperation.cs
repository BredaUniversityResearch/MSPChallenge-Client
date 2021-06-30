using System.Collections.Generic;
using UnityEngine;

namespace Assets.Networking
{
    public class ParallelOperation : AbstractOperation
    {
        List<AbstractOperation> operations;
        HashSet<int> remainingOperations;

        public ParallelOperation(CompletionHandler completionCallback = null) : base(completionCallback)
        {
            operations = new List<AbstractOperation>();
            remainingOperations = new HashSet<int>();
        }
        
        public ParallelOperation AddOperation(AbstractOperation operation)
        {
            if (started)
            {
                Debug.LogError("Cannot add operations to an operation that has started.");
            }
            else
            {
                operations.Add(operation);
                remainingOperations.Add(remainingOperations.Count);
            }
            return this;
        }

        public override void StartOperation(int index, CompletionHandler completionCallback)
        {        
            base.StartOperation(index, completionCallback);
            if (operations == null || operations.Count == 0)
            {
                Complete();
                return;
            }
            for(int i = 0; i < operations.Count; i++)
                operations[i].StartOperation(i, OperationCompleted);
        }

        public void OperationCompleted(int index)
        {
            if (index == -1)
            {
                Debug.LogError("Unexpected operation completed");
                return;
            }
            remainingOperations.Remove(index);
            if (remainingOperations.Count == 0)
                Complete();
        }
    }
}
