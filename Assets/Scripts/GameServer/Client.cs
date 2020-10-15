namespace SYBOClientTest
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Diagnostics = System.Diagnostics;
    using UnityEngine;
    using Interfases;

    [RequireComponent(typeof(ThreadManager))]
    public class Client : MonoBehaviour
    {
        #region Singleton

        /// <summary>
        /// Singleton Field
        /// </summary>
        private static Client _instance;

        /// <summary>
        /// Singleton Property Access
        /// </summary>
        public static Client Instance
        {
            get { return _instance; }
        }

        #endregion
        
        public string serverAddress = "127.0.0.1";
        public int serverPort = 33328;
        public int dataBufferSize = 4096;

        private string debugColor = "#33FF00";
        private Diagnostics.Stopwatch stopwatch;
        private List<int> responseTimes;
        private IConectionProtocolClient conectionProtocol;
        public IConectionProtocolClient ConectionProtocol
        {
            set { conectionProtocol = value; }
        }

        public Action<string> OnServerDrivenEvent;

        public bool Connected
        {
            get
            {
                if (conectionProtocol != null)
                {
                    return conectionProtocol.Conected;
                }

                return false;
            }
        }

        public void ConnectToServer(Action whenDone)
        {
            if (conectionProtocol == null)
            {
                conectionProtocol = new TCP();
            }

            conectionProtocol.Address = serverAddress;
            conectionProtocol.Port = serverPort;
            conectionProtocol.BufferSize = dataBufferSize;
            conectionProtocol.OnMessageReceivedCallback += MessageReceived;

            if (!conectionProtocol.Conected)
            {
                conectionProtocol.Connect(
                    () => {
                        OnConnectionComplete();
                        whenDone.Invoke();
                    }
                );
            }
        }

        private void OnConnectionComplete()
        {
            stopwatch = new Diagnostics.Stopwatch();
            responseTimes = new List<int>();
        }

        public void SendRequest(string command, Action<string> responseCallback)
        {
            if (
                (conectionProtocol != null) &&
                (conectionProtocol.Conected)
            )
            {
                if (
                    (requestBeingResolved != null) ||
                    (conectionProtocol.AwaitingResponse)
                )
                {
                    requestsPending
                        .Enqueue(
                            new Tuple<string, Action<string>>(
                                command
                                , responseCallback
                            )
                    );
                }
                else
                {
                    stopwatch.Start();
                    requestBeingResolved = new Tuple<string, Action<string>>(
                        command
                        , responseCallback
                    );
                    conectionProtocol.SendData(requestBeingResolved.Item1);
                }
            }
        }

        private void MessageReceived(string responseMessage)
        {
            if (requestBeingResolved != null)
            {
                requestBeingResolved.Item2.Invoke(responseMessage);
                stopwatch.Stop();
                responseTimes.Add(stopwatch.Elapsed.Milliseconds);
                Debug.Log($"<color={debugColor}>The response was {responseMessage} and it took {responseTimes[(responseTimes.Count - 1)]} ms.</color>");
                requestBeingResolved = null;
            }
            else
            {
                OnServerDrivenEvent?.Invoke(responseMessage);
            }
        }

        public void DisconnectFromServer()
        {
            int averageResponseTime = 0;

            foreach (int responseTime in responseTimes)
            {
                averageResponseTime += responseTime;
            }

            if (responseTimes.Count != 0)
            {
                Debug.Log($"<color={debugColor}>The Average response time was {(averageResponseTime / responseTimes.Count)} ms.</color>");
            }

            responseTimes.Clear();
            conectionProtocol.Disconect();
            requestsPending.Clear();
            StopCoroutine(ConcurrecyManagement());
        }

        #region Concurrency Control

        private Queue<Tuple<string, Action<string>>> requestsPending;
        private Tuple<string, Action<string>> requestBeingResolved;
        private bool running;

        public IEnumerator ConcurrecyManagement()
        {
            while (running)
            {
                while (requestsPending.Count > 0)
                {
                    yield return new WaitWhile(
                        () => (
                            conectionProtocol.AwaitingResponse // the sockets is waiting for response
                            || requestBeingResolved != null // the request has been resolved. So is not null
                        )
                    );

                    requestBeingResolved = requestsPending.Dequeue();
                    stopwatch.Start();
                    conectionProtocol.SendData(requestBeingResolved.Item1);
                }
                yield return new WaitUntil(
                    () => (
                        (
                            (requestBeingResolved != null) // Something is being resolved
                            && (requestsPending.Count > 0) // and there's pending requests
                        )
                        || !running // or we're no running anymore. We're closing
                    )
                );
            }
        }

        #endregion

        #region Monobehaviour Methods

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(this);
                running = true;
                requestsPending = new Queue<Tuple<string, Action<string>>>();
                StartCoroutine(ConcurrecyManagement());
            }
            else if (_instance != null)
            {
                Debug.Log($"<color={debugColor}>Instance of the client already exists, destroying object!</color>");
                Destroy(this);
            }
        }

        void OnDestroy()
        {
            if (conectionProtocol.Conected)
            {
                requestsPending.Clear();
                conectionProtocol.Disconect();
            }
            running = false;
            StopCoroutine(ConcurrecyManagement());
            requestBeingResolved = null;
            conectionProtocol = null;
            stopwatch = null;
        }

        #endregion
    }
}
