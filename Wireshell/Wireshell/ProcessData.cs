using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wireshell
{
    public class ProcessData
    {
        public string Ip { get; set; }
        public long Port { get; set; }
        public int PID { get; set; }
        public string Name { get; set; }
        public DateTime TimeStamp { get; set; }
        public bool Uploaded { get; set; }
        public bool Connected { get; set; }
        public int Pointer { get; set; }

        public ProcessData(string Ip, long Port, int PID, string Name, DateTime TimeStamp)
        {
            this.Ip = Ip;
            this.Port = Port;
            this.PID = PID;
            this.Name = Name;
            this.TimeStamp = TimeStamp;
            Uploaded = false;
            Connected = true;
            Pointer = -1;
        }

        public void WriteData()
        {
            Console.WriteLine("IP: " + Ip + "\t Port: " + Port + "\t PID: " + PID + "\t Name: " + Name);
        }

        public bool Equals(ProcessData data)
        {
            return Ip == data.Ip && Port == data.Port && PID == data.PID && Name == data.Name;
        }

        public int CountByName(List<ProcessData> List)
        {
            int count = 0;
            foreach (ProcessData pData in List)
            {
                if (this.Name == pData.Name)
                {
                    count++;
                }
            }
            return count;
        }

    }
}
