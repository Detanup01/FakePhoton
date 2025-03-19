using FakePhotonLib.Managers;
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

public class MessageAndCallback : IBinaryData
{
    public MessageAndCallback(int challenge)
    {
        Challenge = challenge;
    }
    public RtsMessageType MessageType;
    public int Challenge;
    public bool IsNotValid;
    public OperationResponse? operationResponse;
    public OperationRequest? operationRequest;
    public bool? IsInit;


    public Type Type => typeof(MessageAndCallback);

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
        bool IsEncrypted = (b2 & 128) > 0;
        if (IsEncrypted)
        {
            if (!EncryptionManager.EncryptionByChallenge.TryGetValue(Challenge, out var cryptoProvider))
            {
                Log.Error("This should not throw!");
                return;
            }
            var input = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            Console.WriteLine(BitConverter.ToString(input).Replace("-", string.Empty));
            var data = cryptoProvider.Decrypt(input);
            Console.WriteLine(BitConverter.ToString(data).Replace("-", string.Empty));
            MemoryStream ms = new MemoryStream(data);
            reader = new BinaryReader(ms);
        }
        MessageType = (RtsMessageType)b3;
        switch (b3)
        {
            case 1:
                IsInit = true;
                break;
            case 6:
                operationRequest = new();
                operationRequest.Read(reader);
                break;
            case 7:
                operationResponse = new();
                operationResponse.Read(reader);
                break;
            default:
                break;
        }
        Console.WriteLine(b3);
    }

    public void Reset()
    {

    }

    public void Write(BinaryWriter writer)
    {

    }

    public override string ToString()
    {
        string? oprespone = operationResponse == null ? string.Empty : operationResponse.ToString();
        string? op_request = operationRequest == null ? string.Empty : operationRequest.ToString();
        return $"IsNotValid: {IsNotValid} {MessageType} {IsInit} {oprespone} {op_request}";
    }

    public object ParseToObject()
    {
        return this;
    }

    public object ReadToObject(BinaryReader reader)
    {
        this.Reset();
        this.Read(reader);
        return this;
    }
}
