#define TRYCATCH
//#undef TRYCATCH

#define SEND_MESSAGE_CONSOLE
#undef SEND_MESSAGE_CONSOLE

#define RECEIVE_MESSAGE_CONSOLE
#undef RECEIVE_MESSAGE_CONSOLE

#define ADVANCED_DIAGNOSTIC
#undef ADVANCED_DIAGNOSTIC

#define DISPOSE_DEBUG
//#undef DISPOSE_DEBUG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;
using System.Threading;

public class TCP_Client
{
    const int RECEIVE_BUFFER_SIZE = 1000000;
    const int MESSAGE_BUFFER_SIZE = 1000000;

    public TCP_Client(int localPort, bool useIPV4, int externalPort, IPAddress externalIPAddress, out bool success, out SocketException outputException, NetworkMessageManager messageManager)
    {
        success = false;
        outputException = null;

        if (useIPV4)
        {
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        else
        {
            m_socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
        }
        IPEndPoint localEndPoint = null;

        try
        {
            if (useIPV4)
            {
                localEndPoint = new IPEndPoint(IPAddress.Any, localPort);

            }
            else // IPV6
            {
                localEndPoint = new IPEndPoint(IPAddress.IPv6Any, localPort);

            }
            m_socket.Bind(localEndPoint);
        }
        catch (SocketException ex)
        {
            outputException = ex;
            Debug.LogError(outputException);
            return;
        }

        m_externalEndPoint = new IPEndPoint(externalIPAddress, externalPort);
        try
        {
            m_socket.Connect(m_externalEndPoint);
        }
        catch (SocketException ex)
        {
            outputException = ex;
            Debug.LogError(outputException);
            return;
        }

        if (!m_socket.Connected)
        {
            Debug.LogError("TCP_Client: Is not connected !");
            return;
        }

        m_messageManager = messageManager;

        m_sending_thread = new Thread(new ThreadStart(thread_sendProcedure));
        m_sending_thread.Start();

        m_receiving_thread = new Thread(new ThreadStart(thread_receiveProcedure));
        m_receiving_thread.Start();

        success = true;
    }

    public TCP_Client(Socket socket, NetworkMessageManager messageManager)
    {
        if (socket == null)
        {
            Debug.LogError("TCP_Client: empty socket-reference !");
            return;
        }
        if (!socket.Connected)
        {
            Debug.LogError("TCP_Client: socket-reference not connected !");
            return;
        }

        try
        {
            m_externalEndPoint = socket.RemoteEndPoint as IPEndPoint;
        }
        catch (Exception ex)
        {
            Debug.LogError("TCP_Client: Parsing external IPEndPoint failed: " + ex);
        }

        m_messageManager = messageManager;

        m_socket = socket;

        m_sending_thread = new Thread(new ThreadStart(thread_sendProcedure));
        m_sending_thread.Start();

        m_receiving_thread = new Thread(new ThreadStart(thread_receiveProcedure));
        m_receiving_thread.Start();
    }

    /// <summary>
    /// initializes a fake TCP-Client with no socket for Network-communication. Used for a local client. Use addDataReceived() to simulate incoming network-data.
    /// </summary>
    public TCP_Client(NetworkMessageManager messageManager, IPEndPoint externalEndPoint)
    {
        m_isLocalClient = true;
        m_messageManager = messageManager;
        m_externalEndPoint = externalEndPoint;
    }

    ~TCP_Client()
    {
        dispose();
    }

    private ManualResetEvent m_sendThreadMRE = new ManualResetEvent(false);
    private NetworkMessageManager m_messageManager = null;
    private TCP_Server m_localClientTCPServer;
    private bool m_isLocalClient = false; // is fake client with no socket 
    private Socket m_socket;
    private Queue<NetworkMessage> m_dataToSend = new Queue<NetworkMessage>();
    private Queue<NetworkMessage> m_receivedDataBuffer = new Queue<NetworkMessage>();
    private long LOCKED_stopSendingThread = 0;
    private long LOCKED_stopReceivingThread = 0;
    private long LOCKED_lostConnection = 0;
    private Thread m_sending_thread = null;
    private Thread m_receiving_thread = null;
    private IPEndPoint m_externalEndPoint;
    public IPEndPoint externalIPEndPoint
    {
        get
        {
            return m_externalEndPoint;
        }
    }
    public bool lostConnection
    {
        get { return Interlocked.Read(ref LOCKED_lostConnection) == 1; }
    }

#if ADVANCED_DIAGNOSTIC
    private DateTime m_lastTimeReceivedThreadActive = DateTime.MinValue;
#endif

    public void addDataToSend(NetworkMessage newMessage)
    {
        if (m_isLocalClient)
        {
            NetworkMessage tempMessage = m_messageManager.getNetworkMessage();
            byte[] temp_data;
            int length;

            if (newMessage.getOutput(out temp_data, out length, true, false))
            {
                if (tempMessage.setInputData(temp_data, length, false))
                {
                    m_localClientTCPServer.addReceivedDataLocalClient(tempMessage);
                }
                else
                {
                    m_messageManager.recycleNetworkMessage(tempMessage);
                    Debug.LogWarning("TCP_Client: decoding local player data failed: ID: " + newMessage.getOutputMessageBitView());
                }
            }
            else
            {
                m_messageManager.recycleNetworkMessage(tempMessage);
                Debug.LogWarning("TCP_Client: encoding local player data failed: ID: " + newMessage.messageContextID);
            }

            m_messageManager.recycleNetworkMessage(newMessage);
        }
        else
        {
            lock (m_dataToSend)
            {
                m_dataToSend.Enqueue(newMessage);
                if (m_dataToSend.Count == 1)
                {
                    m_sendThreadMRE.Set(); // was zero before --> continue thread
                }
            }
        }
    }

    public void localClient_setServer(TCP_Server server)
    {
        if (m_isLocalClient)
        {
            if (m_localClientTCPServer == null)
            {
                m_localClientTCPServer = server;
            }
            else
            {
                Debug.LogError("TCP_Client: Attempt to set TCP-Server-Reference, that is already set.");
            }
        }
        else
        {
            Debug.LogError("TCP_Client: Attempt to set TCP-Server-Reference while not local client.");
        }
    }

    /// <summary>
    /// Adds data to the received buffer. Use for local client only !
    /// </summary>
    /// <param name="data"></param>
    public void addDataReceived(NetworkMessage data)
    {
        if (m_isLocalClient)
        {
            m_receivedDataBuffer.Enqueue(data);
        }
        else
        {
            Debug.LogWarning("TCP_Client: Attempt to add to received data, while not local client. This methode is intended for a local client only !");
        }
    }

    public Queue<NetworkMessage> colletDataBuffer()
    {
        Queue<NetworkMessage> returnValue = new Queue<NetworkMessage>();

        if (m_receivedDataBuffer.Count > 0)
        {
            lock (m_receivedDataBuffer)
            {
                while (m_receivedDataBuffer.Count > 0)
                {
                    returnValue.Enqueue(m_receivedDataBuffer.Dequeue());
                }
            }
        }

        return returnValue;
    }

    public string getDiagnostic()
    {
        string generalSocketState;

        if (m_isLocalClient)
        {
            generalSocketState = "local client";
        }
        else if (m_socket == null)
        {
            generalSocketState = "socket null";
        }
        else if (!m_socket.Connected)
        {
            generalSocketState = "socket not connected";
        }
        else
        {
            generalSocketState = "socket connected to " + m_socket.RemoteEndPoint.ToString();
        }

        string lostConnection;

        if (Interlocked.Read(ref LOCKED_lostConnection) == 1)
        {
            lostConnection = "connection lost";
        }
        else
        {
            lostConnection = "connected";
        }

        string lastTimeReceiveThread = "";

#if ADVANCED_DIAGNOSTIC
        lastTimeReceiveThread = " Last Time receive thread run: " + m_lastTimeReceivedThreadActive.Hour.ToString() + ":" + m_lastTimeReceivedThreadActive.Minute.ToString() + ":" + m_lastTimeReceivedThreadActive.Second.ToString() + ";";
#endif
        return "Socket State: " + generalSocketState + "; connection: " + lostConnection + ";" + lastTimeReceiveThread + " receivedDataBuffer.count: " + m_receivedDataBuffer.Count + ";";
    }

    private void thread_sendProcedure()
    {
        byte[] temp_data;
        int temp_dataListCount;
        NetworkMessage temp_networkMessage;
        int temp_messageLength;

        byte[] temp_lengthByte;

        while (Interlocked.Read(ref LOCKED_stopSendingThread) == 0)
        {
#if TRYCATCH
            try
            {
#endif
                if (!m_socket.Connected)
                {
                    Interlocked.Exchange(ref LOCKED_lostConnection, 1);
                    break;
                }

                temp_dataListCount = m_dataToSend.Count;

                lock (m_dataToSend)
                {
                    if (m_dataToSend.Count < 1)
                    {
                        m_sendThreadMRE.Reset();
                    }
                }

                m_sendThreadMRE.WaitOne();

                // send
                for (int i = 0; i < temp_dataListCount; i++)
                {
                    lock (m_dataToSend)
                    {
                        temp_networkMessage = m_dataToSend.Dequeue();
                    }

                    if (temp_networkMessage.getOutput(out temp_data, out temp_messageLength, true, false))
                    {
                        // prepend message length
                        for (int j = temp_messageLength - 1; j > -1; j--)
                        {
                            temp_data[j + 4] = temp_data[j];
                        }

                        temp_messageLength += 4;
                        temp_lengthByte = BitConverter.GetBytes(temp_messageLength);

                        for (int j = 0; j < 4; j++)
                        {
                            temp_data[j] = temp_lengthByte[j];
                        }

#if SEND_MESSAGE_CONSOLE
                        Debug.Log("TCP_Client: output: " + temp_networkMessage.ToString() + "; IP:" + ((IPEndPoint)m_socket.RemoteEndPoint).ToString());
#endif

                        // send
                        m_socket.Send(temp_data, temp_messageLength, SocketFlags.None);
                    }
                    else
                    {
                        Debug.LogWarning("TCP_Client: thread_sendProcedure: encoding message failed: " + temp_networkMessage.getOutputMessageBitView());
                    }

                    m_messageManager.recycleNetworkMessage(temp_networkMessage);
                }
#if TRYCATCH
            }
            catch (ThreadAbortException abortEx)
            {
                if (Interlocked.Read(ref LOCKED_stopSendingThread) == 0)
                {
                    Debug.LogError("TCP_Client send failed: " + abortEx);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("TCP_Client send failed: " + ex);
            }
#endif
        }
    }

    private void thread_receiveProcedure()
    {
        byte[] temp_streamBuffer = new byte[RECEIVE_BUFFER_SIZE];
        int temp_streamSegmentSize;
        NetworkMessage temp_message;
        byte[] temp_messageBuffer = new byte[MESSAGE_BUFFER_SIZE];
        int messageBufferIndex = 0;
        int messageLength = -1;
        int temp_indexDelta;

        while (Interlocked.Read(ref LOCKED_stopReceivingThread) == 0)
        {
#if TRYCATCH
            try
            {
#endif
#if ADVANCED_DIAGNOSTIC
                m_lastTimeReceivedThreadActive = DateTime.Now;
#endif

                if (!m_socket.Connected)
                {
                    Interlocked.Exchange(ref LOCKED_lostConnection, 1);
                    break;
                }

                temp_streamSegmentSize = m_socket.Receive(temp_streamBuffer);

                if (temp_streamSegmentSize == 0) // TCP "End"-message
                {
                    temp_message = m_messageManager.getNetworkMessage();

                    temp_message.messageType = NetworkMessage.MessageType.TCP_End;
                    lock (m_receivedDataBuffer)
                    {
                        m_receivedDataBuffer.Enqueue(temp_message);
                    }

                    Interlocked.Exchange(ref LOCKED_stopSendingThread, 1);
                    Interlocked.Exchange(ref LOCKED_stopReceivingThread, 1);

                    if (m_socket.Connected)
                    {
                        try
                        {
                            m_socket.Shutdown(SocketShutdown.Both);
                            m_socket.Close();
                        }
                        catch(Exception ex)
                        {
                            Debug.Log("TCP_Client: thread_receiveProcedure: error while closing connection after remote connection close message: " + ex);
                        }
                    }
                }
                else // normal message fragment
                {
                    //Debug.Log("messageBufferIndex: " + messageBufferIndex + "; temp_streamSegmentSize: "+ temp_streamSegmentSize);

                    for (int i = 0; i < temp_streamSegmentSize; i++)
                    {
                        temp_messageBuffer[messageBufferIndex] = temp_streamBuffer[i];
                        messageBufferIndex++;
                    }

                    while (messageBufferIndex > 4)
                    {
                        messageLength = BitConverter.ToInt32(temp_messageBuffer, 0);

                        if (messageBufferIndex >= messageLength) // message completed
                        {
                            // remove count from buffer array

                            temp_indexDelta = messageBufferIndex - 4;

                            for (int i = 0; i < temp_indexDelta; i++)
                            {
                                temp_messageBuffer[i] = temp_messageBuffer[i + 4];
                            }

                            messageBufferIndex -= 4;
                            messageLength -= 4;

                            // assemble message
                            temp_message = m_messageManager.getNetworkMessage();

                            if (temp_message.setInputData(temp_messageBuffer, messageLength))
                            {
#if RECEIVE_MESSAGE_CONSOLE
                                Debug.Log("TCP_Client: received message: " + temp_message.ToString());
#endif
                                lock (m_receivedDataBuffer)
                                {
                                    m_receivedDataBuffer.Enqueue(temp_message);
                                }
                            }
                            else
                            {
                                Debug.LogWarning("TCP_Client: thread_receiveProcedure: couldnt decode message: Length: \"" + messageLength + "\", Data: " + temp_message.getInputMessageBitView());
                                m_messageManager.recycleNetworkMessage(temp_message);
                            }

                            // rearrange array

                            temp_indexDelta = messageBufferIndex - messageLength;

                            for (int i = 0; i < temp_indexDelta; i++)
                            {
                                temp_messageBuffer[i] = temp_messageBuffer[messageLength + i];
                            }

                            messageBufferIndex -= messageLength;
                            messageLength = -1;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
#if TRYCATCH
            }
            catch (ThreadAbortException abortEx)
            {
                if (Interlocked.Read(ref LOCKED_stopReceivingThread) == 0)
                {
                    Debug.LogError("TCP_Client receive failed: " + abortEx);
                }
            }
            catch (SocketException socketEx)
            {
                if (socketEx.ErrorCode == 10053)
                {
                    Debug.LogWarning("TCP_Client receive failed: errorCode:" + socketEx.ErrorCode + ", Exception: " + socketEx);
                }
                if (socketEx.ErrorCode == 10054) // connection closed
                {
                    temp_message = m_messageManager.getNetworkMessage();

                    temp_message.messageType = NetworkMessage.MessageType.TCP_End;
                    lock (m_receivedDataBuffer)
                    {
                        m_receivedDataBuffer.Enqueue(temp_message);
                    }

                    Debug.LogWarning("TCP_Client receive failed: errorCode:" + socketEx.ErrorCode + ", Exception: " + socketEx);
                }
                else
                {
                    Debug.LogError("TCP_Client receive failed: errorCode:" + socketEx.ErrorCode + ", Exception: " + socketEx);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("TCP_Client receive failed: errorCode:" + ex);
                break;
            }
#endif
        }
    }

    public void stopReceivingThread()
    {
        Interlocked.Exchange(ref LOCKED_stopReceivingThread, 1);

        if (m_receiving_thread != null)
        {
            m_receiving_thread.Abort();
            m_receiving_thread = null;
        }
    }

    public void dispose()
    {
        try
        {
#if DISPOSE_DEBUG
            Debug.Log("TCP_Client: dispose");
#endif

            Interlocked.Exchange(ref LOCKED_stopSendingThread, 1);

            if (m_sendThreadMRE != null)
            {
                m_sendThreadMRE.Set();
                m_sendThreadMRE = null;
            }

            if (m_sending_thread != null)
            {
                m_sending_thread.Abort();
                m_sending_thread = null;
            }

            stopReceivingThread();

            if (m_dataToSend != null)
            {
                lock (m_dataToSend)
                {
                    if (m_messageManager != null)
                    {
                        while (m_dataToSend.Count > 0)
                        {
                            NetworkMessage message = m_dataToSend.Dequeue();
                            m_messageManager.recycleNetworkMessage(message);
                        }
                    }
                }
                m_dataToSend = null;
            }

            if (m_socket != null)
            {
                if (m_socket.Connected)
                {
                    m_socket.Disconnect(false); // TEST: new try
                    //m_socket.Shutdown(SocketShutdown.Both);
                    //m_tcpClient.Client.Disconnect(false);
                }
                m_socket.Close(1);
                //m_socket.Dispose(); // causes weird exceptions
                m_socket = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("TCP_Client: failed to dispose: " + ex);
        }
    }

}
