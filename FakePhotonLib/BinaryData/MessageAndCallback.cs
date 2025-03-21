using FakePhotonLib.Managers;
using FakePhotonLib.PhotonRelated;
using FakePhotonLib.Protocols;
using Serilog;

namespace FakePhotonLib.BinaryData;
public enum RtsMessageType : byte
{
    Init,
    InitResponse,
    Operation,
    OperationResponse,
    Event,
    DisconnectMessage,
    InternalOperationRequest,
    InternalOperationResponse,
    Message,
    RawMessage,
    Unknown = 255
}

public class MessageAndCallback
{
    public MessageAndCallback()
    {
        Challenge = 0;
    }
    public MessageAndCallback(int challenge)
    {
        Challenge = challenge;
    }
    public RtsMessageType MessageType;
    public int Challenge;
    public bool IsNotValid;
    public OperationResponse? operationResponse;
    public OperationRequest? operationRequest;
    public EventData? eventData;
    public DisconnectMessage? disconnectMessage;
    public bool? IsInit;
    public bool IsEncrypted;


    public void Read(StreamBuffer reader)
    {
        byte b = reader.ReadByte();
        IsNotValid = b != 243 && b != 253;
        if (IsNotValid)
        {
            Console.WriteLine("No regular operation UDP message");
            return;
        }

        byte b2 =  reader.ReadByte();
        Console.WriteLine($"b2: {b2}");
        byte b3 = (byte)(b2 & 127);
        Console.WriteLine($"b3: {b3}");
        MessageType = (RtsMessageType)b3;
        IsEncrypted = (b2 & 128) > 0;
        bool flag7 = b3 != 1;
        Console.WriteLine("IsEncrypted: " + IsEncrypted);
        Console.WriteLine("Flag7: "+ flag7);
        if (IsEncrypted)
        {
            if (!EncryptionManager.EncryptionByChallenge.TryGetValue(Challenge, out var cryptoProvider))
            {
                Log.Error("This should not throw!");
                return;
            }
            var data = cryptoProvider.Decrypt(reader.GetBuffer(), 2, reader.Length - 2);
            reader = new(data);
        }
        switch (MessageType)
        {
            case RtsMessageType.Init:
                IsInit = true;
                break;
            case RtsMessageType.InitResponse:
                break;
            case RtsMessageType.InternalOperationRequest:
                operationRequest = Protocol.ProtocolDefault.DeserializeOperationRequest(reader);
                break;
            case RtsMessageType.OperationResponse:
                operationResponse = Protocol.ProtocolDefault.DeserializeOperationResponse(reader);
                break;
            case RtsMessageType.Event:
                eventData = Protocol.ProtocolDefault.DeserializeEventData(reader);
                break;
            case RtsMessageType.DisconnectMessage:
                disconnectMessage = Protocol.ProtocolDefault.DeserializeDisconnectMessage(reader);
                break;
            default:
                Console.WriteLine("unkown! " + MessageType);
                break;
        }
    }

    public void Write(StreamBuffer writer)
    {
        writer.WriteByte(253);
        var msg_type = (byte)MessageType;
        if (IsEncrypted)
            msg_type |= 128;
        writer.WriteByte(msg_type);
        byte[] data = [];
        if (operationResponse != null)
            Protocol.ProtocolDefault.Serialize(operationResponse!);
        if (operationRequest != null)
            Protocol.ProtocolDefault.Serialize(operationRequest!);
        if (eventData != null)
            Protocol.ProtocolDefault.Serialize(eventData!);
        if (disconnectMessage != null)
            Protocol.ProtocolDefault.Serialize(disconnectMessage!);
        if (IsEncrypted)
        {
            if (!EncryptionManager.EncryptionByChallenge.TryGetValue(Challenge, out var cryptoProvider))
            {
                Log.Error("This should not throw!");
                return;
            }
            data = cryptoProvider.Encrypt(data);
        }
        writer.Write(data, 0, data.Length);
    }

    public override string ToString()
    {
        string? oprespone = operationResponse == null ? string.Empty : operationResponse.ToString();
        string? op_request = operationRequest == null ? string.Empty : operationRequest.ToString();
        return $"IsNotValid: {IsNotValid} {MessageType} {IsInit} {oprespone} {op_request}";
    }
}
