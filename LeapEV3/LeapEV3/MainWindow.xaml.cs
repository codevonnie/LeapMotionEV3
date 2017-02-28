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

        // when Connect button is clicked
        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _brick = new Brick(new BluetoothCommunication("com6")); //connect to brick using bluetooth at the selected port
                //_brick = new Brick(new UsbCommunication());
                //_brick = new Brick(new NetworkCommunication("192.168.2.237"));
                //_brick.BrickChanged += brick_BrickChanged;
                await _brick.ConnectAsync(); //make connection to brick
                Output.Text = "Brick Connected"; //output text to screen that connection has taken place
                controller.EventContext = SynchronizationContext.Current; // used for dispatching events
                controller.FrameReady += newFrameHandler; //when a tracking frame is ready
                Output.Text += "        Leap Connected";

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
                float roll = rightHand.PalmNormal.Roll; //get roll value of the right hand
                float leftStrength = leftHand.GrabStrength; //get the value of grab hand gesture of left hand
                float pinch = rightHand.PinchStrength; //get value of pinch gesture of right hand
                List<Finger> fingerList = rightHand.Fingers;
                float fingers = fingerList.Count;
                float pitch = rightHand.Direction.Pitch;
                Output.Text = ("Strength: " + strength.ToString() + " Right Roll: " + roll.ToString() + "Finger count: " + fingers + "Right Pitch: " + pitch);

                //if right hand is a fist
                if (strength == 1)
                {
                    if (roll > 0)
                    {
                        await powerLeftWheel(); //send power to left motor
                    }
                    else if (roll <= -2)
                    {
                        await powerRightWheel(); //send power to right motor
                    }

                    else if (roll < 0 && roll > -1.9) //send power to both motors
                    {
                        await powerWheels();
                    }
                    else if (pitch > 0.5)
                    {
                        await StopMotorBC();
                    }

                }
                else if (strength != 1)
                {
                    if (roll > 0)
                    {
                        await powerLeftWheelReverse(); //send power to left motor
                    }
                    else if (roll <= -2)
                    {
                        await powerRightWheelReverse(); //send power to right motor
                    }

                    else if (roll < 0 && roll > -1.9) //send power to both motors
                    {
                        await powerWheelsReverse();
                    }

                    else if(pitch > 0.5)
                    {
                        await StopMotorBC();
                    }
                }
                
                //if left hand roll is less than 0 and not in a fist gesture
                if (leftStrength == 1)
                {
                    await grasp(); //send power to grabber motor
                }
                //if left hand roll is greater than 0 and not in a fist gesture
                else if (leftStrength != 1)
                {
                    await unGrasp(); //send power in reverse to grabber motor
                }
            }

        }//newFrameHandler

        private async Task powerLeftWheel()
        {
            //_brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.B, 50, 1000, false);
            await _brick.DirectCommand.TurnMotorAtPowerForTimeAsync(OutputPort.C, 80, 1000, false);
            //await _brick.BatchCommand.SendCommandAsync();
            MotorOutput.Text = "powerLeftWheel";
        }


        private async Task powerRightWheel()
        {
            //_brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.C, 50, 1000, false);
            //await _brick.BatchCommand.SendCommandAsync();
            await _brick.DirectCommand.TurnMotorAtPowerForTimeAsync(OutputPort.B, 80, 1000, false);
            MotorOutput.Text = "powerRightWheel";
        }

        //power to port A and C
        private async Task powerWheels()
        {
            _brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.B, 50, 1000, false);
            _brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.C, 50, 1000, false);
            await _brick.BatchCommand.SendCommandAsync();
            MotorOutput.Text = "powerBothWheel";
        }

        private async Task powerLeftWheelReverse()
        {
            //_brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.B, 50, 1000, false);
            await _brick.DirectCommand.TurnMotorAtPowerForTimeAsync(OutputPort.C, -50, 1000, false);
            //await _brick.BatchCommand.SendCommandAsync();
        }


        private async Task powerRightWheelReverse()
        {
            //_brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.C, 50, 1000, false);
            //await _brick.BatchCommand.SendCommandAsync();
            await _brick.DirectCommand.TurnMotorAtPowerForTimeAsync(OutputPort.B, -50, 1000, false);
        }

        //power to port A and C
        private async Task powerWheelsReverse()
        {
            _brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.B, -50, 1000, false);
            _brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.C, -50, 1000, false);
            await _brick.BatchCommand.SendCommandAsync();
        }

        
        //close grabber
        private async Task grasp()
        {
            await _brick.DirectCommand.TurnMotorAtPowerAsync(OutputPort.A, 100);
        }

        //open grabber
        private async Task unGrasp()
        {
            await _brick.DirectCommand.TurnMotorAtPowerAsync(OutputPort.A, -100);
        }
        
        //stop power to ports B and D
        //private async Task StopMotorB()
        //{
        //    await _brick.DirectCommand.StopMotorAsync(OutputPort.A, false);
        //    //await _brick.DirectCommand.StopMotorAsync(OutputPort.D, false);
        //    MotorOutput.Text = "Stop A";
        //}

        //stop power to ports B and C
        private async Task StopMotorBC()
        {
            await _brick.DirectCommand.StopMotorAsync(OutputPort.B, false);
            await _brick.DirectCommand.StopMotorAsync(OutputPort.C, false);
            MotorOutput.Text = "Stop B and C";
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
