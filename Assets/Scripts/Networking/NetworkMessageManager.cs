#define TRACK_MESSAGE_LOSS
//#undef TRACK_MESSAGE_LOSS
#define CHECK_MULTIPLE_RECYLE
//#undef CHECK_MULTIPLE_RECYLE

using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkMessageManager
{
    const int CACHE_SIZE = 4000;

    /// <summary>
    /// class for creating, recyling and caching network messages
    /// </summary>
    /// <param name="fillCache">fill cache array with new NetworkMessage-entries ?</param>
    public NetworkMessageManager(bool fillCache)
    {
        m_maxIndex = CACHE_SIZE - 1;
        m_networkMessagesCache = new NetworkMessage[CACHE_SIZE];

        if (fillCache)
        {
            for (int i = 0; i < CACHE_SIZE; i++)
            {
                m_networkMessagesCache[i] = new NetworkMessage();
            }
            m_cacheIndex = m_maxIndex;
        }
    }

    private NetworkMessage[] m_networkMessagesCache;
    private int m_cacheIndex = -1;
    private int m_maxIndex;
    private object LOCK_cacheInUse = new object();

#if TRACK_MESSAGE_LOSS
    private HashSet<NetworkMessage> m_releasedMessages = new HashSet<NetworkMessage>();
    private Dictionary<NetworkMessage, string> m_releasedMessages_stackTrace = new Dictionary<NetworkMessage, string>();
#endif

    /// <summary>
    /// get a new or recyled network message. this method is threadsafe 
    /// </summary>
    /// <returns></returns>
    public NetworkMessage getNetworkMessage()
    {
        NetworkMessage temp_returnValue = null;
        lock (LOCK_cacheInUse)
        {
            if (m_cacheIndex > -1)
            {
                //lock (LOCK_cacheInUse)
                //{
                temp_returnValue = m_networkMessagesCache[m_cacheIndex];
                m_networkMessagesCache[m_cacheIndex] = null;
                --m_cacheIndex;
                //}
                temp_returnValue.clear();
            }
            else
            {
                Debug.LogWarning("NetworkMessageManager: getNetworkMessage: cache empty (" + CACHE_SIZE + " messages in use) => creating new message");
                temp_returnValue = new NetworkMessage();
            }

#if TRACK_MESSAGE_LOSS
            //lock (LOCK_cacheInUse)
            //{
            if (m_releasedMessages.Contains(temp_returnValue))
            {
                Debug.LogError("NetworkMessageManager: networkmessage released multiple times: " + temp_returnValue.ToString() + "\nLast Stack Trace: \n" + m_releasedMessages_stackTrace[temp_returnValue]);
                return new NetworkMessage();
            }
            else
            {
                m_releasedMessages.Add(temp_returnValue);

                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
                string allTraces = DateTime.Now.Second + ":" + DateTime.Now.Millisecond + ": ";
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    System.Diagnostics.StackFrame frame = stackTrace.GetFrame(i);
                    allTraces += frame.GetMethod() + "\n";
                    allTraces += frame.GetFileName() + "\n";
                    allTraces += frame.GetFileLineNumber() + "\n";
                }
                m_releasedMessages_stackTrace.Add(temp_returnValue, allTraces);
            }
        }
#endif

        //Debug.Log("returned: " + temp_returnValue.GetHashCode() + " (" + m_cacheIndex + "/" + CACHE_SIZE + ")" + ": t=" + DateTime.Now.Second + "," + DateTime.Now.Millisecond);
        return temp_returnValue;
    }

    /// <summary>
    /// add a network message for recyling. make sure the network message will no longer be used ! this method is threadsafe
    /// </summary>
    /// <param name="message"></param>
    public void recycleNetworkMessage(NetworkMessage message)
    {
        //Debug.Log("recyled: " + message.GetHashCode() + " (" + m_cacheIndex + "/" + CACHE_SIZE + ") ID:"+ message.messageContextID + " : t=" + DateTime.Now.Second + "," + DateTime.Now.Millisecond);

        if (message == null)
        {
            Debug.LogWarning("NetworkMessageManager: recycleNetworkMessage: message is null");
        }
        else
        {
            if (m_cacheIndex == m_maxIndex)
            {
                Debug.LogWarning("NetworkMessageManager: recycleNetworkMessage: Cache limit (" + CACHE_SIZE + ") reached. skipping... (for garbage collector)");
            }
            else
            {
                lock (LOCK_cacheInUse)
                {
#if CHECK_MULTIPLE_RECYLE
                    for (int i = 0; i < count; i++)
                    {
                        if (m_networkMessagesCache[i] == message)
                        {
                            if (m_releasedMessages_stackTrace.ContainsKey(message))
                            {
                                Debug.LogWarning("NetworkMessageManager: multiple recyle: \"" + message.ToString() + "\"\nLast Saved: \n" + m_releasedMessages_stackTrace[message]);
                            }
                            else
                            {
                                Debug.LogWarning("NetworkMessageManager: multiple recyle: \"" + message.ToString() + "\"\nMessage not found as released ");
                            }
                            return;
                        }
                    }
#endif
                    ++m_cacheIndex;
                    m_networkMessagesCache[m_cacheIndex] = message;

#if TRACK_MESSAGE_LOSS
                    if (m_releasedMessages.Contains(message))
                    {
                        m_releasedMessages.Remove(message);
                        m_releasedMessages_stackTrace.Remove(message);
                    }
#endif
                }
            }
        }
    }

    /// <summary>
    /// sets all cache-entries to NULL
    /// </summary>
    public void clear()
    {
        lock (LOCK_cacheInUse)
        {
            for (int i = 0; i < CACHE_SIZE; i++)
            {
                m_networkMessagesCache[i] = null;
            }
            m_cacheIndex = -1;
        }
    }


    public void logReleasedMessages()
    {
#if TRACK_MESSAGE_LOSS
        NetworkMessage[] result = null;

        lock (LOCK_cacheInUse)
        {
            result = new NetworkMessage[m_releasedMessages.Count];
            m_releasedMessages.CopyTo(result);
        }

        string logMessage = "NetworkMessageManager: released messages: " + m_releasedMessages.Count + "\n";
        int encodedCount = 0;
        int decodedCount = 0;

        for (int i = 0; i < result.Length; i++)
        {
            if (result[i].decodingSuccessful)
            {
                decodedCount++;
            }

            if (result[i].encodingSuccessful)
            {
                encodedCount++;
            }
        }

        logMessage += "encoded: " + encodedCount + "\n";
        logMessage += "decoded: " + decodedCount + "\n";

        Debug.Log(logMessage);
#else
        Debug.Log("NetworkMessageManager: logReleasedMessages: not available");
#endif
    }


    public int count
    {
        get
        {
            return m_cacheIndex + 1;
        }
    }

}

