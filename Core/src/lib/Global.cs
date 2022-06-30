using Autodesk.AutoCAD.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC
{
    static class Global
    {
        public static Counter counter;
        public static bool Licensed = false;
        public static TrayItem MyTray;
        public static Licensing.CheckLicense CheckLicense;
        public static string dataPath;

        public static void AddCounter(int i)
        {
            counter.ThresholdReached += Counter_ThresholdReached;
            counter.CounterAdd(i);
        }

        private static void Counter_ThresholdReached(object sender, EventArgs e)
        {
            PLC.Command.Bubble();
        }
    }


    class Counter
    {
        public int value;

        public Counter(int x)
        {
            value = x;
        }

        public void CounterAdd(int i)
        {
            value += i;
            if (value <= 0)
            {
                OnThresholdReached(EventArgs.Empty);
            }
        }

        protected virtual void OnThresholdReached(EventArgs e)
        {
            ThresholdReached?.Invoke(this, e);
        }

        public event EventHandler ThresholdReached;
    }
}
