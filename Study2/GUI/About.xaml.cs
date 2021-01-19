using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PLC.GUI
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About(PLC.Licensing.CheckLicense c)
        {
            InitializeComponent();
            if (Global.Licensed)
            {
                LicensedTo.Text = c.LicensedTo;
                TimeSpan T = Convert.ToDateTime(c.LicenseExpiration) - DateTime.Now;
                LicenseExpiration.Text = c.LicenseExpiration + " [" + T.Days + " days remaining]";
                TrialExpiration.Text = "-";
                string str = c.MachineID;
                for (int ins = 5; ins < str.Length; ins += 5 + 1)
                {
                    str = str.Insert(ins, "-");
                }
                MachineID.Text = str;
            }
            else
            {
                TrialExpiration.Text = Global.counter.value.ToString();
                EasyLicense.Lib.License.Validator.HardwareInfo MID = new EasyLicense.Lib.License.Validator.HardwareInfo();
                Answer.Children.Remove(MachineID);
                Button myButton = new Button
                {
                    Content = "Generate Activation Code"
                };
                myButton.Click += MyButton_Click;
                Answer.Children.Add(myButton);
            }

        }
        private void MyButton_Click(object sender, RoutedEventArgs e)
        {
            EasyLicense.Lib.License.Validator.HardwareInfo x = new EasyLicense.Lib.License.Validator.HardwareInfo();
            string str = x.GetHardwareString();
            for (int ins = 5; ins < str.Length; ins += 5 + 1)
            {
                str = str.Insert(ins, "-");
            }
            Clipboard.SetText(str);
            MessageBox.Show("Your Activation Code: " + '"' + str + '"' + " has been copied to clipboard.\nPress CTRL + V to paste.");
        }
        private void URL_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://billymosis.com");
        }
    }
}
