#define SEND_RECEIVE_TRY_CATCH
//#undef SEND_RECEIVE_TRY_CATCH

#define DEBUG_RECEIVE
//#undef DEBUG_RECEIVE

using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System;

public class UDPInterface
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UDPInterface"/> class.
    /// </summary>
    /// <param name="port">Port.</param>
    /// <param name="ipv4">If set to <c>true</c> the protocol ipv4 will be used. If set to <c>false</c> ipv6 will be used .</param>
    public UDPInterface(int port, bool ipv4, out bool success, out SocketException outputException, UDPInterface localClient, NetworkMessageManager messageManager)
    {
        outputException = null;

        m_port = port;
        m_ipv4 = ipv4;
        m_localClientInterface = localClient;
        m_messageManager = messageManager;

        m_UDPClient = new UdpClient();

        if (ipv4)
        {
            m_localEndPoint = new IPEndPoint(IPAddress.Any, m_port);
        }
        else
        {
            m_localEndPoint = new IPEndPoint(IPAddress.IPv6Any, m_port);
        }

        try
        {
            m_UDPClient.Client.Bind(m_localEndPoint);
            try
            {
                // ignore connection reset errors
                int SIO_UDP_CONNRESET = -1744830452;
                m_UDPClient.Client.IOControl(
                    SIO_UDP_CONNRESET,
                    new byte[] { 0, 0, 0, 0 },
                    null
                );
                success = true;
            }
            catch (SocketException ex2)
            {
                outputException = ex2;
                success = false;
                Debug.LogError("UDPInterface: setting IOControl:SIO_UDP_CONNRESET failed ! \n" + ex2);
                GUIManager.singleton.displayMessage(ex2.ToString());
            }
        }
        catch (SocketException ex)
        {
            outputException = ex;
            success = false;
            return;
        }

        m_receiver_thread = new Thread(new ThreadStart(receiver_threadProcedure));
        m_receiver_thread.Start();

        m_sender_thread = new Thread(new ThreadStart(sender_threadProcedure));
        m_sender_thread.Start();
    }

    /// <summary>
    /// Interface for a local client
    /// </summary>
    public UDPInterface(bool ipv4, NetworkMessageManager messageManager)
    {
        m_isLocalClient = true;
        m_messageManager = messageManager;

        if (ipv4)
        {
            m_localEndPoint = new IPEndPoint(IPAddress.Any, m_port);
        }
        else
        {
            m_localEndPoint = new IPEndPoint(IPAddress.IPv6Any, m_port);
        }
    }

    ~UDPInterface()
    {
        dispose();
    }

    public bool m_discardInputPackages = false;
    public bool m_discardOutputPackages = false;

    private bool m_isLocalClient = false;
    private UDPInterface m_localClientInterface; // only on server with local client
    private UDPInterface m_ServerInterface; // only on local client
    private Queue<NetworkMessage> m_receivedFromLocalClient = new Queue<NetworkMessage>();
    private NetworkMessageManager m_messageManager = null;

    #region Receiver Variables

    private const int receiver_maxBufferSize = 1000;
    private int m_port;
    private UdpClient m_UDPClient = null;
    private IPEndPoint m_localEndPoint;
    private Thread m_receiver_thread = null;
    private long m_LOCKED_stopReceiverThread = 0;
    private long m_LOCKED_receiverThreadDone = 0;
    private Queue<NetworkMessage> m_LOCKED_receiverDataBuffer = new Queue<NetworkMessage>();
    private bool m_ipv4;
    private object m_receiveBufferLock = new object();
    private object m_dataToSendLock = new object();

    #endregion

    #region Sender Variables

    private List<NetworkMessage> m_dataToSend = new List<NetworkMessage>();
    private long m_LOCKED_cancelSenderThread = 0;
    private long m_LOCKED_senderThreadDone = 0;
    private Thread m_sender_thread = null;

    #endregion

    public void localClient_SetServer(UDPInterface server)
    {
        if (m_isLocalClient)
        {
            if (m_ServerInterface == null)
            {
                m_ServerInterface = server;
            }
            else
            {
                Debug.LogError("UDPInterface: Attempt to set UDP-Server-Reference, that is already set.");
            }
        }
        else
        {
            Debug.LogError("UDPInterface: Attempt to set UDP-Server-Reference while not local client.");
        }
    }

    public void addDataToSend(NetworkMessage message, IPEndPoint receiver)
    {
        if (m_isLocalClient)
        {
            NetworkMessage tempMessage = m_messageManager.getNetworkMessage();
            byte[] temp_data;
            int length;

            if (message.getOutput(out temp_data, out length, true, false))
            {
                if (tempMessage.setInputData(temp_data, length, false))
                {
                    m_ServerInterface.addDataReceivedFromLocalClient(tempMessage);
                }
                else
                {
                    Debug.LogWarning("UDPInterface: decoding local player data failed: ID: " + message.getOutputMessageBitView());
                    m_messageManager.recycleNetworkMessage(tempMessage);
                }
            }
            else
            {
                Debug.LogWarning("UDPInterface: encoding local player data failed: ID: " + message.messageContextID);
                m_messageManager.recycleNetworkMessage(tempMessage);
            }
            m_messageManager.recycleNetworkMessage(message);
        }
        else // is server or client
        {
            if (receiver.Equals(m_localEndPoint)) // to local client
            {
                NetworkMessage tempMessage = m_messageManager.getNetworkMessage();
                byte[] temp_data;
                int length;

                if (message.getOutput(out temp_data, out length, true, false))
                {
                    if (tempMessage.setInputData(temp_data, length, false))
                    {
                        m_localClientInterface.addDataReceived(tempMessage);
                    }
                    else
                    {
                        Debug.LogWarning("UDPInterface: decoding to local player data failed: ID: " + message.getOutputMessageBitView());
                        m_messageManager.recycleNetworkMessage(tempMessage);
                    }
                }
                else
                {
                    Debug.LogWarning("UDPInterface: encoding to local player data failed: ID: " + message.messageContextID);
                    m_messageManager.recycleNetworkMessage(tempMessage);
                }

                m_messageManager.recycleNetworkMessage(message);

                //m_localClientInterface.addDataReceived(message);
            }
            else // to external client 
            {
                message.iPEndPoint = receiver;

                lock (m_dataToSendLock)
                {
                    m_dataToSend.Add(message);
                }
            }
        }
    }

    /// <summary>
    /// Adds data to the received buffer. Use for local client only !
    /// </summary>
    /// <param name="message"></param>
    public void addDataReceived(NetworkMessage message)
    {
        if (m_isLocalClient)
        {
            message.iPEndPoint = m_localEndPoint;
            lock (m_receiveBufferLock)
            {
                m_LOCKED_receiverDataBuffer.Enqueue(message);
            }
        }
        else
        {
            Debug.LogWarning("UDPInterface: Attempt to add to received data, while not local client. This methode is intended for a local client only !");
        }
    }

    /// <summary>
    /// Adds data to the received buffer. Only a local client should call this.
    /// </summary>
    /// <param name="data"></param>
    public void addDataReceivedFromLocalClient(NetworkMessage message)
    {
        message.iPEndPoint = m_localEndPoint;
        m_receivedFromLocalClient.Enqueue(message);
    }

    public Queue<NetworkMessage> colletDataBuffer()
    {
        Queue<NetworkMessage> m_temp_collectDataBufferReturn = new Queue<NetworkMessage>();

        //mainProcedure(); // test

        // local client
        //Debug.Log("UDPInterface: m_receivedFromLocalClient.Count = " + m_receivedFromLocalClient.Count);
        while (m_receivedFromLocalClient.Count > 0)
        {
            m_temp_collectDataBufferReturn.Enqueue(m_receivedFromLocalClient.Dequeue());
        }
        // end local client

        lock (m_receiveBufferLock)
        {
            while (m_LOCKED_receiverDataBuffer.Count > 0)
            {
                m_temp_collectDataBufferReturn.Enqueue(m_LOCKED_receiverDataBuffer.Dequeue());
            }
        }

        return m_temp_collectDataBufferReturn;
    }

    private void receiver_threadProcedure() // seperate Thread
    {
        NetworkMessage temp_networkMessage;
        byte[] receivedData;
        IPEndPoint temp_IPEndPoint;
        int temp_MessageQueueCount;

        if (m_ipv4)
        {
            temp_IPEndPoint = new IPEndPoint(IPAddress.Any, m_port);
        }
        else
        {
            temp_IPEndPoint = new IPEndPoint(IPAddress.IPv6Any, m_port);
        }

        while (Interlocked.Read(ref m_LOCKED_stopReceiverThread) == 0)
        {
#if SEND_RECEIVE_TRY_CATCH
            try
            {
#endif
                // receive data
                receivedData = m_UDPClient.Receive(ref temp_IPEndPoint);

#if UNITY_EDITOR
                if (m_discardInputPackages)
                {
                    continue;
                }
#endif
                lock (m_receiveBufferLock)
                {
                    temp_MessageQueueCount = m_LOCKED_receiverDataBuffer.Count;
                }
                // write data
                if (temp_MessageQueueCount < receiver_maxBufferSize)
                {
                    temp_networkMessage = m_messageManager.getNetworkMessage();
                    if (temp_networkMessage.setInputData(receivedData, receivedData.Length, false))
                    {
                        temp_networkMessage.iPEndPoint = new IPEndPoint(temp_IPEndPoint.Address, temp_IPEndPoint.Port);
                        lock (m_receiveBufferLock)
                        {
                            m_LOCKED_receiverDataBuffer.Enqueue(temp_networkMessage);
#if DEBUG_RECEIVE
                            Debug.Log("UDPInterface: receiver_threadProcedure: received: [" + temp_networkMessage.iPEndPoint.ToString() +"]: " + temp_networkMessage.ToString());
#endif
                        }
                    }
                    else
                    {
                        Debug.LogWarning("UDPInterface: decoding message failed: " + NetworkMessage.showBinary(receivedData));
                        m_messageManager.recycleNetworkMessage(temp_networkMessage);
                    }
                }
                else
                {
                    string error = "UDP-Receiver: Buffer overflow \n";
                    lock (m_receiveBufferLock)
                    {
                        while (m_LOCKED_receiverDataBuffer.Count > 0)
                        {
                            NetworkMessage temp_clearedMessage = null;

                            temp_clearedMessage = m_LOCKED_receiverDataBuffer.Dequeue();

                            error += "Message: " + temp_clearedMessage.ToString() + "\n";
                            m_messageManager.recycleNetworkMessage(temp_clearedMessage);
                        }
                    }
                    Debug.LogError(error);
                }
#if SEND_RECEIVE_TRY_CATCH
            }
            catch (ThreadAbortException abortEx)
            {
                if (Interlocked.Read(ref m_LOCKED_stopReceiverThread) == 0)
                {
                    Debug.LogError("UDP-Receiver failed: " + abortEx + "\n" + abortEx.Source + "\n" + abortEx.StackTrace);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("UDP-Receiver failed: " + ex + "\n" + ex.Source + "\n" + ex.StackTrace);
            }
#endif
        }

        Interlocked.Exchange(ref m_LOCKED_receiverThreadDone, 1);
    }

    private void sender_threadProcedure()
    {
        int listCount;
        byte[] temp_data;
        int temp_dataLength;

        while (Interlocked.Read(ref m_LOCKED_cancelSenderThread) == 0)
        {
#if SEND_RECEIVE_TRY_CATCH
            try
            {
#endif
                listCount = m_dataToSend.Count;

                if (listCount == 0)
                {
                    Thread.Sleep(2);
                    continue;
                }

                for (int i = 0; i < listCount; i++)
                {
#if UNITY_EDITOR
                    if (m_discardOutputPackages)
                    {
                        m_messageManager.recycleNetworkMessage(m_dataToSend[i]);
                        continue;
                    }
#endif
                    if (m_dataToSend[i].getOutput(out temp_data, out temp_dataLength, true, false))
                    {
                        m_UDPClient.Send(temp_data, temp_dataLength, m_dataToSend[i].iPEndPoint);
                    }
                    else
                    {
                        Debug.LogWarning("UDPInterface: sender_threadProcedure: encoding message failed: " + m_dataToSend[i].getOutputMessageBitView());
                    }

                    m_messageManager.recycleNetworkMessage(m_dataToSend[i]);
                }

                lock (m_dataToSendLock)
                {
                    m_dataToSend.RemoveRange(0, listCount);
                }

#if SEND_RECEIVE_TRY_CATCH
            }
            catch (ThreadAbortException abortEx)
            {
                if (Interlocked.Read(ref m_LOCKED_cancelSenderThread) == 0)
                {
                    Debug.LogError("UDP-Sender failed: " + abortEx);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("UDP-Sender failed: " + ex + ":" + ex.Source + ":" + ex.StackTrace);
            }
#endif
        }
        Interlocked.Exchange(ref m_LOCKED_senderThreadDone, 1);
    }

    public void dispose()
    {
        //Debug.Log("UDPInterface: Disposing.");

        try
        {
            disposeReceiver();
            disposeSender();

            if (m_UDPClient != null)
            {
                m_UDPClient.Close();
                m_UDPClient = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("UDPInterface: dispose failed: " + ex);
        }
    }

    private void disposeReceiver()
    {
        if (m_receiver_thread != null)
        {
            Interlocked.Exchange(ref m_LOCKED_stopReceiverThread, 1);
            //while(Interlocked.Read(ref m_LOCKED_receiverThreadDone) == 0){} // wait for thread to end
            m_receiver_thread.Abort();
            m_receiver_thread = null;
        }
        if (m_LOCKED_receiverDataBuffer != null)
        {
            lock (m_LOCKED_receiverDataBuffer)
            {
                while (m_LOCKED_receiverDataBuffer.Count > 0)
                {
                    NetworkMessage message = m_LOCKED_receiverDataBuffer.Dequeue();
                    m_messageManager.recycleNetworkMessage(message);
                }
            }

            m_LOCKED_receiverDataBuffer = null;
        }
    }

    private void disposeSender()
    {
        if (m_sender_thread != null)
        {
            Interlocked.Exchange(ref m_LOCKED_cancelSenderThread, 1);
            //while(Interlocked.Read(ref m_LOCKED_senderThreadDone) == 0){} // wait for thread to end
            m_sender_thread.Abort();
            m_sender_thread = null;
        }
        if (m_dataToSend != null)
        {
            lock (m_dataToSend)
            {
                for (int i = 0; i < m_dataToSend.Count; i++)
                {
                    m_messageManager.recycleNetworkMessage(m_dataToSend[i]);
                }
                m_dataToSend.Clear();
            }

            m_dataToSend = null;
        }
    }

}