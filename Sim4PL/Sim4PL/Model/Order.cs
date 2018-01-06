using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using O2DESNet;

namespace Sim4PL.Model
{
    public class Order : State<Order.Statics>
    {
        #region Statics
        public class Statics : Scenario
        {
            public int Origin { get; set; }
            public int Destination { get; set; }
        }
        #endregion
        
        #region Dynamics
        public DateTime OrderTime { get; private set; }
        public DateTime DeliveryTime { get; private set; }
        #endregion

        #region Events
        private abstract class InternalEvent : Event<Order, Statics> { } // event adapter 

        private class PlaceEvent : InternalEvent
        {
            internal DateTime DeliveryTime { get; set; }
            public override void Invoke()
            {
                This.OrderTime = ClockTime;
                This.DeliveryTime = DeliveryTime;
            }
        }
        #endregion

        #region Input Events - Getters
        public Event Place(DateTime deliveryTime) { return new PlaceEvent { This = this, DeliveryTime = deliveryTime }; }
        #endregion
        
        public Order(Statics config, int seed, string tag = null) : base(config, seed, tag)
        {
            Name = "Order";
        }

        public override void WriteToConsole(DateTime? clockTime = null)
        {
            //Console.WriteLine("{0} [{1}] -> {2} [{3}]", OrderTime, Origin, LatestDeliveryTime, Destination);
        }
    }
}
