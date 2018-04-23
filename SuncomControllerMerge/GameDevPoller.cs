using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.DirectInput;
using System.Windows.Forms;
using vJoyInterfaceWrap;


namespace SuncomControllerMerge
{
    public class GameDevPoller
    {
        Joystick joystickDevice;
        Joystick throttleDevice;
        MainForm mainForm;
        vJoy joystick;
        vJoy.JoystickState iReport;
        bool vjoyEnabled;
        uint id = 1;
        double axisScale;
        DirectInput input;

        public void Init(MainForm parent)
        {
            mainForm = parent;
            ClearDevices();
            // Create one joystick object and a position structure.
            input = new DirectInput();
            joystick = new vJoy();
            iReport = new vJoy.JoystickState();
            vjoyEnabled = false;


            // Get the driver attributes (Vendor ID, Product ID, Version Number)
            if (!joystick.vJoyEnabled())
            {
                MessageBox.Show("vJoy driver not enabled: Failed Getting vJoy attributes.\n", "Error");
                return;
            }

            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(id);
            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                case VjdStat.VJD_STAT_FREE:
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    MessageBox.Show("vJoy Device is already owned by another feeder. Cannot continue", "Error");
                    return;
                case VjdStat.VJD_STAT_MISS:
                    MessageBox.Show("vJoy Device is not installed or disabled. Cannot continue", "Error");
                    return;
                default:
                    MessageBox.Show("vJoy Device general error. Cannot continue", "Error");
                    return;
            };

            // Make sure all needed axes and buttons are supported
            bool AxisX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X);
            bool AxisY = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y);
            bool AxisRX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RX);
            bool AxisRZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RZ);
            int nButtons = joystick.GetVJDButtonNumber(id);
            int ContPovNumber = joystick.GetVJDContPovNumber(id);
            if (!AxisX || !AxisY || !AxisRX || !AxisRZ || nButtons < 28 || ContPovNumber < 3)
            {
                MessageBox.Show("vJoy Device is not configured correctly. Must have X,Y,Rx,Ry analog axis, 28 buttons and 3 Analog POVs. Cannot continue\n", "Error");
                return;
            }

            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
            {
                MessageBox.Show("Failed to acquire vJoy device number", "Error");
                return;
            }

            long maxval = 0;
            joystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_X, ref maxval);
            axisScale = (double)maxval / 65536.0;
            mainForm.LblVjoyStat.Text = "Found. Ver: " + joystick.GetvJoySerialNumberString();
            vjoyEnabled = true;
        }

        void ClearDevices()
        {
            joystickDevice = null;
            throttleDevice = null;
            mainForm.LblThrottleStat.Text = "Waiting...";
            mainForm.LblJoystickStat.Text = "Waiting...";
        }

        public bool ValidateDeviceExistance()
        {
            // Find all the GameControl devices that are attached.
            
            IList<DeviceInstance> gameControllerList = input.GetDevices(DeviceClass.GameControl,
                DeviceEnumerationFlags.AttachedOnly);

            // check for devices existance
            foreach (DeviceInstance deviceInstance in gameControllerList)
            {
                // Move to the first device
                //if (DetectDevice(deviceInstance, "vjoy", ref vjoyDevice, mainForm.LblVjoyStat))
                    //continue;
                if (DetectDevice(deviceInstance, "suncom f15 talon", ref joystickDevice, mainForm.LblJoystickStat))
                    continue;
                if (DetectDevice(deviceInstance, "suncom sfs throttle", ref throttleDevice, mainForm.LblThrottleStat))
                    continue;
            }
            return (joystickDevice != null && throttleDevice != null && vjoyEnabled);
        }

        bool DetectDevice(DeviceInstance deviceInstance, string name, ref Joystick dev, Label lbl)
        {
            if (deviceInstance.ProductName.ToLower().StartsWith(name))
            {
                // create a device from this controller.
                dev = new Joystick(input, deviceInstance.InstanceGuid);
                dev.SetCooperativeLevel(mainForm.Handle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                // Tell DirectX that this is a Joystick.
                //dev.SetDataFormat(DeviceDataFormat.Joystick);
                // Finally, acquire the device.
                dev.Acquire();
                lbl.Text = "Found";
                return true;
            }
            return false;
        }

        public bool Poll()
        {
            JoystickState state;
            if ((joystickDevice == null) || (throttleDevice == null))
                return false;
            try
            {
                // poll the joystick
                throttleDevice.Poll();
                // update the joystick state field
                state = throttleDevice.GetCurrentState();
                iReport.bDevice = (byte)id;
                iReport.AxisZRot = (int)((double)state.X * axisScale);
                iReport.AxisXRot = (int)((double)state.Y * axisScale);


                // Set throttle buttons one by one
                bool [] buttons = state.Buttons;
                iReport.Buttons = (uint)0;
                int i;
                for (i = 0; i < buttons.Length; i++)
                    if (buttons[i])
                        iReport.Buttons |= (uint)1 << (i + 12);

                // throttle povs
                int[] povs = state.PointOfViewControllers;
                iReport.bHatsEx1 = (uint)povs[0];
                iReport.bHatsEx2 = (uint)povs[1];

                joystickDevice.Poll();
                // update the joystick state field
                state = joystickDevice.GetCurrentState();
                iReport.AxisX = (int)((double)state.X * axisScale);
                iReport.AxisY = (int)((double)state.Y * axisScale);

                // Set joystick buttons one by one
                buttons = state.Buttons;
                for (i = 0; i < buttons.Length; i++)
                    if (buttons[i])
                        iReport.Buttons |= (uint)1 << i;

                // joystick povs
                povs = state.PointOfViewControllers;
                iReport.bHats = (uint)povs[0]; // Neutral state
                /*** Feed the driver with the position packet - is fails then wait for input then try to re-acquire device ***/
                if (!joystick.UpdateVJD(id, ref iReport))
                {
                    MessageBox.Show("Feeding vJoy device failed - try to enable device then press OK", "Error");
                    joystick.AcquireVJD(id);
                }

            }
            catch (Exception)
            {
                ClearDevices();
                //MessageBox.Show("Connection to Joystick/Throttle was lost. Was it unplugged or locked by another application?", "Error");
                return false;
            }
            return true;
        }
    }
}
