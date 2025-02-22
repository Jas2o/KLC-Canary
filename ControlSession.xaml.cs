﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KLC_Finch
{
    /// <summary>
    /// Interaction logic for ControlSession.xaml
    /// </summary>
    public partial class ControlSession : UserControl
    {
        public ControlSession()
        {
            InitializeComponent();
        }

        public ControlSession(string name)
        {
            InitializeComponent();
            btnMachine.Content = name;

            btnRCShared.Visibility = Visibility.Collapsed;
            //btnRCShared2.Visibility = Visibility.Collapsed;
            btnRCPrivate.Visibility = Visibility.Collapsed;
        }
    }
}
