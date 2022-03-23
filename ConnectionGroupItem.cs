using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KLC_Finch
{
    public class ConnectionGroupItem
    {
        public Button btnAgent;
        public Button btnRCShared;
        public Button btnRCPrivate;

        public ConnectionGroupItem(Connection conn)
        {
            btnAgent = new Button()
            {
                Content = conn.Label,
                Tag = conn,
            };
            btnRCShared = new Button()
            {
                Content = " - RC Shared",
                Visibility = Visibility.Collapsed,
                Tag = conn
            };
            btnRCPrivate = new Button()
            {
                Content = " - RC Private",
                Visibility = Visibility.Collapsed,
                Tag = conn
            };
        }
    }
}
