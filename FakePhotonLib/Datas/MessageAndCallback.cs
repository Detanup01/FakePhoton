using FakePhotonLib.Datas;
using FakePhotonLib.Managers;
using FakePhotonLib.Protocols;
using Serilog;
using System.Security.Cryptography;

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

public class MessageAndCallback : ICloneable
{
    public ClientPeer? peer;

    public RtsMessageType MessageType;
    public int Challenge;
    public bool IsNotValid;
    public OperationResponse? operationResponse;
    public OperationRequest? operationRequest;
    public EventData? eventData;
    public DisconnectMessage? disconnectMessage;
    public bool? IsInit;
    public bool IsEncrypted;


    public void Read(BinaryReader reader)
    {
        byte b = reader.ReadByte();
        IsNotValid = b != 243 && b != 253;
        if (IsNotValid)
        {
            Console.WriteLine("No regular operation UDP message");
            return;
        }

        byte b2 =  reader.ReadByte();
        byte b3 = (byte)(b2 & 127);
        MessageType = (RtsMessageType)b3;
        IsEncrypted = (b2 & 128) > 0;
        bool flag7 = b3 != 1;
        if (IsEncrypted)
        {
            if (peer == null)
            {
                Log.Error("Peer is null!! cannot decrypt encrypted packet!");
                return;
            }
            if (!EncryptionManager.EncryptionByChallenge.TryGetValue(peer.Challenge, out var cryptoProvider))
            {
                Log.Error("This should not throw (Read IsEncrypted)!");
                return;
            }
            var data = cryptoProvider.Decrypt(reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position)));
            reader = new(new MemoryStream(data));
        }
        switch (MessageType)
        {
            case RtsMessageType.Init:
                IsInit = true;
                break;
            case RtsMessageType.InitResponse:
                break;
            case RtsMessageType.Operation:
            case RtsMessageType.InternalOperationRequest:
                operationRequest = Protocol.ProtocolDefault.DeserializeOperationRequest(reader);
                break;
            case RtsMessageType.OperationResponse:
            case RtsMessageType.InternalOperationResponse:
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

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)243);
        var msg_type = (byte)MessageType;
        if (IsEncrypted)
        {
            msg_type |= 128;
            Log.Information("Sending IsEncrypted!");
        }
            
        writer.Write(msg_type);

        using MemoryStream ms = new();
        using BinaryWriter dataWriter = new(ms);

        if (operationResponse != null)
            Protocol.ProtocolDefault.SerializeOperationResponse(dataWriter, operationResponse, false);
        if (operationRequest != null)
            Protocol.ProtocolDefault.SerializeOperationRequest(dataWriter, operationRequest.OperationCode, operationRequest.Parameters, false);
        if (eventData != null)
            Protocol.ProtocolDefault.SerializeEventData(dataWriter, eventData, false);
        if (disconnectMessage != null)
            Protocol.ProtocolDefault.SerializeMessage(dataWriter, disconnectMessage);
        byte[] data = ms.ToArray();
        try
        {
            if (operationResponse != null)
            {
                var rsp = Protocol.ProtocolDefault.DeserializeOperationResponse(new(new MemoryStream(data)));
            }
            if (operationRequest != null)
            {
                var rsp = Protocol.ProtocolDefault.DeserializeOperationRequest(new(new MemoryStream(data)));
            }
            if (eventData != null)
            {
                var rsp = Protocol.ProtocolDefault.DeserializeEventData(new(new MemoryStream(data)));
            }
        }
        catch (Exception ex)
        {
            Log.Error("{ERROR}", ex);
        }
        if (IsEncrypted)
        {
            if (peer == null)
            {
                Log.Error("Peer is null!! cannot encrypt packet!");
                return;
            }
            if (!EncryptionManager.EncryptionByChallenge.TryGetValue(peer.Challenge, out var cryptoProvider))
            {
                Log.Error("This should not throw! (EncryptionByChallenge)");
                return;
            }
            writer.Write(cryptoProvider.Encrypt(data));
        }
        else
            writer.Write(data);
    }

    public void Reset()
    {
        MessageType = RtsMessageType.Init;
        operationResponse = null;
        operationRequest = null;
        eventData = null;
        disconnectMessage = null;
        IsInit = null;
        IsEncrypted = false;
    }

    public override string ToString()
    {
        string? oprespone = operationResponse == null ? string.Empty : operationResponse.ToString();
        string? op_request = operationRequest == null ? string.Empty : operationRequest.ToString();
        return $"IsNotValid: {IsNotValid} {MessageType} {IsInit.HasValue} {IsEncrypted} {oprespone} {op_request}";
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}
