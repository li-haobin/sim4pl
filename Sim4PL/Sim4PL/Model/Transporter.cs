using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using O2DESNet;

namespace Sim4PL.Model
{
    public class Transporter : State<Transporter.Statics>
    {
        #region Statics
        public class Statics : Scenario
        {
            public enum Phase { Idle, Relocating, Picking, Transporting }
        }
        #endregion

        #region Dynamics
        public Network Network { get; private set; }
        public Order AssignedOrder { get; private set; } = null;
        public int CurrentNode { get; private set; }
        public int? Target { get; private set; }
        public Statics.Phase Phase { get; private set; } = Statics.Phase.Idle;
        public DateTime Timestamp { get; private set; }
        public HourCounter HourCounter_Transporting { get; } = new HourCounter();
        #endregion

        #region Events
        private abstract class InternalEvent : Event<Transporter, Statics> { } // event adapter 

        private class InitEvent : InternalEvent
        {
            internal Network Network { get; set; }
            public override void Invoke()
            {
                This.Network = Network;
                This.CurrentNode = DefaultRS.Next(Network.Config.NNodes);
            }
        }
        private class AssignEvent : InternalEvent
        {
            internal Order Order { get; set; }
            public override void Invoke()
            {
                Log("Assign {0} for {1}", This, Order);
                This.AssignedOrder = Order;
                if (Order.Config.Origin == This.CurrentNode) Execute(new StartTransport());
                else Execute(new StartPickEvent());
            }
        }
        private class StartPickEvent : InternalEvent
        {
            public override void Invoke()
            {
                Log("Start Pick {0} for {1}", This, This.AssignedOrder);
                This.Phase = Statics.Phase.Picking;
                This.Timestamp = ClockTime;
                This.Target = This.AssignedOrder.Config.Origin;
                Schedule(new StartTransport(), TimeSpan.FromDays(This.Network.Config.TravellingTimes[
                    This.CurrentNode, This.Target.Value]));                
            }
        }
        private class StartTransport : InternalEvent
        {
            public override void Invoke()
            {
                Log("Start Transport {0} for {1}", This, This.AssignedOrder);
                This.Phase = Statics.Phase.Transporting;
                This.CurrentNode = This.AssignedOrder.Config.Origin;
                This.Timestamp = ClockTime;
                This.Target = This.AssignedOrder.Config.Destination;
                This.HourCounter_Transporting.ObserveCount(1, ClockTime);
                Execute(This.OnStartTransport.Select(e => e(This.AssignedOrder)));
                Schedule(new FinishTransport(), TimeSpan.FromDays(This.Network.Config.TravellingTimes[
                    This.CurrentNode, This.Target.Value]));                
            }
        }
        private class FinishTransport : InternalEvent
        {
            public override void Invoke()
            {
                Log("Finish Transport {0} for {1}", This, This.AssignedOrder);
                This.Phase = Statics.Phase.Idle;
                This.CurrentNode = This.AssignedOrder.Config.Destination;
                This.Timestamp = ClockTime;
                This.Target = null;
                This.HourCounter_Transporting.ObserveCount(0, ClockTime);
                Schedule(This.OnFinishTransport.Select(e => e(This.AssignedOrder)));
                This.AssignedOrder = null;
            }
        }
        private class StartRelocateEvent : InternalEvent
        {
            internal int Node { get; set; }
            public override void Invoke()
            {
                if (This.Phase != Statics.Phase.Idle) throw new Exception("Transporter is not idle.");
                Log("Start Relocate {0} to {1}", This, Node);
                This.Phase = Statics.Phase.Relocating;
                This.CurrentNode = This.Target.Value;
                This.Target = null;                
                This.Timestamp = ClockTime;
                Schedule(new StartTransport(), TimeSpan.FromDays(This.Network.Config.TravellingTimes[
                    This.CurrentNode, This.AssignedOrder.Config.Origin]));
            }
        }
        private class FinishRelocate : InternalEvent
        {
            public override void Invoke()
            {
                Log("Finish Transport {0} for {1}", This, This.AssignedOrder);
                This.Phase = Statics.Phase.Idle;
                This.CurrentNode = This.Target.Value;
                This.Target = null;
                This.Timestamp = ClockTime;                
                Schedule(This.OnFinishRelocate.Select(e => e()));
            }
        }
        #endregion

        #region Input Events - Getters
        public Event Init(Network network) { return new InitEvent { This = this, Network = network }; }
        public Event Assign(Order order) { return new AssignEvent { This = this, Order = order }; }
        #endregion

        #region Output Events - Reference to Getters
        public List<Func<Order, Event>> OnStartTransport { get; } = new List<Func<Order, Event>>();
        public List<Func<Order, Event>> OnFinishTransport { get; } = new List<Func<Order, Event>>();
        public List<Func<Event>> OnFinishRelocate { get; } = new List<Func<Event>>();
        #endregion

        public Transporter(Statics config, int seed, string tag = null) : base(config, seed, tag)
        {
            Name = "Transporter";
        }

        public override void WarmedUp(DateTime clockTime)
        {
            base.WarmedUp(clockTime);
        }

        public override void WriteToConsole(DateTime? clockTime = null)
        {
            Console.WriteLine("{0} [{1}->{2}] {3} {4} {5}",
                this, CurrentNode, Target, Phase, AssignedOrder, Timestamp);
        }
    }
}
