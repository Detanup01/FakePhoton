namespace FakePhotonLib.PhotonRelated.StructWrapping;

public enum Pooling
{
    Disconnected = 0,
    Connected = 1,
    ReleaseOnUnwrap = 2,
    Readonly = 4,
    CheckedOut = 8,
}
