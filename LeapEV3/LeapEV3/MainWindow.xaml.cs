using System;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows;
using Lego.Ev3.Core;
using Lego.Ev3.Desktop;
using Leap;
using System.Threading;
using System.Collections.Generic;

namespace LeapEV3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Brick _brick;
        private Controller controller = new Controller();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Ports.ItemsSource = SerialPort.GetPortNames();
            Ports.SelectedIndex = (Ports.ItemsSource as string[]).Length - 1;
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _brick = new Brick(new BluetoothCommunication((string)Ports.SelectedItem));
                //_brick = new Brick(new UsbCommunication());
                //_brick = new Brick(new NetworkCommunication("192.168.2.237"));
                //_brick.BrickChanged += brick_BrickChanged;
                await _brick.ConnectAsync();
                Output.Text = "Connected";
                controller.EventContext = SynchronizationContext.Current;
                controller.FrameReady += newFrameHandler;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect: " + ex);
            }
        }//Connect_Click

        async void newFrameHandler(object sender, FrameEventArgs eventArgs)
        {
            Leap.Frame frame = eventArgs.frame;
            //if (frame.Hands.Count == 1)
            //{
            //    List<Hand> hands = frame.Hands;
            //    Hand firstHand = hands[0];
            //}
            if (frame.Hands.Count == 2)
            {
                List<Hand> hands = frame.Hands;
                Hand firstHand = hands[0];
                Hand secondHand = hands[1];

                if (firstHand.IsLeft)
                {
                    Hand leftHand = firstHand;
                    Hand rightHand = secondHand;

                    float strength = rightHand.GrabStrength;
                    float roll = leftHand.PalmNormal.Roll;
                    float leftStrength = leftHand.GrabStrength;
                    float pinch = rightHand.PinchStrength;
                    Leap.Vector filteredHandPosition = leftHand.StabilizedPalmPosition;
                    Output.Text = filteredHandPosition.ToString();
                    if (strength == 1)
                    {
                        await powerWheel();
                    }
                    else if (pinch == 1)
                    {
                        await powerWheelReverse();
                    }
                    else
                    {
                        await StopMotorANow();
                    }

                    if ((roll < 0) && (leftStrength != 1))
                    {
                        await powerSteering();
                    }
                    else if ((roll > 0) && (leftStrength != 1))
                    {
                        await powerBReverse();
                    }
                    else
                    {
                        await StopMotorBNow();
                    }
                }
                else
                {
                    Hand leftHand = secondHand;
                    Hand rightHand = firstHand;

                    float strength = rightHand.GrabStrength;
                    float roll = leftHand.PalmNormal.Roll;
                    float leftStrength = leftHand.GrabStrength;
                    float pinch = rightHand.PinchStrength;
                    Leap.Vector filteredHandPosition = leftHand.StabilizedPalmPosition;
                    Output.Text = filteredHandPosition.ToString();
                    if (strength == 1)
                    {
                        await powerWheel();
                    }
                    else if (pinch == 1)
                    {
                        await powerWheelReverse();
                    }
                    else
                    {
                        await StopMotorANow();
                    }

                    if ((roll < 0) && (leftStrength != 1))
                    {
                        await powerSteering();
                    }
                    else if ((roll > 0) && (leftStrength != 1))
                    {
                        await powerBReverse();
                    }
                    else
                    {
                        await StopMotorBNow();
                    }
                }

            }

        }
        //power to port A and C
        private async Task powerWheel()
        {
            _brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, -100, 1000, false);
            _brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.C, -100, 1000, false);
            await _brick.BatchCommand.SendCommandAsync();
        }

        private async Task powerWheelReverse()
        {
            _brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, 100, 1000, false);
            _brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.C, 100, 1000, false);
            await _brick.BatchCommand.SendCommandAsync();
        }

        //power to Port B and D
        private async Task powerSteering()  
        {
            _brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, 80, 1000, false);
            //_brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.D, 80, 1000, false);
            await _brick.BatchCommand.SendCommandAsync();
        }
        //reverse power to port B and D
        private async Task powerBReverse()
        {
            _brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, -80, 1000, false);
            //_brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.D, -80, 1000, false);
            await _brick.BatchCommand.SendCommandAsync();
        }


        private async Task StopMotorBNow()
        {
            await _brick.DirectCommand.StopMotorAsync(OutputPort.B, false);
            await _brick.DirectCommand.StopMotorAsync(OutputPort.D, false);
        }


        private async Task StopMotorANow()
        {
            await _brick.DirectCommand.StopMotorAsync(OutputPort.A, false);
            await _brick.DirectCommand.StopMotorAsync(OutputPort.C, false);
        }



        //stop all motors
        private async Task StopMotorNow()
        {
            await _brick.DirectCommand.StopMotorAsync(OutputPort.All, false);
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            _brick.Disconnect();
        }

        
    }
}
