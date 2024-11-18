using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KLC_Finch {
    /// <summary>
    /// Interaction logic for WindowRCTest.xaml
    /// </summary>
    public partial class WindowRCTest2 : Window {

        //private readonly int width = 800;
        //private readonly int height = 1080;

        private const string exampleDefault = @"{""default_screen"":65539,""screens"":[{""screen_id"":65539,""screen_name"":""Test Screen"",""screen_width"":800,""screen_height"":1080,""screen_x"":0,""screen_y"":0}]}";

        private const string example1 = @"{""default_screen"":131073,""screens"":[{""screen_height"":2160,""screen_id"":131073,""screen_name"":""\\\\.\\DISPLAY4"",""screen_width"":3840,""screen_x"":1920,""screen_y"":-698},{""screen_height"":1080,""screen_id"":196706,""screen_name"":""\\\\.\\DISPLAY5"",""screen_width"":1920,""screen_x"":0,""screen_y"":0}]}";

        private const string example2 = @"{""default_screen"":65539,""screens"":[{""screen_height"":1080,""screen_id"":65539,""screen_name"":""\\\\.\\DISPLAY1"",""screen_width"":1920,""screen_x"":1920,""screen_y"":0},{""screen_height"":1080,""screen_id"":65537,""screen_name"":""\\\\.\\DISPLAY3"",""screen_width"":1920,""screen_x"":0,""screen_y"":0},{""screen_height"":1080,""screen_id"":18446744073389015000,""screen_name"":""\\\\.\\DISPLAY2"",""screen_width"":1920,""screen_x"":-1920,""screen_y"":0},{""screen_height"":1080,""screen_id"":349242001,""screen_name"":""\\\\.\\DISPLAY4"",""screen_width"":1920,""screen_x"":-3840,""screen_y"":0}]}";

        private const string example3 = @"{""default_screen"":131073,""screens"":[{""screen_height"":900,""screen_id"":131073,""screen_name"":""\\\\.\\DISPLAY1"",""screen_width"":1600,""screen_x"":0,""screen_y"":0},{""screen_height"":1080,""screen_id"":1245327,""screen_name"":""\\\\.\\DISPLAY2"",""screen_width"":1920,""screen_x"":1615,""screen_y"":-741},{""screen_height"":1080,""screen_id"":196759,""screen_name"":""\\\\.\\DISPLAY3"",""screen_width"":1920,""screen_x"":-305,""screen_y"":-1080}]}";

        public string ReturnValue;
        public bool ReturnMac;

        public WindowRCTest2() {
            InitializeComponent();
            txtInputJson.Text = exampleDefault;
        }

        private void BtnTemplateDefault_Click(object sender, RoutedEventArgs e) {
            txtInputJson.Text = exampleDefault;
        }

        private void BtnTemplate1_Click(object sender, RoutedEventArgs e) {
            //L-NB39
            txtInputJson.Text = example1;
        }

        private void BtnTemplate2_Click(object sender, RoutedEventArgs e) {
            //Monitor-2
            txtInputJson.Text = example2;
        }

        private void BtnTemplate3_Click(object sender, RoutedEventArgs e) {
            txtInputJson.Text = example3;
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e) {
            try {
                dynamic json = JsonConvert.DeserializeObject(txtInputJson.Text);
                string jsonstr = KLC.Util.JsonPrettify(txtInputJson.Text);
                txtInputJson.Text = jsonstr;

                ReturnValue = jsonstr;
                ReturnMac = (bool)chkMac.IsChecked;
                this.Close();
            } catch(Exception) {
            }
        }

        private void chkRetina_Changed(object sender, RoutedEventArgs e) {
            //rcTest.SetRetina((bool)chkRetina.IsChecked);
        }

    }
}
