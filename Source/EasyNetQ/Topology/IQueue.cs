namespace EasyNetQ.Topology
{
    /// <summary>
    /// Represents an AMQP queue
    /// </summary>
    public interface IQueue : IBindable
    {
        /// <summary>
        /// The name of the queue
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Is this queue transient?
        /// </summary>
        bool IsExclusive { get; }
        /// <summary>
        /// Is this queue durable?
        /// </summary>
        bool IsDurable { get; }
        /// <summary>
        /// Is this queue auto-delete ?
        /// </summary>
        bool IsAutoDelete { get; }
        /// <summary>
        /// If this queue has been bound to an exchange, gets the name of the exchange
        /// </summary>
        string BoundExchange { get; set; }
        /// <summary>
        /// If the consumer is canceled, should the queue be re-consumed? If true, the queue and binding will be recreated if necessary.
        /// </summary>
        bool IsConsumerRepairable { get; }
    }
}