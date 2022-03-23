using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KLC_Finch
{
    public class ConnectionGroup
    {
        public string Name;
        public Label Label;
        public Border Border;
        public StackPanel GroupStack;

        public ConnectionGroup(string name)
        {
            Name = name;

            Label = new Label()
            {
                Content = Name,
            };
            Border = new Border()
            {
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(1)
            };
            GroupStack = new StackPanel();

            Border.Child = GroupStack;
        }
    }
}
