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
        public List<Order> Orders { get; } = new List<Order>();
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
                Log("[{0}] -> [{1}] {2}", Order.Config.Origin, Order.Config.Destination, Order.DeliveryTime);
                This.Orders.Add(Order);
            }
        }
        #endregion
        
        public Network(Statics config, int seed, string tag = null) : base(config, seed, tag)
        {
            Name = "Network";
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
        }

        public override void WarmedUp(DateTime clockTime)
        {
            base.WarmedUp(clockTime);
        }

        public override void WriteToConsole(DateTime? clockTime = null)
        {
            base.WriteToConsole(clockTime);
        }
    }
}
