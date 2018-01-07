using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using O2DESNet;
using O2DESNet.Distributions;

namespace Sim4PL.Model
{
    public class Network : State<Network.Statics>
    {
        #region Statics
        public class Statics : Scenario
        {
            /// <summary>
            /// Number of nodes
            /// 运输节点的数量
            /// </summary>
            public int NNodes { get; set; }
            /// <summary>
            /// Number of Transporters
            /// 运输者数量
            /// </summary>
            public int NTransporters { get; set; }
            /// <summary>
            /// Daily rates for transportation demands between pairs of nodes
            /// 每对运输节点之间的需求率
            /// </summary>
            public double[,] DemandRates { get; set; }
            /// <summary>
            /// Travelling times in days between pairs of nodes
            /// 运输时间，以天为单位
            /// </summary>
            public double[,] TravellingTimes { get; set; }
            /// <summary>
            /// 宽限期（可以延迟的运输时间）平均值，以天为单位
            /// </summary>
            public double[,] GracePeriod_Mean { get; set; }
            /// <summary>
            /// 宽限期（可以延迟的运输时间）离散系数，以天为单位
            /// </summary>
            public double[,] GracePeriod_CoeffVar { get; set; }
        }
        #endregion

        #region Sub-Modules
        private Dictionary<Tuple<int, int>, Generator<Order>> Generators { get; }
            = new Dictionary<Tuple<int, int>, Generator<Order>>();
        #endregion

        #region Dynamics
        public List<Order> OrdersToAssign { get; } = new List<Order>();
        public HashSet<Order> OrdersAssigned { get; } = new HashSet<Order>();
        public List<Order> OrdersDelivered { get; } = new List<Order>();
        public List<Transporter> Transporters { get; } = new List<Transporter>();
        public double DelayRate { get { return 1.0 * OrdersDelivered.Count(o => o.Delay.TotalDays > 0) / OrdersDelivered.Count; } }
        public double TimeRatio_Transporting { get { return Transporters.Average(t => t.HourCounter_Transporting.AverageCount); } }
        #endregion

        #region Events
        private abstract class InternalEvent : Event<Network, Statics> { } // event adapter 
        private class DemandArriveEvent : InternalEvent
        {
            internal Order Order { get; set; }
            internal int Origin { get; set; }
            internal int Destination { get; set; }
            public override void Invoke()
            {
                var o = Order.Config;
                Execute(Order.Place(ClockTime.AddDays(Gamma.Sample(DefaultRS,
                    Config.GracePeriod_Mean[o.Origin, o.Destination],
                    Config.GracePeriod_CoeffVar[o.Origin, o.Destination])
                    )));
                Log("Demand Arrive [{0}] -> [{1}] {2}",
                    Order.Config.Origin, Order.Config.Destination, Order.ExpectedDeliveryTime);
                This.OrdersToAssign.Add(Order);
                Execute(new DispatchEvent());
            }
        }
        private class DispatchEvent : InternalEvent
        {
            public override void Invoke()
            {
                var transList = This.Transporters.Where(t => t.Phase == Transporter.Statics.Phase.Idle).ToList();
                
                /*** Implement dispatching rule here ***/
                #region Dispatching Rule 
                if (This.OrdersToAssign.Count > 0 && transList.Count > 0)
                {
                    Log("Dispatch");
                    var order = This.OrdersToAssign.First();
                    var trans = transList.OrderBy(t => Config.TravellingTimes[t.CurrentNode, order.Config.Origin]).First();
                    Execute(trans.Assign(order));
                    This.OrdersToAssign.RemoveAt(0);
                    This.OrdersAssigned.Add(order);
                    Execute(new DispatchEvent());
                }
                #endregion
            }
        }
        private class DeliverEvent : InternalEvent
        {
            internal Order Order { get; set; }
            public override void Invoke()
            {
                Log("Deliver {0}", Order);
                This.OrdersAssigned.Remove(Order);
                This.OrdersDelivered.Add(Order);
                Execute(Order.Deliver());
                Execute(new DispatchEvent());                
            }
        }
        #endregion

        public Network(Statics config, int seed, string tag = null) : base(config, seed, tag)
        {
            Name = "Network";
            Display = true;

            for (int i = 0; i < Config.NNodes; i++)
                for (int j = 0; j < Config.NNodes; j++)
                    if (i != j && Config.DemandRates[i, j] > 0)
                    {
                        int o = i, d = j;
                        var g = new Generator<Order>(
                            new Generator<Order>.Statics
                            {
                                Create = rs => new Order(new Order.Statics
                                {
                                    Origin = o,
                                    Destination = d,
                                }, rs.Next()),
                                InterArrivalTime = rs => TimeSpan.FromDays(
                                    Exponential.Sample(rs, 1 / Config.DemandRates[o, d])),
                                SkipFirst = true,
                            }, DefaultRS.Next());
                        g.OnArrive.Add(order => new DemandArriveEvent { This = this, Order = order });
                        Generators.Add(new Tuple<int, int>(i, j), g);
                        InitEvents.Add(g.Start());
                    }
            Transporters.AddRange(Enumerable.Range(0, Config.NTransporters)
                .Select(i => new Transporter(new Transporter.Statics(), DefaultRS.Next())
                {
                    Display = Display,
                    Tag = "Transporter#" + i,
                }));
            foreach (var t in Transporters) t.OnFinishTransport.Add(order => new DeliverEvent { This = this, Order = order });
            InitEvents.AddRange(Transporters.Select(t => t.Init(this)));
        }

        public override void WarmedUp(DateTime clockTime)
        {
            base.WarmedUp(clockTime);
        }

        public override void WriteToConsole(DateTime? clockTime = null)
        {
            foreach (var t in Transporters) t.WriteToConsole(clockTime);
            Console.Write("To Assign: ");
            foreach (var o in OrdersToAssign) Console.Write("{0} ", o);
            Console.WriteLine();
            Console.WriteLine("Delay Rate: {0:F2}%  Trans. Ratio: {1:F2}%", 100 * DelayRate, 100 * TimeRatio_Transporting);
            Console.WriteLine();
        }
    }
}
