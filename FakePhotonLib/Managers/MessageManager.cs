using FakePhotonLib.BinaryData;
using FakePhotonLib.Datas;
using Serilog;

namespace FakePhotonLib.Managers;

public static class MessageManager
{
    public static MessageAndCallback Parse(ClientPeer peer, MessageAndCallback messageAndCallback)
    {
        MessageAndCallback ReturnMessage = (MessageAndCallback)messageAndCallback.Clone();
        // This for checking client what sent.
        if (messageAndCallback.operationResponse != null)
        {
            OperationResponseManager.Parse(peer, messageAndCallback.operationResponse);
        }

        if (messageAndCallback.operationRequest != null)
        {
            ReturnMessage.Reset();
            ReturnMessage.MessageType = messageAndCallback.MessageType == RtsMessageType.InternalOperationRequest ? RtsMessageType.InternalOperationResponse : RtsMessageType.OperationResponse;
            ReturnMessage.operationResponse = OperationRequestManager.Parse(peer, messageAndCallback.operationRequest);
        }
        if (messageAndCallback.IsInit != null)
        {
            ReturnMessage.Reset();
            ReturnMessage.MessageType = RtsMessageType.InitResponse;

        }
        Log.Information(ReturnMessage.ToString());
        return ReturnMessage;
    }
}
