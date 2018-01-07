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
        public DateTime ExpectedDeliveryTime { get; private set; }
        public DateTime ActualDeliveryTime { get; private set; }
        public TimeSpan Delay
        {
            get
            {
                return ActualDeliveryTime < ExpectedDeliveryTime ?
                    TimeSpan.FromDays(0) : ActualDeliveryTime - ExpectedDeliveryTime;
            }
        }
        #endregion

        #region Events
        private abstract class InternalEvent : Event<Order, Statics> { } // event adapter 

        private class PlaceEvent : InternalEvent
        {
            internal DateTime ExpectedDeliveryTime { get; set; }
            public override void Invoke()
            {
                This.OrderTime = ClockTime;
                This.ExpectedDeliveryTime = ExpectedDeliveryTime;
            }
        }
        private class DeliverEvent : InternalEvent
        {
            public override void Invoke()
            {
                This.ActualDeliveryTime = ClockTime;
            }
        }
        #endregion

        #region Input Events - Getters
        public Event Place(DateTime deliveryTime) { return new PlaceEvent { This = this, ExpectedDeliveryTime = deliveryTime }; }
        public Event Deliver() { return new DeliverEvent { This = this }; }
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
