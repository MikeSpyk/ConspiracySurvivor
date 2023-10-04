using System;
using System.Text;
using System.Net;
using UnityEngine;

public class NetworkMessage
{
    public enum MessageType { Undefined, Normal, TCP_End }

    private const int MAX_PARAMETERS = 15; // for float and int values.      2^4 = 16.       16 - 1 = 15 (account zero)
    private const int STRING_MAX_PARAMETERS = 100; // for string values
    private const int BYTE_BUFFER_SIZE = 1000; // data-array-size (output-data). 2 byte ID, 1 byte counts, 15 * 4 = 60 int, 15 * 4 = 60 float => 123 for header&int,float-values, rest for strings
    private const char STRING_SEPARATOR = (char)17; // "free" char

    public static int CreatedMessageCounter = 0;

    /// <summary>
    /// don't use this. use NetworkMessageManager.getNetworkMessage instead
    /// </summary>
    public NetworkMessage()
    {
        m_outputData = new byte[BYTE_BUFFER_SIZE];
        m_integerValues = new int[MAX_PARAMETERS];
        m_floatValues = new float[MAX_PARAMETERS];
        m_stringValues = new string[STRING_MAX_PARAMETERS];
        m_temp_encryptionByteParsed = new uint[BYTE_BUFFER_SIZE / 4];

        m_hashCode = NetworkMessage.CreatedMessageCounter;
        NetworkMessage.CreatedMessageCounter++;
    }

    ~NetworkMessage()
    {
        if (UnityEngine.Application.isPlaying)
        {
            Debug.LogWarning("NetworkMessage Destructor Called: Message: " + ToString());
        }
    }

    // general memebers
    private int m_messageContextID = -1;
    private int[] m_integerValues;
    private float[] m_floatValues;
    private string[] m_stringValues;
    private byte[] m_outputData;
    private int m_outputDataLength = -1;
    private bool m_outputDataReady = false;
    private byte[] m_inputData;
    private int m_floatIndex = -1;
    private int m_IntegerIndex = -1;
    private int m_StringIndex = -1;
    private int m_temp_lastDecryptIndex = -1;
    private byte[] m_temp_lastDecryptUint = new byte[4];
    private uint[] m_temp_encryptionByteParsed;
    private int m_temp_encryptionByteCounter = -1;
    private MessageType m_messageType = MessageType.Undefined;
    private IPEndPoint m_IPEndPoint = new IPEndPoint(IPAddress.None, 0);
    private int m_hashCode;

    // input decoding members
    private bool m_decodingSuccessful = false;
    private byte[] m_temp_inputMessageContextID = new byte[4];
    private byte[] m_temp_inputMessageIntegerLengthBytes = new byte[4];
    private int m_temp_inputMessageIntegerLength;
    private byte[] m_temp_inputMessageFloatLengthBytes = new byte[4];
    private int m_temp_inputMessageFloatLength;
    private int m_temp_inputMessageVariableSize;
    private int m_temp_inputMessagePosCounter;
    private string m_temp_inputMessageAllStrings;
    private string[] m_temp_inputMessageAllStringSplit;
    private int m_lastDecodeLength = 0; // DEBUG

    // output encoding members
    private byte[] m_temp_messageContext;
    private int m_temp_currentBytePos = 0;
    private byte[] m_temp_castedByte;
    private int m_temp_output_integerCount;
    private int m_temp_output_floatCount;
    private int m_temp_output_stringCount;
    private bool m_outputEncrypted = false;
    private uint[] m_encryptionKey = null;

    public override int GetHashCode()
    {
        return m_hashCode;
    }

    public override string ToString()
    {
        string returnValue = "";

        if (encodingSuccessful)
        {
            returnValue += "Encoded Message. ";
        }
        else if (decodingSuccessful)
        {
            returnValue += "Decoded Message. ";
        }
        else
        {
            returnValue += "Uninitalised Message. ";
        }

        returnValue += "HashCode:" + GetHashCode() + ", Message-ID: " + messageContextID + ", Counts: Int: " + integerValuesCount + ", Float: " + floatValuesCount + ", String: " + stringValuesCount + ". ";
        if (integerValuesCount > 0)
        {
            returnValue += "int Values: ";
            for (int i = 0; i < integerValuesCount; i++)
            {
                returnValue += getIntValue(i) + ", ";
            }
        }
        if (floatValuesCount > 0)
        {
            returnValue += "float Values: ";
            for (int i = 0; i < floatValuesCount; i++)
            {
                returnValue += getFloatValue(i) + ", ";
            }
        }
        if (stringValuesCount > 0)
        {
            returnValue += "string Values: ";
            for (int i = 0; i < stringValuesCount; i++)
            {
                byte[] byteString = Encoding.Unicode.GetBytes(getStringValue(i));
                returnValue += Encoding.UTF8.GetString(Encoding.Convert(Encoding.Unicode, Encoding.UTF8, byteString)) + ", ";
            }
        }

        return returnValue;
    }

    private void decodeInputDataCountsWithin(int length)
    {
        m_lastDecodeLength = length;

        if (length < 3)
        {
            Debug.LogWarning("NetworkMessage: decodeInputDataCountsWithin: input data too short. need at least 3 bytes: input: " + length);
            return;
        }

        if (length >= BYTE_BUFFER_SIZE)
        {
            Debug.LogWarning("NetworkMessage: decodeInputDataCountsWithin: input data too long. max allowed size: " + BYTE_BUFFER_SIZE + " bytes, input: " + length + " bytes");
            return;
        }

        //Console.WriteLine("m_inputData: " + showBinary(m_inputData));

        // context

        m_temp_inputMessageContextID[0] = m_inputData[0];
        m_temp_inputMessageContextID[1] = m_inputData[1];

        m_messageContextID = BitConverter.ToInt32(m_temp_inputMessageContextID, 0);

        //Console.WriteLine("NetworkMessage: decodeInputData: m_messageContextID: " + m_messageContextID);

        // integer count

        m_temp_inputMessageIntegerLengthBytes[0] = m_inputData[2];

        m_temp_inputMessageIntegerLength = BitConverter.ToInt32(m_temp_inputMessageIntegerLengthBytes, 0) >> 4;

        //Console.WriteLine("NetworkMessage: decodeInputData: m_temp_inputMessageIntegerLength: " + m_temp_inputMessageIntegerLength);

        // float count

        m_temp_inputMessageFloatLengthBytes[0] = m_inputData[2];

        m_temp_inputMessageFloatLength = (int)((uint)(BitConverter.ToInt32(m_temp_inputMessageFloatLengthBytes, 0) << 28) >> 28);

        //Console.WriteLine("NetworkMessage: decodeInputData: m_temp_inputMessageFloatLength: " + m_temp_inputMessageFloatLength);

        // size check

        m_temp_inputMessageVariableSize = 3 + (m_temp_inputMessageFloatLength + m_temp_inputMessageIntegerLength) * 4;

        if (length < m_temp_inputMessageVariableSize)
        {
            Debug.LogWarning("NetworkMessage: decodeInputDataCountsWithin: input data too short. needed at least " + m_temp_inputMessageVariableSize + ". input: " + length);
            return;
        }

        // decode int

        m_temp_inputMessagePosCounter = 3;

        for (int i = 0; i < m_temp_inputMessageIntegerLength; i++)
        {
            m_IntegerIndex++;
            m_integerValues[m_IntegerIndex] = BitConverter.ToInt32(m_inputData, m_temp_inputMessagePosCounter);
            //Console.WriteLine("NetworkMessage: decodeInputData: added integer: " + m_integerValues[i]);
            m_temp_inputMessagePosCounter += 4;
        }

        // decode float

        for (int i = 0; i < m_temp_inputMessageFloatLength; i++)
        {
            m_floatIndex++;
            m_floatValues[m_floatIndex] = BitConverter.ToSingle(m_inputData, m_temp_inputMessagePosCounter);
            //Console.WriteLine("NetworkMessage: decodeInputData: added float: " + m_floatValues[i]);
            m_temp_inputMessagePosCounter += 4;
        }

        // decode string

        m_temp_inputMessageAllStrings = Encoding.Unicode.GetString(m_inputData, m_temp_inputMessagePosCounter, length - m_temp_inputMessagePosCounter);

        m_temp_inputMessageAllStringSplit = m_temp_inputMessageAllStrings.Split(STRING_SEPARATOR);

        if (m_temp_inputMessageAllStringSplit.Length >= STRING_MAX_PARAMETERS)
        {
            Debug.LogWarning("NetworkMessage: decodeInputDataCountsWithin: too many string parameters. Count: " + m_temp_inputMessageAllStringSplit.Length + ", max allowed: " + (STRING_MAX_PARAMETERS - 1));
            return;
        }

        for (int i = 0; i < m_temp_inputMessageAllStringSplit.Length; i++)
        {
            if (m_temp_inputMessageAllStringSplit[i] == string.Empty)
            {
                continue;
            }

            m_StringIndex++;
            m_stringValues[m_StringIndex] = m_temp_inputMessageAllStringSplit[i];

            //Console.WriteLine("NetworkMessage: decodeInputData: added string: " + m_stringValues[i]);
        }

        m_decodingSuccessful = true;
        m_messageType = MessageType.Normal;
    }

    private void decodeInputDataCountsGiven(int floatValues, int integerValues)
    {
        if (m_inputData.Length < 2 + (floatValues + integerValues) * 4)
        {
            Debug.LogWarning("NetworkMessage: decodeInputDataCountsGiven: input data too short. need at least " + (2 + (floatValues + integerValues) * 4) + " bytes with the given value-counts: input: " + m_inputData.Length);
            return;
        }

        if (m_inputData.Length >= BYTE_BUFFER_SIZE)
        {
            Debug.LogWarning("NetworkMessage: decodeInputDataCountsGiven: input data too long. max allowed size: " + BYTE_BUFFER_SIZE + " bytes, input: " + m_inputData.Length + " bytes");
            return;
        }

        //Console.WriteLine("m_inputData: " + showBinary(m_inputData));

        // context

        m_temp_inputMessageContextID[0] = m_inputData[0];
        m_temp_inputMessageContextID[1] = m_inputData[1];

        m_messageContextID = BitConverter.ToInt32(m_temp_inputMessageContextID, 0);

        //Console.WriteLine("NetworkMessage: decodeInputData: m_messageContextID: " + m_messageContextID);


        // decode int

        m_temp_inputMessagePosCounter = 2;

        for (int i = 0; i < integerValues; i++)
        {
            m_IntegerIndex++;
            m_integerValues[m_IntegerIndex] = BitConverter.ToInt32(m_inputData, m_temp_inputMessagePosCounter);
            //Console.WriteLine("NetworkMessage: decodeInputData: added integer: " + m_integerValues[i]);
            m_temp_inputMessagePosCounter += 4;
        }

        // decode float

        for (int i = 0; i < floatValues; i++)
        {
            m_floatIndex++;
            m_floatValues[m_floatIndex] = BitConverter.ToSingle(m_inputData, m_temp_inputMessagePosCounter);
            //Console.WriteLine("NetworkMessage: decodeInputData: added float: " + m_floatValues[i]);
            m_temp_inputMessagePosCounter += 4;
        }

        // decode string

        m_temp_inputMessageAllStrings = Encoding.Unicode.GetString(m_inputData, m_temp_inputMessagePosCounter, m_inputData.Length - m_temp_inputMessagePosCounter);

        m_temp_inputMessageAllStringSplit = m_temp_inputMessageAllStrings.Split(STRING_SEPARATOR);

        if (m_temp_inputMessageAllStringSplit.Length >= STRING_MAX_PARAMETERS)
        {
            Debug.LogWarning("NetworkMessage: decodeInputDataCountsGiven: too many string parameters. Count: " + m_temp_inputMessageAllStringSplit.Length + ", max allowed: " + (STRING_MAX_PARAMETERS - 1));
            return;
        }

        for (int i = 0; i < m_temp_inputMessageAllStringSplit.Length; i++)
        {
            if (m_temp_inputMessageAllStringSplit[i] == string.Empty)
            {
                continue;
            }

            m_StringIndex++;
            m_stringValues[m_StringIndex] = m_temp_inputMessageAllStringSplit[i];

            //Console.WriteLine("NetworkMessage: decodeInputData: added string: " + m_stringValues[i]);
        }

        m_decodingSuccessful = true;
        m_messageType = MessageType.Normal;
    }

    /// <summary>
    /// check if the load was successful and if the loaded input-variables-count match the provided variables-count
    /// </summary>
    /// <param name="intCount"></param>
    /// <param name="floatCount"></param>
    /// <param name="stringCount"></param>
    /// <returns></returns>
    public bool checkInputCorrectness(int intCount, int floatCount, int stringCount)
    {
        if (m_decodingSuccessful)
        {
            if (intCount != integerValuesCount)
            {
                return false;
            }

            if (floatCount != floatValuesCount)
            {
                return false;
            }

            if (stringCount != stringValuesCount)
            {
                return false;
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    private void createBinaryOutputData(bool addCount)
    {
        if (m_messageContextID == -1)
        {
            Debug.LogWarning("NetworkMessage: createBinaryOutputData: m_messageContextID not set");
            return;
        }

        m_temp_messageContext = BitConverter.GetBytes(m_messageContextID);

        //Console.WriteLine("m_temp_messageContext: " + showBinary(m_temp_messageContext, 2));

        // message context
        m_outputData[0] = m_temp_messageContext[0];
        m_outputData[1] = m_temp_messageContext[1];

        if (addCount)
        {
            // int & float count
            m_temp_output_integerCount = integerValuesCount;
            m_temp_output_floatCount = floatValuesCount;

            m_outputData[2] = BitConverter.GetBytes(m_temp_output_integerCount << 4 | m_temp_output_floatCount)[0];

            // int values
            m_temp_currentBytePos = 3;
        }
        else
        {
            m_temp_currentBytePos = 2;
        }

        for (int i = 0; i < integerValuesCount; i++)
        {
            m_temp_castedByte = BitConverter.GetBytes(m_integerValues[i]);

            m_outputData[m_temp_currentBytePos] = m_temp_castedByte[0];
            m_outputData[m_temp_currentBytePos + 1] = m_temp_castedByte[1];
            m_outputData[m_temp_currentBytePos + 2] = m_temp_castedByte[2];
            m_outputData[m_temp_currentBytePos + 3] = m_temp_castedByte[3];

            m_temp_currentBytePos += 4;
        }

        // float values
        for (int i = 0; i < floatValuesCount; i++)
        {
            m_temp_castedByte = BitConverter.GetBytes(m_floatValues[i]);

            m_outputData[m_temp_currentBytePos] = m_temp_castedByte[0];
            m_outputData[m_temp_currentBytePos + 1] = m_temp_castedByte[1];
            m_outputData[m_temp_currentBytePos + 2] = m_temp_castedByte[2];
            m_outputData[m_temp_currentBytePos + 3] = m_temp_castedByte[3];

            m_temp_currentBytePos += 4;
        }

        // string values

        if (m_StringIndex > -1) // if at least 1 string value
        {
            m_temp_output_stringCount = stringValuesCount;

            m_stringValues[m_StringIndex] = m_stringValues[m_StringIndex].Remove(m_stringValues[m_StringIndex].Length - 1); // delete seperator afer last string

            for (int i = 0; i < m_temp_output_stringCount; i++)
            {
                m_temp_castedByte = Encoding.Unicode.GetBytes(m_stringValues[i]);
                //Console.WriteLine("m_temp_castedByte.length: " + m_temp_castedByte.Length);

                if (m_temp_currentBytePos + m_temp_castedByte.Length >= BYTE_BUFFER_SIZE)
                {
                    Debug.LogWarning("NetworkMessage: createBinaryOutputData: string section is too long. exceeding " + BYTE_BUFFER_SIZE + " bytes");
                    return;
                }

                for (int j = 0; j < m_temp_castedByte.Length; j++)
                {
                    m_outputData[m_temp_currentBytePos] = m_temp_castedByte[j];
                    m_temp_currentBytePos++;
                }
            }
        }

        m_outputDataLength = m_temp_currentBytePos; // 2 byte: context, 1 byte: size (float & int), 4 byte each float or int value, string

        //Console.WriteLine("m_outputDataLength: " + m_outputDataLength);
        //Console.WriteLine("m_outputData: " + showBinary(m_outputData, m_outputDataLength));

        m_outputDataReady = true;
    }

    public void copyOutputDataFrom(NetworkMessage sourceMessage)
    {
        if (sourceMessage.encodingSuccessful)
        {
            byte[] temp_output;
            int temp_length;
            sourceMessage.getOutput(out temp_output, out temp_length);

            for (int i = 0; i < temp_length; i++)
            {
                m_outputData[i] = temp_output[i];
            }

            m_outputEncrypted = sourceMessage.outputEncyrpted;
            m_outputDataReady = true;
            m_outputDataLength = temp_length;
        }
        else
        {
            Debug.LogError("NetworkMessage: copyOutputDataFrom: sourceMessage is not encoded yet. make sure a message is encoded before trying to copy it");
        }
    }

    public void setEncryptionKey(uint[] key)
    {
        if (key == null)
        {
            m_encryptionKey = null;
        }
        else
        {
            if (key.Length != BYTE_BUFFER_SIZE / 4)
            {
                Debug.LogWarning("NetworkMessage: setEncryptionKey: key.length is out of bounds: expected: " + (BYTE_BUFFER_SIZE / 4) + " input: " + key.Length);
            }
            else
            {
                m_encryptionKey = key;
            }
        }
    }

    /// <summary>
    /// encryptes the byte-data bitwise with the given key. creates the output-data if it wasnt created yet
    /// </summary>
    /// <param name="addCount">if creating the data: add the int and float values count to the message ?</param>
    private void encryptOutput(bool addCount)
    {
        if (m_encryptionKey == null)
        {
            Debug.LogWarning("NetworkMessage: encryption-Key was'nt set yet");
        }
        else
        {
            if (!m_outputDataReady)
            {
                createBinaryOutputData(addCount);
            }

            /*
            Console.WriteLine("before encryption: " + showBinary(cutByteArray(m_outputData, 100)) + "\n");

            byte[] key = new byte[m_encryptionKey.Length * 4];

            for (int i = 0; i < m_encryptionKey.Length; i++)
            {
                byte[] bytes = BitConverter.GetBytes(m_encryptionKey[i]);

                key[i * 4] = bytes[0];
                key[i * 4 + 1] = bytes[1];
                key[i * 4 + 2] = bytes[2];
                key[i * 4 + 3] = bytes[3];
            }

            Console.WriteLine("encryption key: " + showBinary(cutByteArray(key, 100)) + "\n");
             */

            m_temp_encryptionByteCounter = 0;

            for (int i = 0; i < m_outputDataLength; i += 4)
            {
                // cast uint
                m_temp_encryptionByteParsed[m_temp_encryptionByteCounter] = BitConverter.ToUInt32(m_outputData, i);
                // switch bits
                m_temp_encryptionByteParsed[m_temp_encryptionByteCounter] = m_temp_encryptionByteParsed[m_temp_encryptionByteCounter] ^ m_encryptionKey[m_temp_encryptionByteCounter];
                // cast byte
                byte[] byteConverted = BitConverter.GetBytes(m_temp_encryptionByteParsed[m_temp_encryptionByteCounter]);
                m_outputData[i] = byteConverted[0];
                m_outputData[i + 1] = byteConverted[1];
                m_outputData[i + 2] = byteConverted[2];
                m_outputData[i + 3] = byteConverted[3];

                m_temp_encryptionByteCounter++;
            }

            //Console.WriteLine("after encryption: " + showBinary(cutByteArray(m_outputData, 100)) + "\n");

            m_outputEncrypted = true;
        }
    }

    public void setMessageContext(int messageContextID)
    {
        if (m_outputDataReady)
        {
            Debug.LogError("NetworkMessage: setMessageContext: message is already enclosed");
        }
        else if (messageContextID < 0 || messageContextID > 65534)
        {
            Debug.LogError("NetworkMessage: setMessageContext: messageContextID out of range. Range: [0 - 65534], messageContextID: " + messageContextID);
        }
        else
        {
            m_messageContextID = messageContextID;
            //Console.WriteLine("m_messageContextID: " + messageContextID);
        }
    }

    public int getMessageContextID()
    {
        return m_messageContextID;
    }

    /// <summary>
    /// add integer Values to the List of integers to be send.
    /// </summary>
    /// <param name="values"></param>
    public void addIntegerValues(params int[] values)
    {
        if (m_outputDataReady)
        {
            Debug.LogError("NetworkMessage: addIntegerValues: message is already enclosed");
        }
        else if (integerValuesCount + values.Length > MAX_PARAMETERS)
        {
            Debug.LogError("NetworkMessage: addIntegerValues: argument-count out of range: max allowed: " + MAX_PARAMETERS + ", requestet count: " + (integerValuesCount + values.Length));
        }
        else
        {
            for (int i = 0; i < values.Length; i++)
            {
                m_IntegerIndex++;
                m_integerValues[m_IntegerIndex] = values[i];
            }
        }
    }

    /// <summary>
    /// add float Values to the List of floats to be send.
    /// </summary>
    /// <param name="values"></param>
    public void addFloatValues(params float[] values)
    {
        if (m_outputDataReady)
        {
            Debug.LogError("NetworkMessage: addFloatValues: message is already enclosed");
        }
        else if (floatValuesCount + values.Length > MAX_PARAMETERS)
        {
            Debug.LogError("NetworkMessage: addFloatValues: argument-count out of range: max allowed: " + MAX_PARAMETERS + ", requestet count: " + (floatValuesCount + values.Length));
        }
        else
        {
            for (int i = 0; i < values.Length; i++)
            {
                m_floatIndex++;
                m_floatValues[m_floatIndex] = values[i];
            }
        }
    }

    /// <summary>
    /// add one or more strings. the encoding will be unicode
    /// </summary>
    /// <param name="values"></param>
    public void addStringValues(params string[] values)
    {
        if (m_outputDataReady)
        {
            Debug.LogError("NetworkMessage: addStringValues: message is already enclosed");
        }
        else if (stringValuesCount + values.Length > STRING_MAX_PARAMETERS)
        {
            Debug.LogError("NetworkMessage: addStringValues: argument-count out of range: max allowed: " + STRING_MAX_PARAMETERS + ", requestet count: " + (stringValuesCount + values.Length));
        }
        else
        {
            for (int i = 0; i < values.Length; i++)
            {
                m_StringIndex++;
                m_stringValues[m_StringIndex] = values[i] + STRING_SEPARATOR;
            }
        }
    }

    public void getIntegerValues(out int[] data, out int length)
    {
        data = m_integerValues;
        length = integerValuesCount;
    }

    public void getFloatValues(out float[] data, out int length)
    {
        data = m_floatValues;
        length = floatValuesCount;
    }

    public void getStringValues(out string[] data, out int length)
    {
        data = m_stringValues;
        length = stringValuesCount;
    }

    public int getIntValue(int index)
    {
        if (index > m_IntegerIndex)
        {
            throw new IndexOutOfRangeException("NetworkMessage: Int-Value Index out of Range. Range: [0 - " + MAX_PARAMETERS + "], Input: " + index);
        }

        return m_integerValues[index];
    }

    public float getFloatValue(int index)
    {
        if (index > m_floatIndex)
        {
            throw new IndexOutOfRangeException("NetworkMessage: Float-Value Index out of Range. Range: [0 - " + MAX_PARAMETERS + "], Input: " + index);
        }

        return m_floatValues[index];
    }

    public string getStringValue(int index)
    {
        if (index > m_StringIndex)
        {
            throw new IndexOutOfRangeException("NetworkMessage: String-Value Index out of Range. Range: [0 - " + STRING_MAX_PARAMETERS + "], Input: " + index);
        }

        return m_stringValues[index];
    }

    /// <summary>
    /// only valid if values-counts are within
    /// </summary>
    /// <returns></returns>
    public int getByteLength()
    {
        return m_lastDecodeLength;
    }

    /// <summary>
    /// set the raw (byte) input-data and decode it to the ID, float-values, int-values and strings. the float and int values counts must be present within the message.
    /// </summary>
    /// <param name="data"></param>
    public bool setInputData(byte[] data, int length)
    {
        return setInputData(data, length, false);
    }
    /// <summary>
    /// set the raw (byte) input-data and decode it to the ID, float-values, int-values and strings. the float and int values counts must be present within the message. can also decrypt the raw data before decoding it.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="decrypt">decrypt the the data with the given key</param>
    public bool setInputData(byte[] data, int length, bool decrypt)
    {
        if (m_decodingSuccessful)
        {
            Debug.LogError("NetworkMessage: setInputData: This Messages has already got the input-data set. cant reset the input-data.");
            return false;
        }

        m_inputData = data;
        if (decrypt)
        {
            decryptInput(length);
        }
        decodeInputDataCountsWithin(length);

        return m_decodingSuccessful;
    }
    /// <summary>
    /// set the raw (byte) input-data and decode it to the ID, float-values, int-values and strings with the given value counts. Use this when receiving a message without the float and int count transmitted. can also decrypt the raw data before decoding it.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="decrypt">decrypt the the data with the given key</param>
    /// <param name="floatCount">how many values of float type are in this message</param>
    /// <param name="integerCount">how many values of int type are in this message</param>
    public bool setInputData(byte[] data, int length, bool decrypt, int floatCount, int integerCount)
    {
        if (m_decodingSuccessful)
        {
            Debug.LogError("NetworkMessage: setInputData: This Messages has already got the input-data set. cant reset the input-data.");
            return false;
        }

        if (floatCount < 0 || floatCount > MAX_PARAMETERS)
        {
            Debug.LogError("NetworkMessage: setInputData: float Count out of range: allowed [0-" + MAX_PARAMETERS + "], given: " + floatCount);
            return false;
        }

        if (integerCount < 0 || integerCount > MAX_PARAMETERS)
        {
            Debug.LogError("NetworkMessage: setInputData: integer Count out of range: allowed [0-" + MAX_PARAMETERS + "], given: " + integerCount);
            return false;
        }

        m_inputData = data;
        if (decrypt)
        {
            decryptInput(length);
        }

        decodeInputDataCountsGiven(floatCount, integerCount);

        return m_decodingSuccessful;
    }

    private void decryptInput(int length)
    {
        if (m_encryptionKey == null)
        {
            Debug.LogError("NetworkMessage: decryptInput: encryption-Key was'nt set yet");
        }
        else
        {
            if (m_inputData == null)
            {
                Debug.LogWarning("NetworkMessage: decryptInput: input data is NULL");
                return;
            }

            m_temp_encryptionByteCounter = 0;

            if (length % 4 == 0)
            {
                m_temp_lastDecryptIndex = length;
            }
            else
            {
                m_temp_lastDecryptIndex = (length / 4) * 4;
            }

            byte[] newInputData = new byte[m_temp_lastDecryptIndex];

            for (int i = 0; i < length; i += 4)
            {
                if (i == m_temp_lastDecryptIndex)
                {
                    // cast uint
                    for (int j = 0; j < 4; j++)
                    {
                        if (i + j < length)
                        {
                            m_temp_lastDecryptUint[j] = m_inputData[i + j];
                        }
                        else
                        {
                            m_temp_lastDecryptUint[j] = 0;
                        }

                    }

                    m_temp_encryptionByteParsed[m_temp_encryptionByteCounter] = BitConverter.ToUInt32(m_temp_lastDecryptUint, 0);
                }
                else
                {
                    // cast uint
                    m_temp_encryptionByteParsed[m_temp_encryptionByteCounter] = BitConverter.ToUInt32(m_inputData, i);
                }

                // switch bits
                m_temp_encryptionByteParsed[m_temp_encryptionByteCounter] = m_temp_encryptionByteParsed[m_temp_encryptionByteCounter] ^ m_encryptionKey[m_temp_encryptionByteCounter];
                // cast byte
                byte[] byteConverted = BitConverter.GetBytes(m_temp_encryptionByteParsed[m_temp_encryptionByteCounter]);

                if (i == m_temp_lastDecryptIndex)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        if (i + j < length)
                        {
                            newInputData[i + j] = byteConverted[j];
                        }
                    }
                }
                else
                {
                    newInputData[i] = byteConverted[0];
                    newInputData[i + 1] = byteConverted[1];
                    newInputData[i + 2] = byteConverted[2];
                    newInputData[i + 3] = byteConverted[3];
                }

                m_temp_encryptionByteCounter++;
            }

            m_inputData = newInputData;
        }
    }

    /// <summary>
    /// returns the byte-encoded message-buffer. If the message hasn't been encoded yet, it will get decoded here
    /// </summary>
    /// <param name="data"></param>
    /// <param name="length"></param>
    public bool getOutput(out byte[] data, out int length)
    {
        getOutput(out data, out length, true, false);

        return m_outputDataReady;
    }
    /// <summary>
    /// returns the byte-encoded message-buffer. If the message hasn't been encoded yet, it will get decoded here
    /// </summary>
    /// <param name="data"></param>
    /// <param name="length"></param>
    /// <param name="addCount"></param>
    public bool getOutput(out byte[] data, out int length, bool addCount)
    {
        getOutput(out data, out length, addCount, false);

        return m_outputDataReady;
    }
    /// <summary>
    /// returns the byte-encoded message-buffer. If the message hasn't been encoded yet, it will get decoded here
    /// </summary>
    /// <param name="data"></param>
    /// <param name="length"></param>
    /// <param name="addCount">add the int and float values count to the message ?</param>
    /// <param name="encrypt">encrypte the byte-data bitwise with the given key</param>
    public bool getOutput(out byte[] data, out int length, bool addCount, bool encrypt)
    {
        if (!m_outputDataReady)
        {
            createBinaryOutputData(addCount);
        }

        if (encrypt)
        {
            encryptOutput(addCount);
        }

        data = m_outputData;
        length = m_outputDataLength;

        return m_outputDataReady;
    }

    /// <summary>
    /// gets the byte-encoded-buffer and transfers it into an array of the length of the message
    /// </summary>
    /// <param name="addCount">add the int and float values count to the message ?</param>
    /// <param name="encrypt">encrypte the byte-data bitwise with the given key</param>
    /// <returns></returns>
    public byte[] getOutputCut(bool addCount, bool encrypt)
    {
        if (!m_outputDataReady)
        {
            createBinaryOutputData(addCount);
        }

        if (encrypt)
        {
            encryptOutput(addCount);
        }

        return cutByteArray(m_outputData, m_outputDataLength);
    }
    /// <summary>
    /// gets the byte-encoded-buffer and transfers it into an array of the length of the message
    /// </summary>
    /// <param name="data"></param>
    /// <param name="length"></param>
    /// <param name="addCount">add the int and float values count to the message ?</param>
    /// <param name="encrypt">encrypte the byte-data bitwise with the given key</param>
    public bool getOutputCut(out byte[] data, out int length, bool addCount, bool encrypt)
    {
        if (!m_outputDataReady)
        {
            createBinaryOutputData(addCount);
        }

        if (encrypt)
        {
            encryptOutput(addCount);
        }

        data = cutByteArray(m_outputData, m_outputDataLength);
        length = m_outputDataLength;

        return m_outputDataReady;
    }

    /// <summary>
    /// clears the message so it can be reused. this should get called by NetworkMessageManager only
    /// </summary>
    public void clear()
    {
        m_decodingSuccessful = false;
        m_messageContextID = -1;
        m_IntegerIndex = -1;
        m_floatIndex = -1;
        m_StringIndex = -1;
        m_outputDataLength = -1;
        m_outputDataReady = false;
        m_inputData = null;
        m_outputEncrypted = false;
        m_messageType = MessageType.Undefined;
        m_IPEndPoint = new IPEndPoint(IPAddress.None, 0);
    }

    public int integerValuesCount
    {
        get
        {
            return m_IntegerIndex + 1;
        }
    }

    public int floatValuesCount
    {
        get
        {
            return m_floatIndex + 1;
        }
    }

    public int stringValuesCount
    {
        get
        {
            return m_StringIndex + 1;
        }
    }

    public int messageContextID
    {
        get
        {
            return m_messageContextID;
        }
        set
        {
            setMessageContext(value);
        }
    }

    public bool decodingSuccessful
    {
        get
        {
            return m_decodingSuccessful;
        }
    }

    public bool encodingSuccessful
    {
        get
        {
            return m_outputDataReady;
        }
    }

    public MessageType messageType
    {
        get
        {
            return m_messageType;
        }
        set
        {
            m_messageType = value;
        }
    }

    public IPEndPoint iPEndPoint
    {
        get
        {
            return m_IPEndPoint;
        }
        set
        {
            m_IPEndPoint = value;
        }
    }

    public bool outputEncyrpted
    {
        get
        {
            return m_outputEncrypted;
        }
    }

    public string getOutputMessageBitView()
    {
        if (m_outputDataLength > 0)
        {
            return showBinary(cutByteArray(m_outputData, m_outputDataLength));
        }
        else
        {
            return "Empty";
        }
    }

    public string getInputMessageBitView()
    {
        if (m_inputData != null)
        {
            return showBinary(m_inputData);
        }
        else
        {
            return "NULL";
        }
    }

    public static int maxDataBufferSize
    {
        get
        {
            return BYTE_BUFFER_SIZE;
        }
    }

    public static string showBinary(byte[] data)
    {
        return showBinary(data, data.Length);
    }
    public static string showBinary(byte[] data, int start)
    {
        string output = data.Length + " bytes: ";

        for (int i = start - 1; i >= 0; i--)
        {
            output += Convert.ToString(data[i], 2).PadLeft(8, '0') + ":";
        }
        output += "<--";

        return output;
    }

    public static byte[] cutByteArray(byte[] data, int end)
    {
        if (end < 0)
        {
            return data;
        }

        byte[] returnValue = new byte[end];

        for (int i = 0; i < end; i++)
        {
            returnValue[i] = data[i];
        }

        return returnValue;
    }

}

