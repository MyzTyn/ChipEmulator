using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Chip8
{
     // Byte  - 8-bits
     // Short - 16-bits

    public class Registers
    {
        public void Reset()
        {
            V = new byte[0xF + 1];
            I = 0;
            // start at 0x200
            PC = 0x200;
            Stack = new Stack<ushort>(0xF);
            DelayTimer = 0;
            SoundTimer = 0;
        }

        // 16 8-bit registers: V0 to VF
        public byte[] V;

        // 12-bit I
        public ushort I;

        // Program Counter
        public ushort PC;

        // 16 16-bit Stack And Stack Pointer
        public Stack<ushort> Stack;

        // 8-bit Timers

        public byte DelayTimer;
        public byte SoundTimer;
    }

    public class CHIP_8
    {
        public CHIP_8()
        {
            registers = new Registers();
            memory = new Memory();
            Reset();
        }

        // Reset the Variables to default value
        private void Reset()
        {
            registers.Reset();
            memory.Reset();
            gfx = new bool[DISPLAY_SIZE];
            key = new byte[16];
            // Add fonts data to memory
            memory.LoadData(FONTS);
        }

        // Load Rom from filepath
        public void LoadRom(string filepath)
        {
            // Check and make sure that file is exist
            if (!File.Exists(filepath))
                throw new FileNotFoundException("Can't find a file: {0}", filepath);

            // Throw a Error if file don't end with .ch8
            if (!filepath.EndsWith(".ch8"))
                throw new Exception("Error: File must end with .ch8");

            // use IO.StreamReader to find and load file
            StreamReader reader = new StreamReader(filepath);
            // use MemoryStream for copy byte array to Chip8's memory
            using (var memstream = new MemoryStream())
            {
                reader.BaseStream.CopyTo(memstream);
                memory.LoadData(memstream.ToArray(), 0x200);
            }
            // Close StreamReader once it done
            reader.Close();
        }

        // Pass Key to V[X]
        public void KeyPressed(byte key)
        {
            waitingForKeyPress = false;

            ushort opcode = memory.ReadShort(registers.PC);
            // key pass to Chip8's registers: V;
            registers.V[(opcode & 0x0F00) >> 8] = key;
            // Increment PC by 2
            registers.PC += 2;
        }
        
        // Execute Instruction Sets
        public void ExecuteOpCode()
        {
            // Check if watch is running or not
            if (!watch.IsRunning) 
                watch.Start();

            // Dec Delay Timer
            if (registers.DelayTimer > 0 || registers.SoundTimer > 0)
            {
                if (watch.ElapsedTicks > ticksPer60hz)
                {
                    // Beep Sound
                    if (registers.SoundTimer == 1)
                    {
                        Console.Beep();
                    }
                    if (registers.DelayTimer > 0)
                    {
                        registers.DelayTimer--;
                    }
                    if (registers.SoundTimer > 0)
                    {
                        registers.SoundTimer--;
                    }
                    watch.Restart();
                }
            }

            // OpCode Data to make thing easier for switch

            ushort opcode = memory.ReadShort(registers.PC);

            ushort  NNN     = (ushort)(opcode & 0x0FFF);
            byte    NN      = (byte)(opcode & 0x00FF);
            byte    N       = (byte)(opcode & 0x000F);

            byte    X       = (byte)((opcode & 0x0F00) >> 8);
            byte    Y       = (byte)((opcode & 0x00F0) >> 4);
#if DEBUG
            Debug.Log("Opcode: {0}", opcode.ToString("X4"));
#endif
            registers.PC += 2;
            switch (opcode >> 12)
            {
                case 0:
                    {
                        switch (opcode & 0x00FF)
                        {
                            case 0xE0: // CLS
                                {
                                    for (int i = 0; i < gfx.Length; i++) gfx[i] = false;
                                    break;
                                }
                            case 0xEE: // RET
                                {
                                    registers.PC = registers.Stack.Pop();
                                    break;
                                }
                            default:
#if DEBUG
                                Debug.LogError("Unknown Opcode");
#endif
                                break;
                        }
                        break;
                    }
                case 1: // JP addr
                    {
                        registers.PC = NNN;
                        break;
                    }
                case 2: // CALL addr
                    {
                        registers.Stack.Push(registers.PC);
                        registers.PC = NNN;
                        break;
                    }
                case 3: // SE Vx, byte
                    {
                        if (registers.V[X] == NN)
                            registers.PC += 2;
                        break;
                    }
                case 4: // SNE Vx, byte
                    {
                        if (registers.V[X] != NN)
                            registers.PC += 2;
                        break;
                    }
                case 5: // SE Vx, Vy
                    {
                        if (registers.V[X] == registers.V[Y])
                            registers.PC += 2;
                        break;
                    }
                case 6: // LD Vx, byte
                    {
                        registers.V[X] = NN;
                        break;
                    }
                case 7: // ADD Vx, byte
                    {
                        registers.V[X] += NN;
                        break;
                    }
                case 8:
                    {
                        switch (opcode & 0x000F)
                        {
                            case 0: // LD Vx, Vy
                                {
                                    registers.V[X] = registers.V[Y];
                                    break;
                                }
                            case 1: // OR Vx, Vy
                                {
                                    registers.V[X] |= registers.V[Y];
                                    break;
                                }
                            case 2: // AND Vx, Vy
                                {
                                    registers.V[X] &= registers.V[Y];
                                    break;
                                }
                            case 3: // XOR Vx, Vy
                                {
                                    registers.V[X] ^= registers.V[Y];
                                    break;
                                }
                            case 4: // ADD Vx, Vy
                                {
                                    ushort result = (ushort)(registers.V[X] + registers.V[Y]);

                                    registers.V[0xF] = (byte)(result > 255 ? 1 : 0);
                                    registers.V[X] = (byte)(result & 0x00FF);

                                    break;
                                }
                            case 5: // SUB Vx, Vy
                                {
                                    registers.V[0xF] = (byte)(registers.V[X] > registers.V[Y] ? 1 : 0);
                                    registers.V[X] -= registers.V[Y];
                                    break;
                                }
                            case 6: // SHR Vx {, Vy}
                                {
                                    registers.V[0xF] = (byte)(registers.V[X] & 0x1);
                                    registers.V[X] >>= 1;
                                    break;
                                }
                            case 7: // SUBN Vx, Vy
                                {
                                    registers.V[0xF] = (byte)(registers.V[Y] > registers.V[X] ? 1 : 0);
                                    registers.V[X] = (byte)((registers.V[Y] - registers.V[X]) & 0x00FF);
                                    break;
                                }
                            case 0xE: // SHL Vx {, Vy}
                                {
                                    registers.V[0xF] = (byte)(registers.V[X] >> 7);
                                    registers.V[X] <<= 1;
                                    break;
                                }
                        }
                        break;
                    }
                case 9: // SNE Vx, Vy
                    {
                        if (registers.V[X] != registers.V[Y])
                            registers.PC += 2;
                        break;
                    }
                case 0xA: // LD I, addr
                    {
                        registers.I = NNN;
                        break;
                    }
                case 0xB: // JP V0, addr
                    {
                        registers.PC = (ushort)(registers.V[0] + NNN);
                        break;
                    }
                case 0xC: // RND Vx, byte
                    {
                        registers.V[X] = (byte)(new Random().Next(255) & NN);
                        break;
                    }
                case 0xD: // DRW Vx, Vy, nibble
                    {
                        ushort x = registers.V[X];
                        ushort y = registers.V[Y];
                        ushort height = N;

                        registers.V[15] = 0;
                        
                        for (int i = 0; i < height; i++)
                        {
                            byte mem = memory.ReadByte(registers.I + i);
                        
                            for (int bit = 0; bit < 8; bit++)
                            {
                                // return one-bit from int mem and bit(Position)
                                byte pixel = (byte)((mem >> (7 - bit)) & 0x01);
                                int index = (x + bit) + (y + i) * 64;
                        
                                // 64 * 32 = 2048 - 1 = 2047
                                if (index > 2047) continue;
                                
                                // Check Flag: if pixel is flip
                                if (pixel == 1 && gfx[index] != false) 
                                    registers.V[0xF] = 1;

                                // Write to gfx pixels
                                gfx[index] = gfx[index] ^ pixel == 1;
                            }
                        }
                        break;
                    }
                case 0xE: 
                    {
                        switch (opcode & 0x00FF)
                        {
                            case 0x9E: // SKP Vx
                                {
                                    if (key[registers.V[X]] != 0)
                                        registers.PC += 2;
                                    break;
                                }
                            case 0xA1: // SKNP Vx
                                {
                                    if (key[registers.V[X]] == 0)
                                        registers.PC += 2;
                                    break;
                                }
                        }
                        break;
                    }
                case 0xF: 
                    {
                        switch (opcode & 0xFF)
                        {
                            case 0x07: // LD Vx, DT
                                {
                                    registers.V[X] = registers.DelayTimer;
                                    break;
                                }
                            case 0x0A: // LD Vx, K
                                {
                                    // Wait for Input
                                    waitingForKeyPress = true;
                                    registers.PC -= 2;

                                    break;
                                }
                            case 0x15: // LD DT, Vx
                                {
                                    registers.DelayTimer = registers.V[X];
                                    break;
                                }
                            case 0x18: // LD ST, Vx
                                {
                                    registers.SoundTimer = registers.V[X];
                                    break;
                                }
                            case 0x1E: // ADD I, Vx
                                {
                                    registers.I += registers.V[X];
                                    break;
                                }
                            case 0x29: // LD F, Vx
                                {
                                    registers.I = (ushort)(registers.V[X] * 5);
                                    break;
                                }
                            case 0x33: // LD B, Vx
                                {
                                    memory.WriteByte(registers.I, (byte)(registers.V[X] / 100));
                                    memory.WriteByte(registers.I + 1, (byte)((registers.V[X] % 100) / 10));
                                    memory.WriteByte(registers.I + 2, (byte)(registers.V[X] % 10));
                                    break;
                                }
                            case 0x55: // LD [I], Vx
                                {
                                    for (int i = 0; i <= X; i++)
                                        memory.WriteByte(registers.I + i, registers.V[i]);
                                    break;
                                }
                            case 0x65: // LD Vx, [I]
                                {
                                    for (int i = 0; i <= X; i++)
                                        registers.V[i] = memory.ReadByte(registers.I + i);
                                    break;
                                }
                        }
                        break;
                    }
                default:
#if DEBUG
                    Debug.LogError("Unknown Opcode"); 
#endif
                    break;
            }
        }


#if DEBUG
#region Debug Stuff
        public Dictionary<ushort, string> Disassmble(ushort start, ushort end)
        {
            Dictionary<ushort, string> mapOpCode = new Dictionary<ushort, string>();
            uint addr = start;

            if (end == 0)
                end = (ushort)memory.GetData().Length;

            while (addr <= end)
            {
                string sInst = string.Format("${0:X4}: ", addr);
                ushort opcode = memory.ReadShort((int)addr);

                ushort NNN = (ushort)(opcode & 0x0FFF);
                byte NN = (byte)(opcode & 0x00FF);
                byte N = (byte)(opcode & 0x000F);

                byte X = (byte)((opcode & 0x0F00) >> 8);
                byte Y = (byte)((opcode & 0x00F0) >> 4);

                addr += 2;
                switch (opcode >> 12)
                {
                    case 0:
                        {
                            switch (opcode & 0x00FF)
                            {
                                case 0xE0: // CLS
                                    {
                                        sInst += "CLS";
                                        break;
                                    }
                                case 0xEE: // RET
                                    {
                                        sInst += "RET";
                                        break;
                                    }
                            }
                            break;
                        }
                    case 1: // JP addr
                        {
                            sInst += string.Format("JP {0:X3}", NNN);
                            break;
                        }
                    case 2: // CALL addr
                        {
                            sInst += string.Format("CALL {0:X3}", NNN);
                            break;
                        }
                    case 3: // SE Vx, byte
                        {
                            sInst += string.Format("SE V{0:X}, {1:X2}", X, NN);
                            break;
                        }
                    case 4: // SNE Vx, byte
                        {
                            sInst += string.Format("SNE V{0:X}, {1:X2}", X, NN);
                            break;
                        }
                    case 5: // SE Vx, Vy
                        {
                            sInst += string.Format("SE V{0:X}, V{1:X}", X, Y);
                            break;
                        }
                    case 6: // LD Vx, byte
                        {
                            sInst += string.Format("LD V{0:X}, {1:X2}", X, NN);
                            break;
                        }
                    case 7: // ADD Vx, byte
                        {
                            sInst += string.Format("ADD V{0:X}, {1:X2}", X, NN);
                            break;
                        }
                    case 8:
                        {
                            switch (opcode & 0x000F)
                            {
                                case 0: // LD Vx, Vy
                                    {
                                        sInst += string.Format("LD V{0:X}, V{1:X}", X, Y);
                                        break;
                                    }
                                case 1: // OR Vx, Vy
                                    {
                                        sInst += string.Format("OR V{0:X}, V{1:X}", X, Y);
                                        break;
                                    }
                                case 2: // AND Vx, Vy
                                    {
                                        sInst += string.Format("AND V{0:X}, V{1:X}", X, Y);
                                        break;
                                    }
                                case 3: // XOR Vx, Vy
                                    {
                                        sInst += string.Format("XOR V{0:X}, V{1:X}", X, Y);
                                        break;
                                    }
                                case 4: // ADD Vx, Vy
                                    {
                                        sInst += string.Format("ADD V{0:X}, V{1:X}", X, Y);
                                        break;
                                    }
                                case 5: // SUB Vx, Vy
                                    {
                                        sInst += string.Format("SUB V{0:X}, V{1:X}", X, Y);
                                        break;
                                    }
                                case 6: // SHR Vx {, Vy}
                                    {
                                        sInst += string.Format("SHR V{0:X} (, V{1:X})", X, Y);
                                        break;
                                    }
                                case 7: // SUBN Vx, Vy
                                    {
                                        sInst += string.Format("SUBN V{0:X}, V{1:X}", X, Y);
                                        break;
                                    }
                                case 0xE: // SHL Vx {, Vy}
                                    {
                                        sInst += string.Format("SHL V{0:X} (, V{1:X})", X, Y);
                                        break;
                                    }
                            }
                            break;
                        }
                    case 9: // SNE Vx, Vy
                        {
                            sInst += string.Format("SNE V{0:X}, V{1:X}", X, Y);
                            break;
                        }
                    case 0xA: // LD I, addr
                        {
                            sInst += string.Format("LD I, {0:X3}", NNN);
                            break;
                        }
                    case 0xB: // JP V0, addr
                        {
                            sInst += string.Format("JP V{0:X}, {1:X3}", X, NNN);
                            break;
                        }
                    case 0xC: // RND Vx, byte
                        {
                            sInst += string.Format("RND V{0:X}, {1:X2}", X, NN);
                            break;
                        }
                    case 0xD: // DRW Vx, Vy, nibble
                        {
                            sInst += string.Format("DRW V{0:X}, V{1:X}, {2:X}", X, Y, N);
                            break;
                        }
                    case 0xE:
                        {
                            switch (opcode & 0x00FF)
                            {
                                case 0x9E: // SKP Vx
                                    {
                                        sInst += string.Format("SKP V{0:X}", X);
                                        break;
                                    }
                                case 0xA1: // SKNP Vx
                                    {
                                        sInst += string.Format("SKNP V{0:X}", X);
                                        break;
                                    }
                            }
                            break;
                        }
                    case 0xF:
                        {
                            switch (opcode & 0xFF)
                            {
                                case 0x07: // LD Vx, DT
                                    {
                                        sInst += string.Format("LD V{0:X}, DT", X);
                                        break;
                                    }
                                case 0x0A: // LD Vx, K
                                    {
                                        sInst += string.Format("LD V{0:X}, K", X);
                                        break;
                                    }
                                case 0x15: // LD DT, Vx
                                    {
                                        sInst += string.Format("LD DT, V{0:X}", X);
                                        break;
                                    }
                                case 0x18: // LD ST, Vx
                                    {
                                        sInst += string.Format("LD ST, V{0:X}", X);
                                        break;
                                    }
                                case 0x1E: // ADD I, Vx
                                    {
                                        sInst += string.Format("SUB I, V{0:X}", X);
                                        break;
                                    }
                                case 0x29: // LD F, Vx
                                    {
                                        registers.I = (ushort)(registers.V[X] * 5);
                                        sInst += string.Format("LD F, V{0:X}", X);
                                        break;
                                    }
                                case 0x33: // LD B, Vx
                                    {
                                        sInst += string.Format("LD B, V{0:X}", X);
                                        break;
                                    }
                                case 0x55: // LD [I], Vx
                                    {
                                        sInst += string.Format("LD [I], V{0:X}", X);
                                        break;
                                    }
                                case 0x65: // LD Vx, [I]
                                    {
                                        sInst += string.Format("LD V{0:X}, [I]", X);
                                        break;
                                    }
                            }
                            break;
                        }
                }
                mapOpCode.Add((ushort)(addr - 2), sInst);
            }
            return mapOpCode;
        }

        public List<string> Disassmble(ushort start, ushort end, bool removeWhitespace = false)
        {
            List<string> instCode = Disassmble(start, end).Values.ToList();

            // Remove blank lines
            if (removeWhitespace)
            {
                for (int i = 0; i < instCode.Count; i++)
                {
                    if (instCode[i].EndsWith(" "))
                    {
                        instCode.RemoveAt(i);
                        i--;
                    }

                }
            }

            return instCode;
        }

        // Get Registers info without V[0xF]
        public override string ToString()
        {
            return string.Format("PC: {0}\nStack: {1}\nI: {2}\nDT: {3}\nST: {4}",
                registers.PC.ToString("X3"), StackToString(), registers.I.ToString("X3"), registers.DelayTimer, registers.SoundTimer);
        }

        // Get Registers V info only
        public string VToString()
        {
            string value = "";

            for (int i = 0; i < registers.V.Length; i++)
                value += string.Format("V[{0:X1}]: {1:X2}\n", i, registers.V[i]);

            return value;
        }

        private string StackToString()
        {
            // make a clone of stack
            Stack<ushort> clone = new Stack<ushort>(new Stack<ushort>(registers.Stack));
            string value = clone.Count == 0 ? "NONE" : "";

            while (clone.Count != 0)
            {
                // Extract top of the stack
                int x = clone.Peek();

                // Pop the top element
                clone.Pop();

                value += string.Format("{0:X4} ", x);
            }

            return value;
        }
#endregion
#endif

#region Variables
        public bool[] gfx { get; private set; }
        public byte[] key { get; set; }

        public Registers registers { get; private set; }
        public Memory memory { get; private set; }

        // Await for Key press
        public bool waitingForKeyPress { get; private set; }

        // Use Stopwatch for measure Ticks per 60 Hz
        private Stopwatch watch = new Stopwatch();
        // Total of ticks
        private int ticksPer60hz = (int)(Stopwatch.Frequency * 0.016);

        // Sizes
        const int DISPLAY_SIZE = 64 * 32;

        // Fonts (Read Only)
        private readonly byte[] FONTS =
        {
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80, // F
        };
#endregion
    }
}
