using FakePhotonLib.BinaryData;
using Serilog;

namespace FakePhotonLib.Managers;

public static class MessageManager
{
    public static MessageAndCallback Parse(MessageAndCallback messageAndCallback)
    {
        MessageAndCallback messageAndCallback1 = messageAndCallback;
        // This for checking client what sent.
        if (messageAndCallback.operationResponse != null)
        {
            OperationResponseManager.Parse(messageAndCallback.Challenge, messageAndCallback.operationResponse);
        }

        if (messageAndCallback.operationRequest != null)
        {
            messageAndCallback1.operationResponse = OperationRequestManager.Parse(messageAndCallback.Challenge, messageAndCallback.operationRequest);
        }
        if (messageAndCallback.IsInit != null)
        {
            messageAndCallback1.MessageType = RtsMessageType.InitResponse;
        }
        Log.Information(messageAndCallback1.ToString());
        return messageAndCallback1;
    }
}
