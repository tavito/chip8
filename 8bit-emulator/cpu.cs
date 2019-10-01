using System;
using System.Collections.Generic;
using System.Text;

namespace _8bit_emulator
{
    public class CPU
    {
        #region Propiedades
        /*
        The systems memory map:
        0x000-0x1FF - Chip 8 interpreter (contains font set in emu)
        0x050-0x0A0 - Used for the built in 4x5 pixel font set (0-F)
        0x200-0xFFF - Program ROM and work RAM
        */

        Random random = new Random();

        // 35 opcodes, all two bytes long
        UInt16 opcode;

        // The Chip 8 has 4k memory in total
        public byte[] memory = new byte[4096];

        // CPU registers: The Chip 8 has 15 8-bit general purpose registers
        // named V0,V1 up to VE.
        // The 16th register is used  for the ‘carry flag’. Eight bits is one
        // byte
        public byte[] V = new byte[16];

        // There is an Index register I and a program counter (pc) which can
        // have a value from
        // 0x000 to 0xFFF
        UInt16 I;
        public UInt16 pc;
        public bool WaitingForKeyPress = false;


        /*
        The graphics of the Chip 8 are black and white and the screen has a
        total of 2048 pixels (64 x 32). This can easily be implemented
        using an array that hold the pixel state (1 or 0):
        */
        public Display screen;
        public Keyboard keyboard;

        /*
        Interupts and hardware registers. The Chip 8 has none, but there are
        two timer registers that count at 60 Hz. When set above zero they will
        count down to zero.
        */
        public byte delay_timer;
        public byte sound_timer;

        // Stack and Stack Pointer
        UInt16[] stack = new UInt16[16];
        UInt16 sp;

        Dictionary<ushort, Func<UInt16, byte, byte, byte, byte, byte, bool>> instrucciones;
        Dictionary<ushort, Func<UInt16, byte, byte, byte, byte, byte, bool>> instrucciones0;
        Dictionary<ushort, Func<UInt16, byte, byte, byte, byte, byte, bool>> instrucciones8;
        Dictionary<ushort, Func<UInt16, byte, byte, byte, byte, byte, bool>> instruccionesE;
        Dictionary<ushort, Func<UInt16, byte, byte, byte, byte, byte, bool>> instruccionesF;

        delegate void Instrucciones(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p);

        #endregion Propiedades

        #region Constructor

        public CPU(Display screen, Keyboard keyboard)
        {
            this.screen = screen;
            this.keyboard = keyboard;

            this.instrucciones0 = new Dictionary<ushort, Func<UInt16, byte, byte, byte, byte, byte, bool>>
            {
                { 0xE0, CLS },
                { 0xEE, RET },
            };

            this.instrucciones = new Dictionary<ushort, Func<UInt16, byte, byte, byte, byte, byte, bool>>
            {
                { 0x0, Instrucciones0},
                { 0x1, JP },
                { 0x2, CALL },
                { 0x3, SEVxByte },
                { 0x4, SNEVxByte },
                { 0x5, SEVxVy },
                { 0x6, LDVxByte },
                { 0x7, ADD },
                { 0x8, Instrucciones8 },
                { 0X9, SNEVxVy },
                { 0xA, LDI },
                { 0xB, JPB0Addr },
                { 0xC, RND },
                { 0xD, DRW },
                { 0xE, InstruccionesE },
                { 0xF, InstruccionesF }

            };

            this.instrucciones8 = new Dictionary<ushort, Func<UInt16, byte, byte, byte, byte, byte, bool>>
            {
                { 0x0, LDVxVy },
                { 0x1, OR },
                { 0x2, AND },
                { 0x3, XOR },
                { 0x4, ADDXY },
                { 0x5, SUB },
                { 0x6, SHR },
                { 0x7, SUBN },
                { 0x00E, SHL }
            };

            this.instruccionesE = new Dictionary<ushort, Func<UInt16, byte, byte, byte, byte, byte, bool>>
            {
                { 0x9E, SKP },
                { 0xA1, SKNP }
            };

            this.instruccionesF = new Dictionary<ushort, Func<UInt16, byte, byte, byte, byte, byte, bool>>
            {
                { 0x7, LDVxDT},
                { 0xA, LDVxK},
                { 0x15, LDDTVx },
                { 0x18, LDSTVx },
                { 0x1E, ADDIVx },
                { 0x29, LDFVx },
                { 0x33, LDBVx },
                { 0x55, LDIVx },
                { 0x65, LDVxI },
            };
        }
        #endregion

        #region Implementacón de Instrucciones
        public bool Instrucciones0(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            instrucciones0[kk](nnn, kk, n, x, y, p);
            return true;
        }

        public bool CLS(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p) {
            //Console.WriteLine("CLS");
            Array.Clear(screen.screen, 0, screen.screen.Length);
            return true;
        }

        public bool RET(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("RET");
            if (sp > 0)
                sp--;
            pc = stack[sp];
            return true;
        }

        public bool CALL(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("CALL");
            // Guardo el Pointer Counter en la pila e incremento el
            // puntero de la pila
            if (sp < 16)
            {
                stack[sp] = pc;
                sp++;
            }
            pc = nnn;
            return true;
        }

        public bool JP(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("JP");
            pc = (UInt16)(nnn);
            return true;
        }

        public bool SEVxByte(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("SE");
            if (V[x] == kk)
                pc = (UInt16)((pc + 2) & 0xFFF);
            return true;
        }

        public bool SNEVxByte(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("SNEVxByte");
            if (V[x] != kk)
                pc = (UInt16)((pc + 2) & 0xFFF);
            return true;
        }

        public bool SEVxVy(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("SEVxVy");
            if (V[x] == V[y])
                pc = (UInt16)((pc + 2) & 0xFFF);
            return true;
        }

        public bool LDVxByte(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("LDVxByte");
            V[(opcode & 0x0F00) >> 8] = kk;
            return true;
        }

        public bool ADD(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("ADD");
            V[x] = (byte)(V[x] + kk);
            return true;
        }

        public bool Instrucciones8(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            instrucciones8[n](nnn, kk, n, x, y, p);
            return true;
        }

        public bool LDVxVy(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("LD");
            V[x] = V[y];
            return true;
        }

        public bool OR(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("OR");
            V[x] = (byte)(V[x] | V[y]);
            return true;
        }

        public bool AND(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("AND");
            V[x] = (byte)(V[x] & V[y]);
            return true;
        }

        public bool XOR(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("XOR");
            V[x] = (byte)(V[x] ^ V[y]);
            return true;
        }

        public bool ADDXY(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("ADDXY");
            V[0xF] = (byte)(V[x] + V[y] > 255 ? 1 : 0);
            V[x] = (byte)((V[x] + V[y]) & 0x00FF);
            return true;
        }

        public bool SUB(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("SUB");
            V[0xF] = Convert.ToByte(V[x] > V[y]);
            V[x] = (byte)(V[x] - V[y]);
            return true;
        }

        public bool SHR(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("SHR");
            V[0xF] = (byte)(V[x] & 0x0001); // El acarreo debe ser 1 si el bit menos significativo es 1
            V[x] = (byte)(V[x] >> 1);
            return true;
        }

        public bool SUBN(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("SUBN");
            V[0xF] = (byte)(V[y] > V[x] ? 1 : 0);
            V[x] = (byte)((V[y] - V[x]) & 0x00FF);
            return true;
        }

        public bool SHL(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("SHL");
            V[0xF] = (byte)(((V[x] & 0x80) == 0x80) ? 1 : 0);
            V[x] = (byte)(V[x] << 1);
            return true;
        }

        public bool SNEVxVy(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("SNEVxVy");
            if (V[x] != V[y])
                pc = (UInt16)((pc + 2) & 0xFFF);

            return true;
        }

        public bool LDI(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("LDI");
            I = nnn;
            return true;
        }

        public bool JPB0Addr(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("JPB0Addr");
            pc = (UInt16)((nnn + V[0]) & 0xFFF);
            return true;
        }

        public bool RND(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("RND");
            V[x] = (byte)(random.Next() & kk);
            return true;
        }

        public bool DRW(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("DRW");
            V[0xF] = 0;
            for (int j = 0; j < n; j++)
            {
                uint sprite = memory[I + j];
                for (int i = 0; i < 8; i++)
                {
                    int px = (V[x] + i) & 63; // no se debe pasar de 63 (0x111111)
                    int py = (V[y] + j) & 31; // no se debe pasar de 31 (0x11111)

                    int pos = 64 * py + px;
                    int pixel = (sprite & (1 << (7 - i))) != 0 ? 1 : 0;

                    V[0xF] |= (byte)(screen.screen[pos] & pixel);
                    // Si en el sprite el valor está encendido, el valor que estaba en la pantalla se debe cambiar (XOR)
                    screen.screen[pos] ^= (byte)pixel;
                    //screen[64 * py + px] ^= (byte)((sprite & (1 << (7 - i))) != 0 ? 1 : 0); // !=0 ? 1 : 0
                }
            }
            return true;
        }

        public bool InstruccionesE(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            instruccionesE[kk](nnn, kk, n, x, y, p);
            return true;
        }

        public bool SKP(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("SKP");
            if (((keyboard.keyboard >> V[(opcode & 0x0F00) >> 8]) & 0x01) == 0x01) pc += 2;
                        //case 0xA1:
                        //    if (((keyboard.keyboard >> V[(opcode & 0x0F00) >> 8]) & 0x01) != 0x01) pc += 2;
            return true;
        }

        public bool SKNP(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("SKNP");
            if (((keyboard.keyboard >> V[(opcode & 0x0F00) >> 8]) & 0x01) != 0x01) pc += 2;
            return true;
        }

        public bool InstruccionesF(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            instruccionesF[(ushort)(opcode & 0x00FF)](nnn, kk, n, x, y, p);
            return true;
        }

        public bool LDVxDT(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("LDVxDT");
            V[x] = delay_timer;
            return true;
        }

        public bool LDVxK(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("LDVxK");
            WaitingForKeyPress = true;
            pc -= 2;
            return true;
        }

        public bool LDDTVx(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("LDDTVx");
            delay_timer = V[x];
            return true;
        }

        public bool LDSTVx(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("LDSTVx");
            sound_timer = V[x];
            return true;
        }

        public bool ADDIVx(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("ADDIVx");
            I = (ushort)(I + V[x]);
            return true;
        }

        public bool LDFVx(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("LDFVx");
            I = (ushort)(0x50 + (V[x] & 0xF) * 5);
            return true;
        }

        public bool LDBVx(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("LDBVx");
            memory[I + 2] = (byte)(V[x] % 10); // unidades
            memory[I + 1] = (byte)(V[x] / 10 % 10); // decenas
            memory[I] = (byte)(V[x] / 100 % 10); // centenas
            return true;
        }

        public bool LDIVx(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("LDIVx");
            for (int reg = 0; reg <= x; reg++)
            {
                memory[I + reg] = V[reg];
            }
            return true;
        }

        public bool LDVxI(UInt16 nnn, byte kk, byte n, byte x, byte y, byte p)
        {
            //Console.WriteLine("LDVxI");
            for (int reg = 0; reg <= x; reg++)
            {
                V[reg] = memory[I + reg];
            }
            return true;
        }
        #endregion

        #region Métodos

        public void Inicializar()
        {
            // Initialize registers and memory once
            pc = 0x200;
            opcode = 0;
            I = 0;
            sp = 0;

            for (int i = 0; i < memory.Length; i++)
            {
                memory[i] = 0xFF;
            }

            // Clear memory
            Array.Clear(memory, 0, memory.Length);

            // Clear stack
            Array.Clear(stack, 0, stack.Length);

            // Clear registers V0-VF
            Array.Clear(V, 0, V.Length);

            // Clear display
            Array.Clear(screen.screen, 0, screen.screen.Length);

            // Load fontset
            for (int i = 0; i < 80; i++)
            {
                memory[i + 0x50] = screen.chip8_fontset[i];
            }

            // Reset timers
            delay_timer = 0;
            sound_timer = 0;

    }

        public bool EmularCiclo()
        {
            // Obtengo los dos bytes de la memoria adonde apunta PC,
            // correspondientes a la instrucción a ejecutar
            opcode = (UInt16)(memory[pc] << 8 | memory[pc + 1]);

            // Incremento el Program Counter
            pc = (UInt16)((pc + 2) & 0xFFF);

            // Obtengo algunas variables útiles para la ejecución de instrucciones
            UInt16 nnn = (UInt16)(opcode & 0x0FFF);
            byte kk = (byte)(opcode & 0xFF);
            byte n = (byte)(opcode & 0xF);
            byte p = (byte)(opcode >> 12);
            //x - A 4 - bit value, the lower 4 bits of the high byte of the instruction
            byte x = (byte)((opcode >> 8) & 0xF);
            //y - A 4-bit value, the upper 4 bits of the low byte of the instruction
            byte y = (byte)((opcode >> 4) & 0xF);

            // Ejecuto las instrucciones
            instrucciones[p](nnn, kk, n, x, y, p);

            return false;
        }

        public void KeyPressed(byte key)
        {
            WaitingForKeyPress = false;

            var opcode = (ushort)((pc << 8) | (pc + 1));
            V[(opcode & 0x0F00) >> 8] = key;
            pc += 2;
        }

        #endregion
    }
}
