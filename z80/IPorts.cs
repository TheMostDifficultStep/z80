namespace z80
{
    public interface IPorts
    {
        string Name{ get; }
        byte ReadPort(ushort address);
        void WritePort(ushort address, byte value);
        bool NMI { get; }
        bool MI { get; }
        byte Data { get; }
    }
}