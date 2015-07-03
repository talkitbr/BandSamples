using System;
using System.Threading.Tasks;
using Microsoft.Band;
using Microsoft.Band.Sensors;
using MSBandSample.Common;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace MSBandSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private NavigationHelper navigationHelper;        
        private bool isConnected;
        private string heartRate;
        private string tempetature;
        private string accelerometer;        
        private string calories;
        private string distance;
        private string gyroscopeA;
        private string gyroscopeV;
        private string pedometer;
        private string uv;
        private string contact;

        DispatcherTimer secondTimer;

        public MainPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.NavigationCacheMode = NavigationCacheMode.Required;            
        }        

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }              

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);            
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion        

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.ConnectBand();
        }

        private async void ConnectBand()
        {
            if (this.isConnected)
            {
                this.isConnected = false;
                this.ConnectButton.Content = "Connect Band";                
            }
            else
            {
                this.ConnectButton.IsEnabled = false;
                this.ConnectButton.Content = "Connecting...";

                // Get the list of Microsoft Bands paired 
                IBandInfo[] pairedBands = await BandClientManager.Instance.GetBandsAsync();

                // Check if exist a microsoft band paired
                if (pairedBands.Length < 1)
                {
                    MessageDialog dialog = new MessageDialog("No paired band");
                    await dialog.ShowAsync();
                    return;
                }

                // Connect to Microsoft Band.
                using (var bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                {
                    this.ConnectButton.Content = "Disconnect Band";
                    this.isConnected = true;

                    //  Check current user heart rate consent
                    if (bandClient.SensorManager.HeartRate.GetCurrentUserConsent() != UserConsent.Granted)
                    {
                        await bandClient.SensorManager.HeartRate.RequestUserConsentAsync();
                    }                    

                    // Register event
                    bandClient.SensorManager.HeartRate.ReadingChanged += HeartRate_ReadingChanged;                    
                    bandClient.SensorManager.SkinTemperature.ReadingChanged += SkinTemperature_ReadingChanged;                    
                    bandClient.SensorManager.Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;                    
                    bandClient.SensorManager.Calories.ReadingChanged += Calories_ReadingChanged;
                    bandClient.SensorManager.Contact.ReadingChanged += Contact_ReadingChanged;
                    bandClient.SensorManager.Distance.ReadingChanged += Distance_ReadingChanged;
                    bandClient.SensorManager.Gyroscope.ReadingChanged += Gyroscope_ReadingChanged;
                    bandClient.SensorManager.Pedometer.ReadingChanged += Pedometer_ReadingChanged;
                    bandClient.SensorManager.UV.ReadingChanged += UV_ReadingChanged;

                    
                    // Start Reading
                    await bandClient.SensorManager.HeartRate.StartReadingsAsync();
                    await bandClient.SensorManager.SkinTemperature.StartReadingsAsync();
                    await bandClient.SensorManager.Accelerometer.StartReadingsAsync();
                    await bandClient.SensorManager.Calories.StartReadingsAsync();
                    await bandClient.SensorManager.Contact.StartReadingsAsync();
                    await bandClient.SensorManager.Distance.StartReadingsAsync();
                    await bandClient.SensorManager.Gyroscope.StartReadingsAsync();
                    await bandClient.SensorManager.Pedometer.StartReadingsAsync();
                    await bandClient.SensorManager.UV.StartReadingsAsync();


                    secondTimer = new DispatcherTimer();
                    secondTimer.Tick += SecondTimer_Tick;
                    secondTimer.Interval = new TimeSpan(0, 0, 1);
                    secondTimer.Start();

                    this.ConnectButton.IsEnabled = true;

                    while (this.isConnected)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }

                    this.ConnectButton.IsEnabled = false;

                    // Stop
                    await bandClient.SensorManager.HeartRate.StopReadingsAsync();
                    await bandClient.SensorManager.SkinTemperature.StopReadingsAsync();

                    secondTimer.Stop();

                    await ClearFields();

                    this.ConnectButton.Content = "Connect Band";

                    this.ConnectButton.IsEnabled = true;
                }
            }
        }

        void UV_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandUVReading> e)
        {
            IBandUVReading bandSensor = e.SensorReading;
            this.uv = bandSensor.IndexLevel.ToString();
        }

        void Pedometer_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandPedometerReading> e)
        {
            IBandPedometerReading bandSensor = e.SensorReading;
            this.pedometer = bandSensor.TotalSteps.ToString();
        }

        void Gyroscope_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandGyroscopeReading> e)
        {
            IBandGyroscopeReading bandSensor = e.SensorReading;
            this.gyroscopeA = string.Format("X: {0} - Y: {1} - Z: {2}", bandSensor.AccelerationX.ToString(), bandSensor.AccelerationY.ToString(), bandSensor.AccelerationZ.ToString());
            this.gyroscopeV = string.Format("X: {0} - Y: {1} - Z: {2}", bandSensor.AngularVelocityX.ToString(), bandSensor.AngularVelocityY.ToString(), bandSensor.AngularVelocityZ.ToString());
        }

        void Distance_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandDistanceReading> e)
        {
            IBandDistanceReading bandSensor = e.SensorReading;
            this.distance = bandSensor.TotalDistance.ToString();
        }

        void Contact_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandContactReading> e)
        {
            IBandContactReading bandSensor = e.SensorReading;
            this.contact = bandSensor.State.ToString();
        }

        void Calories_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandCaloriesReading> e)
        {
            IBandCaloriesReading bandSensor = e.SensorReading;
            this.calories = bandSensor.Calories.ToString();
        }

        void Accelerometer_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandAccelerometerReading> e)
        {
            IBandAccelerometerReading bandSensor = e.SensorReading;
            this.accelerometer = string.Format("X: {0} - Y: {1} - Z: {2}", bandSensor.AccelerationX.ToString(), bandSensor.AccelerationY.ToString(), bandSensor.AccelerationZ.ToString());
        }

        void SkinTemperature_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandSkinTemperatureReading> e)
        {
            IBandSkinTemperatureReading bandSensor = e.SensorReading;
            this.tempetature = bandSensor.Temperature.ToString() + "º";
        }

        private void HeartRate_ReadingChanged(object sender, Microsoft.Band.Sensors.BandSensorReadingEventArgs<Microsoft.Band.Sensors.IBandHeartRateReading> e)
        {
            IBandHeartRateReading bandSensor = e.SensorReading;
            this.heartRate = bandSensor.HeartRate.ToString();
        }

        async void SecondTimer_Tick(object sender, object e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.HeartRateValue.Text = !string.IsNullOrEmpty(this.heartRate) ? this.heartRate : string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.TemperatureValue.Text = !string.IsNullOrEmpty(this.tempetature) ? this.tempetature : string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.AccelerometerValue.Text = !string.IsNullOrEmpty(this.accelerometer) ? this.accelerometer : string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.CaloriesValue.Text = !string.IsNullOrEmpty(this.calories) ? this.calories : string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.DistanceValue.Text = !string.IsNullOrEmpty(this.distance) ? this.distance : string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.GyroscopeAValue.Text = !string.IsNullOrEmpty(this.gyroscopeA) ? this.gyroscopeA : string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.GyroscopeVValue.Text = !string.IsNullOrEmpty(this.gyroscopeV) ? this.gyroscopeV : string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.PedometerValue.Text = !string.IsNullOrEmpty(this.pedometer) ? this.pedometer : string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.UVValue.Text = !string.IsNullOrEmpty(this.uv) ? this.uv : string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.ContactValue.Text = !string.IsNullOrEmpty(this.contact) ? this.contact : string.Empty; }).AsTask();
        }

        async Task ClearFields()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.HeartRateValue.Text = string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.TemperatureValue.Text = string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.AccelerometerValue.Text = string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.CaloriesValue.Text = string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.DistanceValue.Text = string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.GyroscopeAValue.Text = string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.GyroscopeVValue.Text = string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.PedometerValue.Text = string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.UVValue.Text = string.Empty; }).AsTask();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.ContactValue.Text = string.Empty; }).AsTask();
        }
    }
}
