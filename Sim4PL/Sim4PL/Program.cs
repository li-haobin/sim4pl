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
                DemandRates = new double[,] {
                    { 0, 1, 2 },
                    { 3, 0, 4 },
                    { 5, 6, 0 },
                },
                TravellingTimes = new double[,] {
                    { 0, 2.2, 2.5 },
                    { 3.0, 0, 4.0 },
                    { 3.5, 3.6, 0 },
                },
                GracePeriod_Mean = new double[,] {
                    { 0, 1.5, 1.0 },
                    { 1.5, 0, 1.5 },
                    { 2.2, 1.0, 0 },
                },
                GracePeriod_CoeffVar = new double[,] {
                    { 0, 1.1, 1.3 },
                    { 0.8, 0, 0.3 },
                    { 0.5, 1.2, 0 },
                },
            };
            var network = new Network(config, seed: 0) { Display = true };
            var sim = new Simulator(network);
            while (true)
            {
                sim.Run(1);
                Console.ReadKey();
            }
        }
    }
}
