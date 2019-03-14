using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace si.birokrat.next.common.networking {
    public class Broadcaster {
        private readonly string _message = string.Empty;
        private readonly int _port = 0;
        private readonly UdpClient _client = null;
        private readonly IPEndPoint _endpoint = null;
        private readonly int _delay = 0;
        private readonly bool _verbose = false;
        private bool running = false;

        public Broadcaster(string message = "", int port = 10000, int delay = 1000, bool verbose = false) {
            _message = message;
            _port = port;
            _client = new UdpClient();
            _endpoint = new IPEndPoint(IPAddress.Broadcast, port);
            _delay = delay;
            _verbose = verbose;
        }

        public void Start() {
            running = true;

            new Thread(() => {
                if (_verbose) {
                    Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] Started broadcasting");
                }

                while (running) {
                    Send(_message);
                    Thread.Sleep(_delay);
                }
            }).Start();
        }

        public void Stop() {
            running = false;
            _client.Close();

            if (_verbose) {
                Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] Stopped broadcasting");
            }
        }

        private void Send(string message) {
            byte[] payload = Encoding.UTF8.GetBytes(message);
            _client.Send(payload, payload.Length, _endpoint);

            if (_verbose) {
                Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] Broadcast message: {message}");
            }
        }
    }
}
