using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Networking
{
    public abstract class AbstractOperation
    {
        public delegate void CompletionHandler(int index);
        protected event CompletionHandler CompletionCallback;
        protected int index = -1;
        protected bool started = false;

        public AbstractOperation(CompletionHandler completionCallback = null)
        {
            if(completionCallback != null)
                this.CompletionCallback += completionCallback;
        }

        public virtual void StartOperation(int index, CompletionHandler completionCallback)
        {
            started = true;
            this.index = index;
            if (completionCallback != null)
                this.CompletionCallback += completionCallback;
        }

        public void Complete()
        {
            if (CompletionCallback != null)
                CompletionCallback(index);
        }
    }
}
