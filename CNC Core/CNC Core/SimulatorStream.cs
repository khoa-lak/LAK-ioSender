using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CNC.Core
{
    public class SimulatorStream : StreamComms
    {
        private Dispatcher dispatcher;
        public event DataReceivedHandler DataReceived;

        public SimulatorStream(Dispatcher dispatcher)
        {
            Comms.com = this;
            this.dispatcher = dispatcher;
            IsOpen = true;
            
            // Send welcome message on startup
            Task.Run(async () =>
            {
                await Task.Delay(100);
                SendResponse("grblHAL 1.1f [']' for help]\r\n");
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
                SendResponse("<Idle|MPos:0.000,0.000,0.000|Bf:15,128|FS:0,0|WCO:0.000,0.000,0.000>\r\n");
            }
            else if (data == 0x18) // Ctrl-X (Reset)
            {
                SendResponse("grblHAL 1.1f [']' for help]\r\n");
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
            ProcessCommand(command + "\r");
        }

        public string GetReply(string command)
        {
            return "ok";
        }

        public void AwaitAck() { }
        public void AwaitAck(string command) { }
        public void AwaitResponse(string command) { }
        public void AwaitResponse() { }
        public void PurgeQueue() { }

        private void ProcessCommand(string command)
        {
            string cmd = command.Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            if (cmd == "?")
            {
                SendResponse("<Idle|MPos:0.000,0.000,0.000|Bf:15,128|FS:0,0|WCO:0.000,0.000,0.000>\r\n");
                return;
            }

            if (cmd.Equals("$I", StringComparison.OrdinalIgnoreCase))
            {
                SendResponse("[ver:1.1f.20210214:]\r\n[INPUT:1.1f.20210214:]\r\n[MSG:grblHAL simulator]\r\nok\r\n");
                return;
            }

            if (cmd.Equals("$G", StringComparison.OrdinalIgnoreCase))
            {
                SendResponse("[GC:G0 G54 G17 G21 G90 G94 M5 M9 T0 F0 S0]\r\nok\r\n");
                return;
            }

            if (cmd.Equals("$#", StringComparison.OrdinalIgnoreCase))
            {
                SendResponse("[G54:0.000,0.000,0.000]\r\n[G55:0.000,0.000,0.000]\r\n[G56:0.000,0.000,0.000]\r\n[G57:0.000,0.000,0.000]\r\n[G58:0.000,0.000,0.000]\r\n[G59:0.000,0.000,0.000]\r\n[G28:0.000,0.000,0.000]\r\n[G30:0.000,0.000,0.000]\r\n[G92:0.000,0.000,0.000]\r\n[TLO:0.000]\r\n[PRB:0.000,0.000,0.000:0]\r\nok\r\n");
                return;
            }

            if (cmd.Equals("$", StringComparison.OrdinalIgnoreCase) || cmd.Equals("$$", StringComparison.OrdinalIgnoreCase))
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
                sb.AppendLine("$130=200.000");
                sb.AppendLine("$131=200.000");
                sb.AppendLine("$132=200.000");
                sb.AppendLine("ok");
                SendResponse(sb.ToString());
                return;
            }

            // General ack for any other commands
            SendResponse("ok\r\n");
        }

        private void SendResponse(string data)
        {
            if (DataReceived != null)
            {
                dispatcher.BeginInvoke(new System.Action(() =>
                {
                    string[] lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (i == lines.Length - 1 && string.IsNullOrEmpty(lines[i]))
                            break;
                        
                        string line = lines[i] + "\r";
                        Reply = line;
                        DataReceived.Invoke(line);
                    }
                }));
            }
        }
    }
}
