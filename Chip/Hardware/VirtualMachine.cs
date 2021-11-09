using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL2;
using Chip8;
using System.Collections.Generic;

namespace Chip
{
    // Custom Color
    public struct Color
    {
        public Color(byte r, byte g, byte b, byte a)
        {
            this.r = r; 
            this.g = g; 
            this.b = b; 
            this.a = a;
        }
        // Convert from Color to RGBA8888 Format
        public uint ToRGBA8888()
        {
            return (uint)((r << 24) + (g << 16) + (b << 8) + a);
        }
        // Create new Color with Zeros
        public static Color Zero()
        {
            return new Color(0, 0, 0, 0);
        }
        // Check and make sure Color is not empty
        public bool IsEmpty()
        {
            return (r == 0) & (g == 0) & (b == 0) & (a == 0);
        }

        public byte r;
        public byte g;
        public byte b;
        public byte a;
    }

    public class VirtualMachine
    {
        // Only accept .ch8 format
        public VirtualMachine(string Filepath)
        {
            m_CPU = new CHIP_8();
            m_CPU.LoadRom(Filepath);
        }

        #region SDL2 System
        // Init SDL2 system
        public void Init(int Width, int Height)
        {
            if (!m_Initialized)
            {
                if (Width == 0 && Height == 0)
                {
                    m_Width = 500;
                    m_Height = 500;
                }
                else
                {
                    m_Width = Width;
                    m_Height = Height;
                }

                // Init SDL
                if (SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING) < 0)
#if DEBUG
                    Chip8.Debug.LogError("There was an issue initilizing SDL. {0}", SDL.SDL_GetError());
#else
                    // Failed
                    System.Environment.Exit(-1);
#endif
                    // Create Window
                    m_Window = SDL.SDL_CreateWindow("CHIP-8 Emulator", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
                    m_Width, m_Height,
                    SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

                // Create Renderer
                m_Renderer = SDL.SDL_CreateRenderer(m_Window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
                    SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

                SDL.SDL_SetWindowResizable(m_Window, SDL.SDL_bool.SDL_TRUE);

                SDL.SDL_RenderSetLogicalSize(m_Renderer, 64, 32);

                m_Texture = SDL.SDL_CreateTexture(m_Renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                    64, 32);

                m_Initialized = true;
            }
        }

        // Resize the window
        private void WindowOnResize(int Width, int Height)
        {
            // Check and make sure that Width or Height can't be Zero
            if (Width <= 0 || Height <= 0)
            {
                m_Width = 500;
                m_Height = 500;
            }
            else
            {
                m_Width = Width;
                m_Height = Height;
            }

            SDL.SDL_SetWindowSize(m_Window, m_Width, m_Height);

            RenderGraphic();
        }

        // Poll Events
        private void SDLPollEvent()
        {
            SDL.SDL_PollEvent(out SDL.SDL_Event e);

            // SDL's Event

            if (e.type == SDL.SDL_EventType.SDL_QUIT)
            {
                m_Running = false;
                SDL.SDL_Quit();
            }
            else if (e.type == SDL.SDL_EventType.SDL_WINDOWEVENT)
            {
                if (e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED)
                    WindowOnResize(e.window.data1, e.window.data2);
            }
            else if (e.type == SDL.SDL_EventType.SDL_KEYDOWN)
            {
                int key = KeyCodeToKey(e.key.keysym.sym);
                if (key != -1)
                {
                    m_CPU.key[key] = 1;

                    if (m_CPU.waitingForKeyPress) m_CPU.KeyPressed((byte)key);
                }

            }
            else if (e.type == SDL.SDL_EventType.SDL_KEYUP)
            {
                int key = KeyCodeToKey(e.key.keysym.sym);
                if (key != -1)
                {
                    m_CPU.key[key] = 0;
                }
            }
        }

        // Shutdown SDL2 system
        public void ShutDown()
        {
            SDL.SDL_DestroyRenderer(m_Renderer);
            SDL.SDL_DestroyWindow(m_Window);
            SDL.SDL_Quit();
            m_Initialized = false;
        }

        // Convert from SDL KeyCode to Chip 8 Hex Input (Mapped the input)
        private static int KeyCodeToKey(SDL.SDL_Keycode keycode)
        {
            switch (keycode)
            {
                case SDL.SDL_Keycode.SDLK_1: // 1
                    return 0x1;
                case SDL.SDL_Keycode.SDLK_2: // 2
                    return 0x2;
                case SDL.SDL_Keycode.SDLK_3: // 3
                    return 0x3;
                case SDL.SDL_Keycode.SDLK_4: // C
                    return 0xC;

                case SDL.SDL_Keycode.SDLK_q: // 4
                    return 0x4;
                case SDL.SDL_Keycode.SDLK_w: // 5
                    return 0x5;
                case SDL.SDL_Keycode.SDLK_e: // 6
                    return 0x6;
                case SDL.SDL_Keycode.SDLK_r: // D
                    return 0xD;

                case SDL.SDL_Keycode.SDLK_a: // 7
                    return 0x7;
                case SDL.SDL_Keycode.SDLK_s: // 8
                    return 0x8;
                case SDL.SDL_Keycode.SDLK_d: // 9
                    return 0x9;
                case SDL.SDL_Keycode.SDLK_f: // E
                    return 0xE;

                case SDL.SDL_Keycode.SDLK_z: // A
                    return 0xA;
                case SDL.SDL_Keycode.SDLK_x: // 0
                    return 0;
                case SDL.SDL_Keycode.SDLK_c: // B
                    return 0xB;
                case SDL.SDL_Keycode.SDLK_v: // F
                    return 0xF;
            }

            return -1;
        }

        // Change color of Graphic - Max Two Color
        public void SetPalettes(Color background, Color sprite)
        {
            if (!background.IsEmpty())
                m_Color0 = background;

            if (!sprite.IsEmpty())
                m_Color1 = sprite;
        }

        // Render The Graphic to SDL
        private void RenderGraphic()
        {
            uint[] buffers = new uint[m_CPU.gfx.Length];
            for (int i = 0; i < m_CPU.gfx.Length; i++)
            {
                if (m_CPU.gfx[i])
                    buffers[i] = m_Color1.ToRGBA8888();
                else if (!m_CPU.gfx[i])
                    buffers[i] = m_Color0.ToRGBA8888();
            }

            // Convert from uint array to IntPTR
            var displayHandle = GCHandle.Alloc(buffers, GCHandleType.Pinned);
            SDL.SDL_UpdateTexture(m_Texture, IntPtr.Zero, displayHandle.AddrOfPinnedObject(), 64 * 4);
            displayHandle.Free();

            // Render to the Screen
            SDL.SDL_RenderClear(m_Renderer);
            SDL.SDL_RenderCopy(m_Renderer, m_Texture, IntPtr.Zero, IntPtr.Zero);
            SDL.SDL_RenderPresent(m_Renderer);
        }
#endregion

        // Emulate Chip-8
        public void MainLoop()
        {
            if (!m_Initialized)
                Init(m_Width, m_Height);

            m_Running = true;

            frameTimer = Stopwatch.StartNew();
            
            while (m_Running)
            {
                // Make sure the waitingForKeyPress is false before execute opcode
                if (!m_CPU.waitingForKeyPress)
                    m_CPU.ExecuteOpCode();

                if (frameTimer.ElapsedTicks > ticksPer60hz)
                {
                    SDLPollEvent();

                    RenderGraphic();

                    frameTimer.Restart();
                }

                // To Delay 1 millionsecond
                System.Threading.Thread.Sleep(1);
            }
        }

#if DEBUG
#region Debug Stuff

        // Convert from byte to readable for human.
        public List<string> GetDisassmble(bool removeWhitespace = false)
        {
            return m_CPU.Disassmble(0, (ushort)m_CPU.memory.GetLength(), removeWhitespace);
        }

        // Debug Emulate Chip-8
        public void DebugMainLoop()
        {
            if (!m_Initialized)
                Init(m_Width, m_Height);
            m_Disassmbled = m_CPU.Disassmble(0, (ushort)m_CPU.memory.GetLength());

            bool mode = false;
            bool nextCode = false;
            m_Running = true;

            frameTimer = Stopwatch.StartNew();

            Console.Clear();

            // Execute one instruction code
            if (!mode)
            {
                // Make sure the waitingForKeyPress false before execute opcode
                if (!m_CPU.waitingForKeyPress)
                    m_CPU.ExecuteOpCode();
                
                PrintCPUDebug();
            }

            while (m_Running)
            {
                

                // Normal Speed
                if (mode)
                {
                    
                    // Make sure the waitingForKeyPress false before execute opcode
                    if (!m_CPU.waitingForKeyPress)
                        m_CPU.ExecuteOpCode();

                    if (frameTimer.ElapsedTicks > ticksPer60hz)
                    {
                        RenderGraphic();

                        frameTimer.Restart();
                    }

                    
                    // Delay 1 ms
                    System.Threading.Thread.Sleep(1);
                }
                // Step
                else if (!mode)
                {
                    if (nextCode)
                    {
                        // Make sure the waitingForKeyPress is false before execute opcode
                        if (!m_CPU.waitingForKeyPress)
                            m_CPU.ExecuteOpCode();

                        RenderGraphic();
                        frameTimer.Restart();

                        nextCode = false;
                    }
                        
                }

                // Debug Custom Event
                SDL.SDL_PollEvent(out SDL.SDL_Event e);
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        {
                            m_Running = false;
                            SDL.SDL_Quit();
                            break;
                        }
                    case SDL.SDL_EventType.SDL_WINDOWEVENT:
                        {
                            if (e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED)
                                WindowOnResize(e.window.data1, e.window.data2);
                            break;
                        }
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                        {
                            if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_n)
                            {
                                // Reverse true and false bool
                                mode = mode != true;
                                if (!mode)
                                {
                                    frameTimer.Restart();
                                }
                            }
                            if (mode)
                            {
                                int key = KeyCodeToKey(e.key.keysym.sym);
                                if (key != -1)
                                {
                                    m_CPU.key[key] = 1;

                                    if (m_CPU.waitingForKeyPress) m_CPU.KeyPressed((byte)key);
                                }
                            }
                            else
                            {
                                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_SPACE)
                                    nextCode = true;
                            }
                            break;
                        }
                    case SDL.SDL_EventType.SDL_KEYUP:
                        {
                            if (mode)
                            {
                                int key = KeyCodeToKey(e.key.keysym.sym);
                                if (key != -1)
                                {
                                    m_CPU.key[key] = 0;
                                }
                            }
                            break;
                        }
                }

                PrintCPUDebug();
            }
        }

        private void PrintCPUDebug()
        {
            // Print the Debug Info
            Console.SetCursorPosition(0, 0);
            
            Console.WriteLine("Opcode: {0}\n{1}\nCPU Info:\n{2}\n{3}", 
                m_CPU.memory.ReadShort(m_CPU.registers.PC).ToString("X4"), m_Disassmbled[m_CPU.registers.PC],
                m_CPU.ToString(), m_CPU.VToString());
        }

        // For Print CPU Debug
        Dictionary<ushort, string> m_Disassmbled;
#endregion
#endif

#region Variables
        // SDL2 Stuff
        private IntPtr m_Window;
        private IntPtr m_Renderer;
        private IntPtr m_Texture;

        private int m_Width;
        private int m_Height;

        private bool m_Initialized = false;
        private bool m_Running = false;

        private Color m_Color0 = new Color(0, 0, 0, 255);
        private Color m_Color1 = new Color(255, 255, 255, 255);

        private CHIP_8 m_CPU;

        // Timer
        Stopwatch frameTimer;
        static int ticksPer60hz = (int)(Stopwatch.Frequency * 0.016);
        #endregion
    }
}
