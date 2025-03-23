using FakePhotonLib.BinaryData;
using Serilog;

namespace FakePhotonLib.Managers;

public static class MessageManager
{
    public static Dictionary<int, short> ChallengeToPeerId = [];

    public static MessageAndCallback Parse(MessageAndCallback messageAndCallback)
    {
        MessageAndCallback ReturnMessage = (MessageAndCallback)messageAndCallback.Clone();
        // This for checking client what sent.
        if (messageAndCallback.operationResponse != null)
        {
            OperationResponseManager.Parse(messageAndCallback.Challenge, messageAndCallback.operationResponse);
        }

        if (messageAndCallback.operationRequest != null)
        {
            ReturnMessage.Reset();
            ReturnMessage.MessageType = RtsMessageType.OperationResponse;
            ReturnMessage.operationResponse = OperationRequestManager.Parse(messageAndCallback.Challenge, messageAndCallback.operationRequest);
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
