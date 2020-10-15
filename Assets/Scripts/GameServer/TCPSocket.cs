namespace SYBOClientTest
{
    using System;
    using System.Net.Sockets;
    using UnityEngine;
    using Packets;
    using Interfases;

    public sealed class TCP : IConectionProtocolClient
    {
        private TcpClient socket;
        private NetworkStream stream;
        private string debugColor = "#990099";

        private OnMessageReceived onMessageReceivedCallback;
        public OnMessageReceived OnMessageReceivedCallback
        {
            get { return onMessageReceivedCallback; }
            set { onMessageReceivedCallback = value; }
        }

        private Action conectionCallback;

        private Packet receivedData;
        private byte[] receiveBuffer;

        private string address;
        public string Address
        {
            set { address = value; }
        }

        private int port;
        public int Port
        {
            set { port = value; }
        }

        private int bufferSize;
        public int BufferSize
        {
            set { bufferSize = value; }
        }

        public bool Conected
        {
            get
            {
                if (socket == null)
                {
                    return false;
                }
                else
                {
                    return socket.Connected;
                }
            }
        }

        private bool awaitingResponse;
        public bool AwaitingResponse
        {
            get { return awaitingResponse; }
        }

        public void Connect(Action OnConectionComplete)
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = bufferSize,
                SendBufferSize = bufferSize,

            };

            receiveBuffer = new byte[bufferSize];
            conectionCallback += OnConectionComplete;
            Debug.Log($"<color={debugColor}>Connecting to server...</color>");
            var result = socket.BeginConnect(address, port, ConnectCallback, socket);

            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
        }

        private void ConnectCallback(IAsyncResult result)
        {
            if (socket.Connected)
            {
                socket.EndConnect(result);

                Debug.Log($"<color={debugColor}>Connection Succeded!</color>");
                stream = socket.GetStream();

                receivedData = new Packet();

                // Enable the service to send messages
                awaitingResponse = false;

                Debug.Log($"<color={debugColor}>Start listening.</color>");
                stream.BeginRead(receiveBuffer, 0, bufferSize, ReceiveCallback, null);
            }
            else
            {
                Debug.Log($"<color={debugColor}>Connection Failed!</color>");
                socket.Close();
                socket.Dispose();
                socket = null;
            }

            conectionCallback.Invoke();
            conectionCallback = null;
        }

        public void Disconect()
        {
            try
            {
                stream.Close();
                socket.Close();
                Debug.Log($"<color={debugColor}>Disconnection protocol started.</color>");
            }
            catch (Exception ex)
            {
                Debug.Log($"<color={debugColor}>An error happened during the disconection protocol:</color>\n{ex}");
            }
        }

        public void SendData(string message)
        {
            Packet dataPacket = new Packet();
            dataPacket.Write(message);
            dataPacket.ClosePacket(address, port);
            // Disable the service to send messages
            awaitingResponse = true;
            SendPacket(dataPacket);
        }

        /// <summary>
        /// Sends the packet to my implemented server
        /// </summary>
        /// <param name="packet"></param>
        private void SendPacket(Packet packet)
        {
            try
            {
                stream = socket.GetStream();

                stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
            }
            catch (Exception ex)
            {
                Debug.Log($"<color={debugColor}>Error sending data to server via TCP:</color>\n{ex}");
                Disconect();
                Dispose();
            }
        }

        /// <summary>
        /// Process data sent by the server
        /// </summary>
        /// <param name="result"></param>
        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                // if socket disposed and is still conected
                if (((stream != null) && (socket != null)) && (socket.Connected))
                {
                    // I read the stream
                    int byteLength = stream.EndRead(result);

                    // If stream didn't sent data, I'll procede to close;
                    // Maybe I should close this by getting a message
                    if ((byteLength <= 0) && socket.Connected)
                    {
                        Disconect();
                        return;
                    }

                    // else I create a auxiliar buffer
                    byte[] data = new byte[byteLength];
                    // copies the data received in the auxiliar buffer
                    Array.Copy(receiveBuffer, data, byteLength);
                    // handles the data, processing it depending on a group of rules
                    receivedData.Reset(HandleData(data));
                    // continues listening the streams
                    stream.BeginRead(receiveBuffer, 0, bufferSize, ReceiveCallback, null);
                }
                else
                {
                    // Release all resources
                    Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"<color={debugColor}>Error receiving TCP data:</color>\n{ex}");
                Disconect();
                Dispose();
            }
        }

        /// <summary>
        /// Handles the received data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool HandleData(byte[] data)
        {
            int packetLength = 0;

            receivedData.SetBytes(data);

            // Checks if at least it has 4 bytes to read.
            // This is because the first data the packet provides is
            // the length of the packet;
            if (receivedData.UnreadLength() >= 4)
            {
                // Gets the packet lenght
                packetLength = receivedData.ReadInt();

                // checks if the packet has data checking the lenght
                // if the length is 0 we return with true to erase receivedData
                if (packetLength <= 0)
                {
                    return true;
                }
            }

            // while there's data tp read
            while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
            {
                // we read the rest of the data, that is valid data, not verification data
                byte[] packetBytes = receivedData.ReadBytes(packetLength);

                ThreadManager.ExecuteOnMainThread(
                    () =>
                    {
                    // Create a new packet of data only and provide it to 
                    // the proper method.
                    using (Packet packet = new Packet(packetBytes))
                        {
                            onMessageReceivedCallback(packet.ReadString());
                        // Enable the service to send messages
                        awaitingResponse = false;
                        }
                    }
                );

                // Reset the length verification of the packet
                packetLength = 0;
                // Checks if at least it has 4 bytes to read.
                // This is because the first data the packet provides is
                // the length of the packet;
                if (receivedData.UnreadLength() >= 4)
                {
                    // Gets the packet lenght
                    packetLength = receivedData.ReadInt();

                    // checks if the packet has data checking the lenght
                    // if the length is 0 we return with true to erase receivedData
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (packetLength <= 1)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Dispose the conecction and releases all resources.
        /// </summary>
        private void Dispose()
        {
            try
            {
                stream.Dispose();
                socket.Dispose();
                Debug.Log($"<color={debugColor}>Disconnection protocol started.</color>");
            }
            catch (Exception ex)
            {
                Debug.Log($"<color={debugColor}>An error happened during the disposal protocol:</color>\n{ex}");
            }
            finally
            {
                stream = null;
                socket = null;
            }
            Debug.Log($"<color={debugColor}>Disconnetion protocol completed. Now you are offline.</color>");
        }
    }
}

