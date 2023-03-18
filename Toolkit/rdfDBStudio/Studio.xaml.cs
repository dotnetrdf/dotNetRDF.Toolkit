using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VDS.RDF.Utilities.Studio
{
    /// <summary>
    /// Interaction logic for Studio.xaml
    /// </summary>
    public partial class Studio 
        : Window
    {
        public Studio()
        {
            InitializeComponent();
        }

        #region Toolbar

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region File Menu

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region View Menu

        private void mnuViewSidebar_Click(object sender, RoutedEventArgs e)
        {
            if (tvwSidebar.Visibility == System.Windows.Visibility.Collapsed)
            {
                gridMain.ColumnDefinitions[0].Width = new GridLength(100, GridUnitType.Star);
                tvwSidebar.Visibility = System.Windows.Visibility.Visible;
                splMain.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                gridMain.ColumnDefinitions[0].Width = new GridLength(0);
                tvwSidebar.Visibility = System.Windows.Visibility.Collapsed;
                splMain.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        #endregion

        #region Help Menu

        private void mnuAbout_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.Show();
        }

        #endregion
    }
}
