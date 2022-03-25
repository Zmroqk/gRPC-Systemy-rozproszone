using System;
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
using ClientgRPC;
using Services.AtmosphericData;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        gRpcProvider Provider;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Provider = new gRpcProvider();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AtmosphericData data = new AtmosphericData()
            {
                Date = datePicker.SelectedDate.ToString(),
                Temperature = int.Parse(txbTemperature.Text),
                Humidity = int.Parse(txbHumidity.Text),
                Pressure = int.Parse(txbPressure.Text),
            };
            Provider.AtmosphericDataHandler.SaveData(data);
        }

        private void AtmosphericData_Click(object sender, RoutedEventArgs e)
        {
            if(datePicker.SelectedDate != null)
            {
                var response = Provider.AtmosphericDataHandler.GetAllData(new Empty());
                ListAtmosphericData.ItemsSource = response.Data.ToList();
            }
        }
    }
}
