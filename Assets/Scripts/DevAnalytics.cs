using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

public static class DevAnalytics
{
    private static readonly IPEndPoint m_devServerIpEndPoint = new IPEndPoint(IPAddress.Parse("167.86.93.181"), 7670);

    private const string LOGFILE_PRE_DIR = "Logs";
    private const string LOGFILE_NAME = "GameLog.txt";

    private static Socket m_outputUDPSocket = null;
    private static AddressFamily m_outputUDPSocketAddFamily = AddressFamily.InterNetwork;

    private static Socket getOutputUDPSocket()
    {
        if (m_outputUDPSocket == null)
        {
            m_outputUDPSocket = new Socket(m_outputUDPSocketAddFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        return m_outputUDPSocket;
    }

    private static void setSocketAddressFamily(AddressFamily family)
    {
        if (family != m_outputUDPSocketAddFamily)
        {
            if (m_outputUDPSocket != null)
            {
                m_outputUDPSocket.Close();
                m_outputUDPSocket.Dispose();
                m_outputUDPSocket = null;
            }
            
            m_outputUDPSocketAddFamily = family;
        }
    }

    public static void writeToLogFile(string text)
    {
        if (!Directory.Exists(System.Environment.CurrentDirectory + "\\" + LOGFILE_PRE_DIR))
        {
            Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\" + LOGFILE_PRE_DIR);
        }

        if (!File.Exists(System.Environment.CurrentDirectory + "\\" + LOGFILE_PRE_DIR + "\\" + LOGFILE_NAME))
        {
            File.Create(System.Environment.CurrentDirectory + "\\" + LOGFILE_PRE_DIR + "\\" + LOGFILE_NAME).Dispose();
        }

        File.AppendAllText(System.Environment.CurrentDirectory + "\\" + LOGFILE_PRE_DIR + "\\" + LOGFILE_NAME, text + System.Environment.NewLine);
    }

    public static void sendPlayerConnectedFeedback(string loginName)
    {
        sendDataToFeedbackServer(string.Format("0;{0}", loginName));
    }

    public static void sendPlayerDisconnectedFeedback(string loginName)
    {
        sendDataToFeedbackServer(string.Format("1;{0}", loginName));
    }

    public static void sendFreeTextFeedback(string text)
    {
        sendDataToFeedbackServer(string.Format("2;{0}", text));
    }

    public static string removeIllegalCharacters(string text)
    {
        return text.Replace("\"", "?").Replace(":", "?").Replace(",", "?").Replace("{", "?").Replace("}", "?");
    }

    public static void sendDataToFeedbackServer(string message)
    {
        byte[] messageBytes = Encoding.Unicode.GetBytes(removeIllegalCharacters(message));

        getOutputUDPSocket().SendTo(messageBytes, m_devServerIpEndPoint);
    }
}
