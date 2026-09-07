/*
 * SimulatorStream.cs - Grbl / grblHAL simulator stream for CNC Controls
 *
 */

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CNC.Core
{
    public class SimulatorStream : StreamComms
    {
        private Dispatcher dispatcher;
        public event DataReceivedHandler DataReceived;

        private double posX = 0.0, posY = 0.0, posZ = 0.0;
        private double wcoX = 0.0, wcoY = 0.0, wcoZ = 0.0;
        private int feedRate = 0, spindleRPM = 0;
        private bool isMetric = true;
        private bool isAbsolute = true;
        private bool isCheckMode = false;
        private string grblState = "Idle";

        public SimulatorStream(Dispatcher dispatcher)
        {
            Comms.com = this;
            this.dispatcher = dispatcher;
            IsOpen = true;
            CommandState = Comms.State.ACK;

            // Send welcome message on startup
            Task.Run(async () =>
            {
                await Task.Delay(100);
                SendResponse("grblHAL 1.1f [']' for help\r\n");
            });
        }

        public bool IsOpen { get; private set; }
        public int OutCount => 0;
        public string Reply { get; private set; } = string.Empty;
        public Comms.StreamType StreamType => Comms.StreamType.Serial;
        public Comms.State CommandState { get; set; } = Comms.State.ACK;
        public bool EventMode { get; set; } = true;
        public Action<int> ByteReceived { get; set; }

        public void Close()
        {
            IsOpen = false;
        }

        public int ReadByte() => -1;

        public void WriteByte(byte data)
        {
            // Handle single character real-time commands
            if (data == 0x87 || data == 0x80 || data == (byte)'?')
            {
                SendStatusReport();
            }
            else if (data == 0x18) // Ctrl-X (Reset)
            {
                grblState = "Idle";
                SendResponse("grblHAL 1.1f [']' for help\r\n");
            }
            else if (data == (byte)'~') // Cycle Start
            {
                if (grblState == "Hold")
                    grblState = "Run";
            }
            else if (data == (byte)'!') // Feed Hold
            {
                if (grblState == "Run")
                    grblState = "Hold";
            }
            else if (data == 0x85) // Jog Cancel
            {
                if (grblState == "Jog")
                    grblState = "Idle";
            }
        }

        public void WriteBytes(byte[] bytes, int len)
        {
            string cmd = Encoding.ASCII.GetString(bytes, 0, len);
            ProcessCommand(cmd);
        }

        public void WriteString(string data)
        {
            ProcessCommand(data);
        }

        public void WriteCommand(string command)
        {
            CommandState = Comms.State.AwaitAck;
            ProcessCommand(command);
        }

        public string GetReply(string command)
        {
            Reply = string.Empty;
            WriteCommand(command);
            AwaitResponse();
            return Reply;
        }

        public void AwaitAck()
        {
            while (Comms.com.CommandState == Comms.State.DataReceived || Comms.com.CommandState == Comms.State.AwaitAck)
                EventUtils.DoEvents();
        }

        public void AwaitAck(string command)
        {
            PurgeQueue();
            Reply = string.Empty;
            WriteCommand(command);

            while (Comms.com.CommandState == Comms.State.DataReceived || Comms.com.CommandState == Comms.State.AwaitAck)
                EventUtils.DoEvents();
        }

        public void AwaitResponse(string command)
        {
            PurgeQueue();
            Reply = string.Empty;
            WriteCommand(command);

            while (Comms.com.CommandState == Comms.State.AwaitAck)
                EventUtils.DoEvents();
        }

        public void AwaitResponse()
        {
            while (Comms.com.CommandState == Comms.State.AwaitAck)
                EventUtils.DoEvents();
        }

        public void PurgeQueue()
        {
            Reply = string.Empty;
        }

        private void SendStatusReport()
        {
            string curState = isCheckMode ? "Check" : grblState;
            string report = string.Format(CultureInfo.InvariantCulture,
                "<{0}|MPos:{1:F3},{2:F3},{3:F3}|Bf:15,128|FS:{4},{5}|WCO:{6:F3},{7:F3},{8:F3}>\r\n",
                curState, posX, posY, posZ, feedRate, spindleRPM, wcoX, wcoY, wcoZ);
            SendResponse(report);
        }

        private void ProcessCommand(string command)
        {
            string cmd = command.Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            // Split multiline commands if any
            string[] subCmds = cmd.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var sc in subCmds)
            {
                ProcessSingleCommand(sc.Trim());
            }
        }

        private void ProcessSingleCommand(string cmd)
        {
            if (string.IsNullOrEmpty(cmd)) return;

            if (cmd == "?")
            {
                SendStatusReport();
                return;
            }

            if (cmd.Equals("$I", StringComparison.OrdinalIgnoreCase) || cmd.Equals("$I+", StringComparison.OrdinalIgnoreCase))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[VER:1.1f.20230501:grblHAL]");
                sb.AppendLine("[OPT:V,15,128,3,0]");
                sb.AppendLine("[AXS:3:XYZ]");
                sb.AppendLine("[NEWOPT:TC,HOME]");
                sb.AppendLine("[FIRMWARE:grblHAL]");
                sb.AppendLine("[SIGNALS:HDERST123]");
                sb.AppendLine("ok");
                SendResponse(sb.ToString());
                return;
            }

            if (cmd.Equals("$G", StringComparison.OrdinalIgnoreCase))
            {
                SendResponse("[GC:G0 G54 G17 G21 G90 G94 M5 M9 T0 F0 S0]\r\nok\r\n");
                return;
            }

            if (cmd.Equals("$#", StringComparison.OrdinalIgnoreCase))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "[G54:{0:F3},{1:F3},{2:F3}]", wcoX, wcoY, wcoZ));
                sb.AppendLine("[G55:0.000,0.000,0.000]");
                sb.AppendLine("[G56:0.000,0.000,0.000]");
                sb.AppendLine("[G57:0.000,0.000,0.000]");
                sb.AppendLine("[G58:0.000,0.000,0.000]");
                sb.AppendLine("[G59:0.000,0.000,0.000]");
                sb.AppendLine("[G28:0.000,0.000,0.000]");
                sb.AppendLine("[G30:0.000,0.000,0.000]");
                sb.AppendLine("[G92:0.000,0.000,0.000]");
                sb.AppendLine("[TLO:0.000]");
                sb.AppendLine("[PRB:0.000,0.000,0.000:0]");
                sb.AppendLine("ok");
                SendResponse(sb.ToString());
                return;
            }

            if (cmd.Equals("$", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("$$", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("$+", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("$ES", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("$EG", StringComparison.OrdinalIgnoreCase))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("$0=10");
                sb.AppendLine("$1=25");
                sb.AppendLine("$2=0");
                sb.AppendLine("$3=0");
                sb.AppendLine("$4=0");
                sb.AppendLine("$5=0");
                sb.AppendLine("$6=0");
                sb.AppendLine("$10=1");
                sb.AppendLine("$11=0.010");
                sb.AppendLine("$12=0.002");
                sb.AppendLine("$13=0");
                sb.AppendLine("$14=0");
                sb.AppendLine("$20=0");
                sb.AppendLine("$21=0");
                sb.AppendLine("$22=0");
                sb.AppendLine("$23=0");
                sb.AppendLine("$24=25.000");
                sb.AppendLine("$25=500.000");
                sb.AppendLine("$26=250");
                sb.AppendLine("$27=1.000");
                sb.AppendLine("$30=1000");
                sb.AppendLine("$31=0");
                sb.AppendLine("$32=0");
                sb.AppendLine("$100=250.000");
                sb.AppendLine("$101=250.000");
                sb.AppendLine("$102=250.000");
                sb.AppendLine("$110=500.000");
                sb.AppendLine("$111=500.000");
                sb.AppendLine("$112=500.000");
                sb.AppendLine("$120=10.000");
                sb.AppendLine("$121=10.000");
                sb.AppendLine("$122=10.000");
                sb.AppendLine("$130=300.000");
                sb.AppendLine("$131=300.000");
                sb.AppendLine("$132=100.000");
                sb.AppendLine("ok");
                SendResponse(sb.ToString());
                return;
            }

            if (cmd.Equals("$EA", StringComparison.OrdinalIgnoreCase) || cmd.Equals("$EE", StringComparison.OrdinalIgnoreCase))
            {
                SendResponse("ok\r\n");
                return;
            }

            if (cmd.StartsWith("$spindle", StringComparison.OrdinalIgnoreCase))
            {
                SendResponse("[SPINDLE:0|0|0|*V|Default|0.0|1000.0]\r\nok\r\n");
                return;
            }

            if (cmd.StartsWith("$tool", StringComparison.OrdinalIgnoreCase))
            {
                SendResponse("ok\r\n");
                return;
            }

            if (cmd.Equals("$X", StringComparison.OrdinalIgnoreCase))
            {
                grblState = "Idle";
                SendResponse("[MSG:Caution: Unlocked]\r\nok\r\n");
                return;
            }

            if (cmd.Equals("$H", StringComparison.OrdinalIgnoreCase))
            {
                posX = posY = posZ = 0.0;
                grblState = "Idle";
                SendResponse("ok\r\n");
                return;
            }

            if (cmd.Equals("$C", StringComparison.OrdinalIgnoreCase))
            {
                isCheckMode = !isCheckMode;
                grblState = isCheckMode ? "Check" : "Idle";
                SendResponse(isCheckMode ? "[MSG:Check mode on]\r\nok\r\n" : "[MSG:Check mode off]\r\nok\r\n");
                return;
            }

            // Jog command: $J=...
            if (cmd.StartsWith("$J=", StringComparison.OrdinalIgnoreCase))
            {
                SimulateMotion(cmd.Substring(3));
                SendResponse("ok\r\n");
                return;
            }

            // GCode motion simulation
            if (cmd.StartsWith("G", StringComparison.OrdinalIgnoreCase) || cmd.StartsWith("M", StringComparison.OrdinalIgnoreCase) || cmd.StartsWith("T", StringComparison.OrdinalIgnoreCase))
            {
                SimulateMotion(cmd);

                if (cmd.Contains("M30") || cmd.Contains("M2"))
                {
                    SendResponse("[MSG:Pgm End]\r\nok\r\n");
                    return;
                }

                SendResponse("ok\r\n");
                return;
            }

            // General ack for any other commands
            SendResponse("ok\r\n");
        }

        private void SimulateMotion(string gcode)
        {
            string upper = gcode.ToUpperInvariant();

            if (upper.Contains("G90")) isAbsolute = true;
            if (upper.Contains("G91")) isAbsolute = false;
            if (upper.Contains("G20")) isMetric = false;
            if (upper.Contains("G21")) isMetric = true;

            double scale = isMetric ? 1.0 : 25.4;

            // Coordinate zeroing: G10 L20 P1 X0 Y0 Z0
            if (upper.Contains("G10") && upper.Contains("L20"))
            {
                var mx = ExtractCoordinate(upper, 'X');
                if (mx.HasValue) wcoX = posX - (mx.Value * scale);
                var my = ExtractCoordinate(upper, 'Y');
                if (my.HasValue) wcoY = posY - (my.Value * scale);
                var mz = ExtractCoordinate(upper, 'Z');
                if (mz.HasValue) wcoZ = posZ - (mz.Value * scale);
                return;
            }

            // G92 Zeroing
            if (upper.Contains("G92") && !upper.Contains("G92.1"))
            {
                var mx = ExtractCoordinate(upper, 'X');
                if (mx.HasValue) wcoX = posX - (mx.Value * scale);
                var my = ExtractCoordinate(upper, 'Y');
                if (my.HasValue) wcoY = posY - (my.Value * scale);
                var mz = ExtractCoordinate(upper, 'Z');
                if (mz.HasValue) wcoZ = posZ - (mz.Value * scale);
                return;
            }

            // Feedrate
            var feedVal = ExtractCoordinate(upper, 'F');
            if (feedVal.HasValue) feedRate = (int)feedVal.Value;

            // Spindle
            var sVal = ExtractCoordinate(upper, 'S');
            if (sVal.HasValue) spindleRPM = (int)sVal.Value;

            // Motion coordinates
            var cx = ExtractCoordinate(upper, 'X');
            if (cx.HasValue)
            {
                double targetX = cx.Value * scale;
                posX = isAbsolute ? (targetX + wcoX) : (posX + targetX);
            }

            var cy = ExtractCoordinate(upper, 'Y');
            if (cy.HasValue)
            {
                double targetY = cy.Value * scale;
                posY = isAbsolute ? (targetY + wcoY) : (posY + targetY);
            }

            var cz = ExtractCoordinate(upper, 'Z');
            if (cz.HasValue)
            {
                double targetZ = cz.Value * scale;
                posZ = isAbsolute ? (targetZ + wcoZ) : (posZ + targetZ);
            }
        }

        private double? ExtractCoordinate(string gcode, char axis)
        {
            var match = Regex.Match(gcode, $@"{axis}\s*([-+]?[0-9]*\.?[0-9]+)");
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
            {
                return val;
            }
            return null;
        }

        private void SendResponse(string data)
        {
            if (DataReceived != null && IsOpen)
            {
                dispatcher.BeginInvoke(new System.Action(() =>
                {
                    string[] lines = data.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string rawLine in lines)
                    {
                        string line = rawLine.Trim();
                        if (string.IsNullOrEmpty(line))
                            continue;

                        Reply = line;
                        CommandState = Reply == "ok" ? Comms.State.ACK : (Reply.StartsWith("error") ? Comms.State.NAK : Comms.State.DataReceived);
                        DataReceived?.Invoke(line);
                    }
                }));
            }
        }
    }
}
