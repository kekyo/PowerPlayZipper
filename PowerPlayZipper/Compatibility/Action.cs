namespace PowerPlayZipper.Compatibility
{
#if NET20
    public delegate void Action();
    public delegate void Action<T0, T1>(T0 arg0, T1 arg1);
    public delegate void Action<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2);
#endif
}
