using System;
using System.Threading;
using System.IO;
using System.Collections;
using System.Text;
using CortexAccess;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace CommandConnector
{
    class Connector
    {
        const string licenseID = "14740daa-c423-4151-b144-ffe7ba27b557";

        static void Main(string[] args)
        {
            Console.WriteLine("CONNECTOR STARTING...");
            Console.WriteLine("Please wear Headset with good signal!!!");

            Console.WriteLine("Starting Minecraft Accessibility Companion server...");
            Thread thr = new Thread(new ThreadStart(WebServerThread.Start));
            thr.Start();

            //TODO: Extra code. This has to be removed.
            //Random _random = new Random();
            //
            //List<String> moves = new List<String>();
            //moves.Add("Push");
            //moves.Add("Pull");
            //moves.Add("Left");
            //moves.Add("Right");
            //CommandStream dse = new CommandStream();
            while (true)
            {
                //int index = _random.Next();
                //string com = moves[index % 4];
                //ArrayList al = new ArrayList();
                ////al.Add(com);
                //al.Add("Push");
                //al.Add(0);
                //
                //dse.ProcessMentalData("sender", al);

                WebServerThread.AddInput("keyboard", 'W', 1);

                Thread.Sleep(10);
                Console.WriteLine("Sent Command to Minecraft");
                //Thread.Sleep(10);
                //dse.ProcessMentalData("sender", al);
                //Console.WriteLine("Sent Command to Minecraft");
                //Thread.Sleep(10);
                //dse.ProcessMentalData("sender", al);
                //Console.WriteLine("Sent Command to Minecraft");
                //Thread.Sleep(10);
                //dse.ProcessMentalData("sender", al);
                //Console.WriteLine("Sent Command to Minecraft");
                //Thread.Sleep(10);
                //dse.ProcessMentalData("sender", al);
                //Console.WriteLine("Sent Command to Minecraft");
                //Thread.Sleep(10);
                //dse.ProcessMentalData("sender", al);
                //Console.WriteLine("Sent Command to Minecraft");
                //Thread.Sleep(10);
                //dse.ProcessMentalData("sender", al);
                //Console.WriteLine("Sent Command to Minecraft");
                //Thread.Sleep(10);

            }


            //Create a DataStream to read stream of mental commands.
            CommandStream dse = new CommandStream();
            dse.AddStreams("com");
            dse.OnSubscribed += SubscribedOK;

            // Need a valid license key and activeSession when subscribe performance metric data
            dse.Start(licenseID, true);

            Console.WriteLine("Press Esc to STOP reading stream and exit");
            while (Console.ReadKey().Key != ConsoleKey.Escape) { }

            // Unsubcribe stream
            dse.UnSubscribe();
            Thread.Sleep(5000);

            // Close Session
            dse.CloseSession();
            Thread.Sleep(5000);
        }

        private static void SubscribedOK(object sender, Dictionary<string, JArray> e)
        {
            foreach (string key in e.Keys)
            {
                Console.WriteLine("SubscribedOK:: Key: " + key);
                if (key == "met")
                {
                    // print header
                    ArrayList header = e[key].ToObject<ArrayList>();
                    Console.WriteLine("SubscribedOK:: MET Values: " + header.ToString());
                }
            }
        }
    }
}
