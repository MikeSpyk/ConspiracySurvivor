#define DEBUG_TRY_CATCH
//#undef DEBUG_TRY_CATCH

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Threading;

public class TCP_Server
{
    private const int DELAYED_SHUTDOWN_TIME = 100; // ms | delay after a connection end request before a connection gets shutdown. needed, to be able to send a connection message with a reason for the disconnect
    private const int CONNECTION_LISTENER_SLEEP = 100; // ms | how long will the thread sleep if no new connection has occured

    public TCP_Server(int port, bool ipv4, out bool success, TCP_Client localClientHost, NetworkMessageManager messageManager)
    {
        success = false;
        m_localClient = localClientHost;
        m_port = port;
        m_messageManager = messageManager;

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
            m_TCPListener = new TcpListener(m_localEndPoint);
            m_TCPListener.Start();
            success = true;
        }
        catch (Exception ex)
        {
            Debug.LogError("TCP-Server: Binding TCP-Host failed: " + ex);
            GUIManager.singleton.displayMessage("TCP-Server: Binding TCP-Host failed: " + ex);
            return;
        }

        m_connectionManagementThread = new Thread(new ThreadStart(connectionManagement_ThreadProcedure));
        m_connectionManagementThread.Start();

        if (localClientHost != null)
        {
            m_EndPoint_TCPClient.Add(m_localEndPoint, null); // add local player
            m_acticeConnections.Add(localClientHost);
        }
    }

    ~TCP_Server()
    {
        dispose();
    }

    private int m_port;
    private IPEndPoint m_localEndPoint;
    private TcpListener m_TCPListener;
    private List<TCP_Client> m_acticeConnections = new List<TCP_Client>();
    private List<TCP_Client> m_lostConnectionsClients = new List<TCP_Client>();
    private Dictionary<IPEndPoint, TCP_Client> m_EndPoint_TCPClient = new Dictionary<IPEndPoint, TCP_Client>();
    private Thread m_connectionManagementThread;
    private long LOCKED_Stop_Connection_Thread = 0;
    private List<NetworkMessage> m_localClientData = new List<NetworkMessage>();
    private TCP_Client m_localClient;
    private List<IPEndPoint> m_newTCPClients = new List<IPEndPoint>();
    private NetworkMessageManager m_messageManager = null;
    private List<TCP_Client> m_delayedShutdownClients = new List<TCP_Client>();
    private List<DateTime> m_delayedShutdownEndTimes = new List<DateTime>();

    public IPEndPoint localEndPoint
    {
        get
        {
            return m_localEndPoint;
        }
    }

    /// <summary>
    /// Closes TCP-Sockets that are no longer connected and returns the IPEndPoints of these sockets
    /// </summary>
    /// <returns></returns>
    public List<IPEndPoint> closeLostConnections()
    {
        List<IPEndPoint> returnValue = new List<IPEndPoint>();

#if DEBUG_TRY_CATCH
        try
        {
#endif
            int lostCount = m_lostConnectionsClients.Count;

            for (int i = 0; i < lostCount; i++)
            {
                returnValue.Add(new IPEndPoint(m_lostConnectionsClients[i].externalIPEndPoint.Address, m_lostConnectionsClients[i].externalIPEndPoint.Port));
            }

            for (int i = 0; i < lostCount; i++)
            {
                lock (m_lostConnectionsClients)
                {
                    m_lostConnectionsClients[0].dispose();
                    m_lostConnectionsClients.RemoveAt(0);
                }
            }

#if DEBUG_TRY_CATCH
        }
        catch (Exception ex)
        {
            Debug.LogError("TCP_Server: closeLostConnections: Exception: " + ex);
        }
#endif
        return returnValue;
    }

    public IPEndPoint[] collectNewConnections()
    {
        IPEndPoint[] returnValue = m_newTCPClients.ToArray();
        lock (m_newTCPClients)
        {
            m_newTCPClients.Clear();
        }
        return returnValue;
    }

    public Queue<NetworkMessage> collectAllClientDataBuffers()
    {
        Queue<NetworkMessage> returnValue = new Queue<NetworkMessage>();
        Queue<NetworkMessage> temp_receivedClientMessages;
        NetworkMessage temp_message;

        for (int i = 0; i < m_acticeConnections.Count; i++)
        {
            if (m_acticeConnections[i] == null)
            {
                Debug.LogWarning("TCP_Server: collectAllClientDataBuffers: TCP Client is null");
            }
            else
            {
                temp_receivedClientMessages = m_acticeConnections[i].colletDataBuffer();

                while (temp_receivedClientMessages.Count > 0)
                {
                    temp_message = temp_receivedClientMessages.Dequeue();
                    temp_message.iPEndPoint = m_acticeConnections[i].externalIPEndPoint;
                    returnValue.Enqueue(temp_message);
                }
            }
        }

        // data from local client
        for (int i = 0; i < m_localClientData.Count; i++)
        {
            m_localClientData[i].iPEndPoint = m_localEndPoint;
            returnValue.Enqueue(m_localClientData[i]);
        }
        m_localClientData.Clear();

        return returnValue;
    }

    public void closeConnectionForClient(IPEndPoint client, bool delayedShutdown = true)
    {
        bool foundMember = false;

#if DEBUG_TRY_CATCH
        try
        {
#endif
            for (int i = 0; i < m_acticeConnections.Count; i++)
            {
                if (m_acticeConnections[i].externalIPEndPoint == client)
                {
                    if (delayedShutdown)
                    {
                        m_acticeConnections[i].stopReceivingThread();
                        m_delayedShutdownClients.Add(m_acticeConnections[i]);
                        m_delayedShutdownEndTimes.Add(DateTime.Now + new TimeSpan(0, 0, 0, 0, DELAYED_SHUTDOWN_TIME));
                    }
                    else
                    {
                        m_acticeConnections[i].dispose();
                    }

                    lock (m_acticeConnections)
                    {
                        m_EndPoint_TCPClient.Remove(m_acticeConnections[i].externalIPEndPoint);
                        m_acticeConnections.RemoveAt(i);
                    }
                    foundMember = true;
                    break;
                }
            }

#if DEBUG_TRY_CATCH
        }
        catch (Exception ex)
        {
            Debug.LogError("TCP_Server: closeConnectionForClient: Exception: " + ex);
        }
#endif
        if (foundMember)
        {
            Debug.Log("TCP_Server: Closed TCP-Connection for client: " + client.ToString());
        }
        else
        {
            Debug.LogWarning("TCP_Server: could not find client to close Connection. Client: " + client.ToString());
        }
    }

    public void addReceivedDataLocalClient(NetworkMessage data)
    {
        m_localClientData.Add(data);
    }

    public void addDataToSend_OneClient(NetworkMessage message, IPEndPoint client)
    {
        if (m_EndPoint_TCPClient.ContainsKey(client))
        {
            if (client.Equals(m_localEndPoint)) // data for local client
            {
                NetworkMessage tempMessage = m_messageManager.getNetworkMessage();
                byte[] temp_data;
                int length;

                if (message.getOutput(out temp_data, out length, true, false))
                {
                    if (tempMessage.setInputData(temp_data, length, false))
                    {
                        m_localClient.addDataReceived(tempMessage);
                    }
                    else
                    {
                        Debug.LogWarning("TCP_Client: decoding to local player data failed: ID: " + message.getOutputMessageBitView());
                    }
                }
                else
                {
                    Debug.LogWarning("TCP_Client: encoding to local player data failed: ID: " + message.messageContextID);
                }
                m_messageManager.recycleNetworkMessage(message);
            }
            else // data for external client
            {
                m_EndPoint_TCPClient[client].addDataToSend(message);
            }
        }
        else
        {
            Debug.LogWarning("TCP_Server: Data for unknown client: " + client.ToString());
            m_messageManager.recycleNetworkMessage(message);
        }
    }

    public void addDataToSend_AllClients(NetworkMessage message)
    {
        byte[] temp_bytes;
        int temp_int;

        if (message.getOutput(out temp_bytes, out temp_int, true, false))
        {
            for (int i = 0; i < m_acticeConnections.Count; i++)
            {
                NetworkMessage temp_message = m_messageManager.getNetworkMessage();
                temp_message.copyOutputDataFrom(message);
                addDataToSend_OneClient(temp_message, m_acticeConnections[i].externalIPEndPoint);
            }
        }
        else
        {
            Debug.LogWarning("TCP_Server: addDataToSend_AllClients: encoding message failed: " + message.getOutputMessageBitView());
        }

        m_messageManager.recycleNetworkMessage(message);
    }

    public List<string> getClientsDiagnostics()
    {
        List<string> returnValue = new List<string>();

        for (int i = 0; i < m_acticeConnections.Count; i++)
        {
            returnValue.Add(m_acticeConnections[i].getDiagnostic());
        }

        return returnValue;
    }

    private void connectionManagement_ThreadProcedure()
    {
        Socket temp_socket;
        TCP_Client temp_client;

        while (Interlocked.Read(ref LOCKED_Stop_Connection_Thread) == 0)
        {
#if DEBUG_TRY_CATCH
            try
            {
#endif
                for (int i = 0; i < m_acticeConnections.Count; i++)
                {
                    if (m_acticeConnections[i].lostConnection) // due to client-side shutting down 
                    {
                        if (!m_lostConnectionsClients.Contains(m_acticeConnections[i]))
                        {
                            lock (m_lostConnectionsClients)
                            {
                                m_lostConnectionsClients.Add(m_acticeConnections[i]);
                            }
                            Debug.LogWarning("TCP_Server: Lost TCP-Connection to Client: " + m_acticeConnections[i].externalIPEndPoint.ToString());
                        }
                    }
                }

                for (int i = 0; i < m_delayedShutdownClients.Count; i++)
                {
                    if (DateTime.Now > m_delayedShutdownEndTimes[i])
                    {
                        m_delayedShutdownClients[i].dispose();
                        m_delayedShutdownClients.RemoveAt(i);
                        m_delayedShutdownEndTimes.RemoveAt(i);
                        i--;
                    }
                }

                if (m_TCPListener.Pending())
                {
                    lock (m_acticeConnections)
                    {
                        temp_socket = m_TCPListener.AcceptSocket();
                        temp_client = new TCP_Client(temp_socket, m_messageManager);

                        m_acticeConnections.Add(temp_client);
                        lock (m_newTCPClients)
                        {
                            m_newTCPClients.Add(temp_client.externalIPEndPoint);
                        }
                        m_EndPoint_TCPClient.Add(temp_client.externalIPEndPoint, temp_client);
                        Debug.Log("TCP_Server: new TCP-Connection established with: " + temp_socket.RemoteEndPoint.ToString());
                    }
                }
                else
                {
                    Thread.Sleep(CONNECTION_LISTENER_SLEEP);
                }
#if DEBUG_TRY_CATCH
            }
            catch (ThreadAbortException abortEx)
            {
                if (Interlocked.Read(ref LOCKED_Stop_Connection_Thread) == 0)
                {
                    Debug.LogError("TCP_Server: connection-management failed: " + abortEx);
                    break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("TCP_Server: connection-management failed: " + ex);
                break;
            }
#endif
        }
    }

    public void dispose()
    {
        try
        {
            Interlocked.Exchange(ref LOCKED_Stop_Connection_Thread, 1);

            if (m_connectionManagementThread != null)
            {
                m_connectionManagementThread.Abort();
                m_connectionManagementThread = null;
            }

            if (m_TCPListener != null)
            {
                m_TCPListener.Stop();
                m_TCPListener = null;
            }

            if (m_acticeConnections != null)
            {
                lock (m_acticeConnections)
                {
                    foreach (TCP_Client client in m_acticeConnections)
                    {
                        client.dispose();
                    }
                    m_acticeConnections.Clear();
                }
                m_acticeConnections = null;
            }

            if (m_lostConnectionsClients != null)
            {
                lock (m_lostConnectionsClients)
                {
                    for (int i = 0; i < m_lostConnectionsClients.Count; i++)
                    {
                        m_lostConnectionsClients[i].dispose();
                    }
                    m_lostConnectionsClients.Clear();
                }
                m_lostConnectionsClients = null;
            }

            /*
            if (m_EndPoint_TCPClient != null)
            {
                m_EndPoint_TCPClient.Clear();
                m_EndPoint_TCPClient = null;
            }
            */
        }
        catch (Exception ex)
        {
            Debug.LogError("TCP_Server: dispose failed: " + ex);
        }
    }

}
