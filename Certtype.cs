namespace CertTrack
{
    // The kinds of certification / license the system tracks.
    // An enum is a fixed list of named choices. It's safer than typing "CRMA" as a
    // string everywhere, because a typo becomes a compile error instead of a silent bug.
    public enum CertType
    {
        CRMA,
        CprFirstAidAed,
        SafetyCare,
        DriverLicense
    }
}
