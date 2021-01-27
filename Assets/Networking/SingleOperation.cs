using System;
using UnityEngine;

namespace Assets.Networking
{
    public class SingleOperation : AbstractOperation
    {
        Action task;

        public SingleOperation(CompletionHandler completionCallback = null) : base(completionCallback)
        {}

        /// <summary>
        /// Assign a task to the operation.
        /// The task should always call this.Complete() when it has been completed.
        /// </summary>
        public SingleOperation SetTask(Action task)
        {
            if (started)
                Debug.LogError("Cannot add a task to an operation that has started.");            
            else
                this.task = task;
            return this;
        }

        public override void StartOperation(int index, CompletionHandler completionCallback)
        {
            base.StartOperation(index, completionCallback);
            if (task != null)
                task.Invoke();
            else
            {
                Debug.LogWarning("Operation without assigned task performed"); 
                Complete();
            }
        }
    }
}
