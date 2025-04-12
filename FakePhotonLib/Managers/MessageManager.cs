using FakePhotonLib.BinaryData;
using FakePhotonLib.Datas;
using Serilog;

namespace FakePhotonLib.Managers;

public static class MessageManager
{
    public static MessageAndCallback Parse(ClientPeer peer, MessageAndCallback input, out (MessageAndCallback, CommandType)? optional)
    {
        optional = null;
        MessageAndCallback ReturnMessage = (MessageAndCallback)input.Clone();
        // This for checking client what sent.
        if (input.operationResponse != null)
        {
            OperationResponseManager.Parse(peer, input.operationResponse);
        }

        if (input.operationRequest != null)
        {
            ReturnMessage.Reset();
            ReturnMessage.MessageType = input.MessageType == RtsMessageType.InternalOperationRequest ? RtsMessageType.InternalOperationResponse : RtsMessageType.OperationResponse;
            ReturnMessage.operationResponse = OperationRequestManager.Parse(peer, input.operationRequest, out optional);
        }
        if (input.IsInit != null)
        {
            ReturnMessage.Reset();
            ReturnMessage.MessageType = RtsMessageType.InitResponse;

        }
        Log.Information(ReturnMessage.ToString());
        return ReturnMessage;
    }
}
