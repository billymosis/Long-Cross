using System;
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
            if (c.License2)
            {
                LicensedTo.Text = c.LicensedTo;
                TimeSpan T = Convert.ToDateTime(c.LicenseExpiration) - DateTime.Now;
                LicenseExpiration.Text = c.LicenseExpiration + " [" + T.Days + " days remaining]";
                TrialExpiration.Text = "-";
                MachineID.Text = c.MachineID;
            }
            else
            {
                TrialExpiration.Text = Global.counter.ToString();
                EasyLicense.Lib.License.Validator.HardwareInfo MID = new EasyLicense.Lib.License.Validator.HardwareInfo();
                Answer.Children.Remove(MachineID);
                Button myButton = new Button
                {
                    Content = "Generate Machine ID"
                };
                myButton.Click += MyButton_Click;
                Answer.Children.Add(myButton);
            }

        }
        private void MyButton_Click(object sender, RoutedEventArgs e)
        {
            EasyLicense.Lib.License.Validator.HardwareInfo x = new EasyLicense.Lib.License.Validator.HardwareInfo();
            var s = x.GetHardwareString();
            Clipboard.SetText(s);
            MessageBox.Show("Your Machine ID: " + s + " has been copied to clipboard.\nPress CTRL + V to paste.");
        }
    }
}
