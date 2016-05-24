using System;
namespace EasyNetQ.Topology
{
    public class Queue : IQueue
    {
        public Queue(string name, bool isExclusive, bool isDurable, bool isAutoDelete, bool isConsumerRepairable)
        {
            Preconditions.CheckNotBlank(name, "name");
            Name = name;
            IsExclusive = isExclusive;
            IsDurable = isDurable;
            IsAutoDelete = isAutoDelete;
            IsConsumerRepairable = isConsumerRepairable;
        }

        public string Name { get; private set; }
        public bool IsExclusive { get; private set; }
        public bool IsDurable { get; private set; }
        public bool IsAutoDelete { get; private set; }

        public string BoundExchange { get; set; }

        public bool IsConsumerRepairable { get; private set; }
        
    }       
}