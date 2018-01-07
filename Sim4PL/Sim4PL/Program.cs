using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using O2DESNet;
using Sim4PL.Model;

namespace Sim4PL
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new Network.Statics
            {
                NNodes = 3,
                NTransporters = 8,
                DemandRates = new double[,] {
                    { 0, 0.5, 0.2 },
                    { 0.3, 0, 0.4 },
                    { 0.1, 0.2, 0 },
                },
                TravellingTimes = new double[,] {
                    { 0, 1.2, 1.5 },
                    { 2.0, 0, 1.0 },
                    { 2.5, 1.8, 0 },
                },
                GracePeriod_Mean = new double[,] {
                    { 0, 4.5, 5.0 },
                    { 6.5, 0, 3.5 },
                    { 4.2, 5.0, 0 },
                },
                GracePeriod_CoeffVar = new double[,] {
                    { 0, 0.1, 0.3 },
                    { 0.8, 0, 0.3 },
                    { 0.5, 0.2, 0 },
                },
            };
            var network = new Network(config, seed: 0);
            var sim = new Simulator(network);
            sim.Run(TimeSpan.FromSeconds(0));
            while (true)
            {
                sim.Run(TimeSpan.FromDays(30));
                sim.WriteToConsole();
                Console.ReadKey();
            }
        }
    }
}
