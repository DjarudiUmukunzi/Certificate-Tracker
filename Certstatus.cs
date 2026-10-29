namespace CertTrack
{
    // How urgent a certification is, based on days until it expires.
    // The dashboard colors each row from this value.
    public enum CertStatus
    {
        Green,    // more than 60 days left: fine
        Yellow,   // 31 to 60 days left: plan the renewal
        Red,      // 30 days or fewer left: escalate
        Expired   // already past its expiration date
    }
}
