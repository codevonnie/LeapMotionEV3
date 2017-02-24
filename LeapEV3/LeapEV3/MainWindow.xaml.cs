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
    /// Control Lego Mindstorms Robot using gestures captured by a Leap Motion
    /// Libraries referenced Leap, Lego.Ev3.Core and Lego.Ev3.Desktop

    public partial class MainWindow : Window
    {
        private Brick _brick; //create instance of EV3 Brick
        private Controller controller = new Controller(); //create instance of Leap Motion Controller

        public MainWindow()
        {
            InitializeComponent();
        }

        // when the MainWindow loads, get the list of EV3 ports and set as a selection dropdown
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Ports.ItemsSource = SerialPort.GetPortNames();
            Ports.SelectedIndex = (Ports.ItemsSource as string[]).Length - 1;
        }

        // when Connect button is clicked
        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _brick = new Brick(new BluetoothCommunication((string)Ports.SelectedItem)); //connect to brick using bluetooth at the selected port
                //_brick = new Brick(new UsbCommunication());
                //_brick = new Brick(new NetworkCommunication("192.168.2.237"));
                //_brick.BrickChanged += brick_BrickChanged;
                await _brick.ConnectAsync(); //make connection to brick
                Output.Text = "Connected"; //output text to screen that connection has taken place
                controller.EventContext = SynchronizationContext.Current; // used for dispatching events
                controller.FrameReady += newFrameHandler; //when a tracking frame is ready

            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect: " + ex);
            }
        }//Connect_Click

        async void newFrameHandler(object sender, FrameEventArgs eventArgs)
        {
            Leap.Frame frame = eventArgs.frame;//returns the most recent frame of tracking data
            //if (frame.Hands.Count == 1)
            //{
            //    List<Hand> hands = frame.Hands;
            //    Hand firstHand = hands[0];
            //}

            //if two hands are counted in the frame
            if (frame.Hands.Count == 2)
            {
                List<Hand> hands = frame.Hands; //get a list of the hands detected in the frame
                // set hands from hand list
                Hand firstHand = hands[0];
                Hand secondHand = hands[1];

                Hand leftHand;
                Hand rightHand;

                //check which hand is the left hand and set left and right hand variables
                if (firstHand.IsLeft)
                {
                    leftHand = firstHand;
                    rightHand = secondHand;
                }
                else
                {
                    leftHand = secondHand;
                    rightHand = firstHand;
                }

                float strength = rightHand.GrabStrength; //get value of grab hand gesture for right hand
                float roll = leftHand.PalmNormal.Roll; //get roll value of the left hand
                float leftStrength = leftHand.GrabStrength; //get the value of grab hand gesture of left hand
                float pinch = rightHand.PinchStrength; //get value of pinch gesture of right hand
                Leap.Vector filteredHandPosition = leftHand.StabilizedPalmPosition;
                Output.Text = filteredHandPosition.ToString();

                //if right hand is a fist
                if (strength == 1)
                {
                    await powerWheel(); //send power to motor
                }
                else if (pinch == 1) //if right hand is pinching
                {
                    await powerWheelReverse(); //send power in reverse to motor
                }
                else
                {
                    await StopMotorAC(); //if no gesture detected, stop power to motor
                }

                //if left hand roll is less than 0 and not in a fist gesture
                if ((roll < 0) && (leftStrength != 1))
                {
                    await powerSteering(); //send power to motor
                }
                //if left hand roll is greater than 0 and not in a fist gesture
                else if ((roll > 0) && (leftStrength != 1))
                {
                    await powerSteeringReverse(); //send power in reverse to motor
                }
                else
                {
                    await StopMotorB(); //if no gesture detected, stop power to motor
                }
            }

        }//newFrameHandler

        //power to port A and C
        private async Task powerWheel()
        {
            _brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, -100, 1000, false);
            _brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.C, -100, 1000, false);
            await _brick.BatchCommand.SendCommandAsync();
        }

        //reverse power to port A and C
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
        //reverse power to ports B and D
        private async Task powerSteeringReverse()
        {
            _brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, -80, 1000, false);
            //_brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.D, -80, 1000, false);
            await _brick.BatchCommand.SendCommandAsync();
        }

        //stop power to ports B and D
        private async Task StopMotorB()
        {
            await _brick.DirectCommand.StopMotorAsync(OutputPort.B, false);
            await _brick.DirectCommand.StopMotorAsync(OutputPort.D, false);
        }

        //stop power to ports A and C
        private async Task StopMotorAC()
        {
            await _brick.DirectCommand.StopMotorAsync(OutputPort.A, false);
            await _brick.DirectCommand.StopMotorAsync(OutputPort.C, false);
        }
        
        //stop all motors
        private async Task StopMotorNow()
        {
            await _brick.DirectCommand.StopMotorAsync(OutputPort.All, false);
        }

        //disconnect from brick
        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            _brick.Disconnect();
        }
    }
}
