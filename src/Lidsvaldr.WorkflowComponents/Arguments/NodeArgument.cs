using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public class NodeArgument<T> : AbstractValueSource<T>
    {
        public override bool IsExhausted { get; protected set; }

        public override bool IsValueReady { get { return (!IsExhausted || _value.Count > 0); } }

        public override Type ValueType { get { return GetType().GetGenericArguments()[0]; } }

        public override event Action<IValueSource> ValueReady;

        private Queue<T> _value;

        public NodeArgument(bool isExhausted)
        {
            _value = new Queue<T>();
            IsExhausted = isExhausted;
        }

        public override async Task<T> Pull()
        {
            while (!IsValueReady)
            {
                //TODO ?? i can't find better solution >__>""
                await Task.Delay(250);
            }

            return IsExhausted ? _value.Dequeue() : _value.Peek();
        }

        public override async Task Push(IValueSource item)
        {
            if (!item.GetType().IsAssignableFrom(ValueType))
            {
                //TODO create resource for messages
                throw new ArgumentException("Node input type mismatch.");
            }
            if (_value.Count > 0 && !IsExhausted)
            {
                throw new ArgumentException("Can't add one more item to non-exhausted resource.");
            }
            _value.Enqueue((T)(await item.Pull()));
        }
    }
}
