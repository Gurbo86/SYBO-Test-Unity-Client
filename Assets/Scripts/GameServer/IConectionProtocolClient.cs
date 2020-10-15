namespace SYBOClientTest.Interfases
{
    using System;

    public delegate void OnMessageReceived(string message);
    // This interphase can be used for HTTP also
    public interface IConectionProtocolClient
    {
        /// <summary>
        /// Sets the address of the server.
        /// </summary>
        string Address { set; }

        /// <summary>
        /// Sets the port of the server.
        /// </summary>
        int Port { set; }

        /// <summary>
        /// Sets the buffer size contact the server.
        /// </summary>
        int BufferSize { set; }

        /// <summary>
        /// Returns true if conected, else false.
        /// </summary>
        bool Conected { get; }

        /// <summary>
        /// Returns true if conected, else false.
        /// </summary>
        bool AwaitingResponse { get; }

        /// <summary>
        /// Delegate informing when a message was received
        /// </summary>
        OnMessageReceived OnMessageReceivedCallback { get; set; }

        /// <summary>
        /// Try to connect to a server.
        /// </summary>
        /// <param name="OnConectionComplete">Responds true if conection was achieved, else false.</param>
        void Connect(Action OnConectionComplete);

        /// <summary>
        /// Sends a closed packet to the server.
        /// </summary>
        /// <param name="message"></param>
        void SendData(string message);

        /// <summary>
        /// Releases all conection resources. Keeps configuration.
        /// </summary>
        void Disconect();
    }
}