using System.Collections.Generic;
using UnityEngine;

namespace Assets.Networking
{
    public class SerialOperation : AbstractOperation
    {
        List<AbstractOperation> operations;
        int currentOperation = 0;  

        public SerialOperation(CompletionHandler completionCallback = null) : base(completionCallback)
        {
            operations = new List<AbstractOperation>();
        }

        public SerialOperation AppendOperation(AbstractOperation operation)
        {
            if (started)            
                Debug.LogError("Cannot append operations to an operation that has started.");           
            else
                operations.Add(operation);
            return this;
        }

        public override void StartOperation(int index, CompletionHandler completionCallback)
        {
            base.StartOperation(index, completionCallback);
            StartNextOperation();           
        }

        public void OperationCompleted(int index)
        {
            if (index == -1 || index != currentOperation)
            {
                Debug.LogError("Unexpected operation completed");
                return;
            }
            currentOperation++;
            StartNextOperation();
        }

        private void StartNextOperation()
        {
            if (currentOperation >= operations.Count)
                Complete();         
            else
                operations[currentOperation].StartOperation(currentOperation, OperationCompleted);
        }
    } 
}
