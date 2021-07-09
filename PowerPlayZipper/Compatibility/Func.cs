namespace PowerPlayZipper.Compatibility
{
#if NET20
    public delegate TR Func<T0, TR>(T0 arg0);
    public delegate TR Func<T0, T1, TR>(T0 arg0, T1 arg1);
#endif
}
