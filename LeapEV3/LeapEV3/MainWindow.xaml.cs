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

                await _brick.ConnectAsync(); //make connection to brick
                Output.Text = "Brick Connected"; //output text to screen that connection has taken place
                controller.EventContext = SynchronizationContext.Current; // used for dispatching events
                controller.FrameReady += newFrameHandler; //when a tracking frame is ready
                Output.Text += "   |     Leap Connected";

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
                 float pitch = rightHand.Direction.Pitch; //pitch is the bend in the elbow
                Output.Text = ("Strength: " + strength.ToString() + " Right Roll: " + roll.ToString() + " Right Pitch: " + pitch);

                //if right hand is a fist
                    if (strength == 1)
                    {
                        if (pitch >= 0.8) // elbow is almost completely bent with hand up in the air
                        {
                            await StopMotorBC(); //stop motors
                        }
                        else if (roll >= 0.3) // if wrist is rolled to the right
                        {
                            await powerLeftWheel(); //send power to left motor
                        }
                        else if (roll <= -0.3) //if wrist is rolled to the left
                        {
                            await powerRightWheel(); //send power to right motor
                        }

                        else if (roll < 0.3 && roll > -0.3) //wrist is not rolled but centred
                        {
                            await powerWheels();
                        }

                }//if
                    //right hand is not a fist i.e. flat palm
                    else if (strength != 1)
                    {
                        if(pitch >= 0.8) // elbow is almost completely bent with hand up in the air
                        {
                            await StopMotorBC(); //stop motors
                        }

                        else if (roll >= 0.3) // if wrist is rolled to the right
                        {
                            await powerLeftWheelReverse(); //send power to left motor
                        }
                        else if (roll <= -0.3) //if wrist is rolled to the left
                        {
                            await powerRightWheelReverse(); //send power to right motor
                        }

                        else if (roll < 0.3 && roll > -0.3) //wrist is not rolled but centred
                        {
                            await powerWheelsReverse(); //send power to both motors
                        }

                }//else if

                //if left hand is in a fist gesture
                if (leftStrength == 1)
                {
                    await grasp(); //send power to grabber motor - i.e. close grabber
                }
                //if left hand roll is not in a fist gesture i.e. flat palm
                else if (leftStrength != 1)
                {
                    await unGrasp(); //send power in reverse to grabber motor - i.e. open grabber
                }
            }

        }//newFrameHandler


        private async Task powerLeftWheel()
        {
            //send direct command to motor C on robot to turn forward at power 80 for 1 second - turns robot right
            await _brick.DirectCommand.TurnMotorAtPowerForTimeAsync(OutputPort.C, 100, 1000, false);
            //MotorOutput.Text = "powerLeftWheel";
        }


        private async Task powerRightWheel()
        {
            //send direct command to motor B on robot to turn forward at power 80 for 1 second - turns robot left
            await _brick.DirectCommand.TurnMotorAtPowerForTimeAsync(OutputPort.B, 100, 1000, false);
            //MotorOutput.Text = "powerRightWheel";
        }

        //send batch command to robot to turn both motors B and C at a power of 50 for 1 second - powers the robot straight forward
        private async Task powerWheels()
        {
            _brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.B, 80, 1000, false);
            _brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.C, 80, 1000, false);
            await _brick.BatchCommand.SendCommandAsync(); //send all batch commands
            //MotorOutput.Text = "powerWheels";
        }

        private async Task powerLeftWheelReverse()
        {
            //send direct command to motor C on robot to turn in reverse at power -50 for 1 second - robot reverses to the right
            await _brick.DirectCommand.TurnMotorAtPowerForTimeAsync(OutputPort.C, -100, 1000, false);
            //MotorOutput.Text = "powerLeftWheelReverse";
        }


        private async Task powerRightWheelReverse()
        {
            //send direct command to motor B on robot to turn in reverse at power -50 for 1 second - robot reverse to the left
            await _brick.DirectCommand.TurnMotorAtPowerForTimeAsync(OutputPort.B, -100, 1000, false);
            //MotorOutput.Text = "powerRightWheelReverse";
        }

        //send batch command to robot to turn both motors B and C at a power of -50 for 1 second - powers the robot backward in a straight direction
        private async Task powerWheelsReverse()
        {
            _brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.B, -80, 1000, false);
            _brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.C, -80, 1000, false);
            await _brick.BatchCommand.SendCommandAsync();
            //MotorOutput.Text = "powerWheelsReverse";
        }


        //send direct command to robot to turn motor A at a power of 100 - robot closes gripper
        private async Task grasp()
        {
            await _brick.DirectCommand.TurnMotorAtPowerAsync(OutputPort.A, 100);
        }

        //send direct command to robot to turn motor A at a power of -100 - robot opens gripper
        private async Task unGrasp()
        {
            await _brick.DirectCommand.TurnMotorAtPowerAsync(OutputPort.A, -100);
        }

        //stop direct commands to motors B and C and stops all power to motors
        private async Task StopMotorBC()
        {
            await _brick.DirectCommand.StopMotorAsync(OutputPort.B, false);
            await _brick.DirectCommand.StopMotorAsync(OutputPort.C, false);
            //MotorOutput.Text = "Stop B and C";
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
            Output.Text = "Disconnected";
        }
    }
}
