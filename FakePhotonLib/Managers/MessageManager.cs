using FakePhotonLib.BinaryData;
using FakePhotonLib.Encryptions;

namespace FakePhotonLib.Managers;

public static class MessageManager
{
    public static void Parse(MessageAndCallback messageAndCallback)
    {
        if (messageAndCallback.operationResponse != null)
        {
            OperationResponseManager.Parse(messageAndCallback.Challenge, messageAndCallback.operationResponse);
        }

        if (messageAndCallback.operationRequest != null)
        {
            foreach (var item in messageAndCallback.operationRequest.Parameters)
            {
                Console.WriteLine(item.Key.ToString());
                Console.WriteLine(item.Value.ToString());

                if (item.Value.GetType() == typeof(byte[]))
                {
                    byte[] data = (byte[])item.Value;
                    Console.WriteLine("Request key? " + BitConverter.ToString(data).Replace("-", string.Empty));

                    var validKey = DiffieHellmanCryptoProvider.PhotonBigIntArrayToMsBigIntArray(data);
                    Console.WriteLine("validKey? " + BitConverter.ToString(validKey).Replace("-", string.Empty));

                }
            }
        }
    }
}
