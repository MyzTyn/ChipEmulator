using System;

namespace Chip8
{
    /*
     Memory Mapped|
    -----------------------------------------------
     0x200 to 0xFFF is Chip-8 Program / Data Space
     0x000 to 0x1FF is reserved for interpreter
     */

    // Custom Memory
    public class Memory
    {
        // Load Binary Data with offset
        public void LoadData(byte[] data, uint offset = 0) => Array.Copy(data, 0, m_Memory, offset, data.Length);
        public byte[] GetData() { return m_Memory; }
        public int GetLength()
        {
            return m_Memory.Length;
        }

        // Wipe the memory
        public void Reset() { m_Memory = new byte[4096]; }

        // return byte from memory
        public byte ReadByte(int address)
        {
            if (address >= 0 && address < 4096)
                return m_Memory[address];

            return 0;
        }

        // return unsigned short from memory
        public ushort ReadShort(int address)
        {
            byte lowResult = ReadByte(address);
            byte highResult = ReadByte(address + 1);

            return (ushort)((lowResult << 8) | highResult);
        }

        // write byte to memory
        public void WriteByte(int address, byte value)
        {
            if (address >= 0 && address < 4096)
                m_Memory[address] = value;
#if DEBUG
            else
                Debug.LogWarning("The Memory Address is too large or too small: {0}", address);
#endif
        }

        private byte[] m_Memory = new byte[0x1000];
    }
}
