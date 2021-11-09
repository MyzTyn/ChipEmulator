using System;
using Chip8;

namespace Chip
{
#if DEBUG
    // Debug to log it
    public class Debugger : IDebugger
    {
        // override Interface's method
        public void Output(string value)
        {
            Console.WriteLine(value);
        }
    }
#endif

     /*
     Use Window Application in this project's config for no console output
     */
    class Program
    {
#if DEBUG
        // To Check if the Application is running in Conole or Window Application
        static bool IsConsoleApp() => Console.OpenStandardInput(1) != System.IO.Stream.Null;
#endif
        // Single Thread Application
        [STAThread]
        static void Main(string[] args)
        {
            VirtualMachine vm;
            string filepath = "";


#if DEBUG
            // Colors
            Color color1 = Color.Zero();
            Color color2 = Color.Zero();

            bool debugMode = false;
            
            if (args.Length > 0)
            {
                // Assume that User only input filepath
                if (args[0].EndsWith(".ch8"))
                    filepath = args[0];
                // Print Help Menu for commands args
                else if (args[0] == "-help")
                    Console.WriteLine("Chip8 Emulator Help:\nOptinals Commands:\n " +
                                "filepath (Enter your rom's filepath and this only accept one args)\n-f filepath (Enter your rom's filepath)\n -debug (Enable DebugMode)\n -color r g b m(m is a mode. 1 = Background or 2 = Sprite Color)\n" +
                                "Example:\n Chip.exe Space Invaders [David Winter].ch8\n Chip.exe -f Space Invaders [David Winter].ch8 -color 255 255 255 2 -debug");
                
                if (args.Length > 1) // If not then do small command args
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        // FilePath
                        if (args[i] == "-f")
                        {
                            if (i + 1 >= args.Length)
                                throw new ArgumentOutOfRangeException("FilePath", "You didn't input correct -f filepath. Example: -f /../pathto/game.ch8");

                            filepath = args[i + 1];
                            i++;
                        }
                        // DebugMode
                        else if (args[i] == "-debug")
                        {
                            debugMode = true;
                        }
                        // Color
                        else if (args[i] == "-color")
                        {
                            if (i + 4 >= args.Length)
                                throw new ArgumentOutOfRangeException("Color", "You didn't input correct -color r g b m. " +
                                    "M is mode: 1 = Background or 2 = Sprite. Example: -color 255 255 255 2");

                            byte r = 0, g = 0, b = 0, m = 0;

                            // Try Parse R
                            if (!byte.TryParse(args[i + 1], out r))
                                throw new Exception("Failed to Parse R. Please input 0 to 255.");
                            // Try Parse g
                            if (!byte.TryParse(args[i + 2], out g))
                                throw new Exception("Failed to Parse R. Please input 0 to 255.");
                            // Try Parse b
                            if (!byte.TryParse(args[i + 3], out b))
                                throw new Exception("Failed to Parse R. Please input 0 to 255.");
                            // Try Parse m
                            if (!byte.TryParse(args[i + 4], out m))
                                throw new Exception("Failed to Parse R. Please input 0 to 255.");
                            else if (m == 1)
                                color1 = new Color(r, g, b, 255);
                            else if (m == 2)
                                color2 = new Color(r, g, b, 255);
                            else
                                throw new Exception("Invalid Input. Please enter 1 (Background) or 2 (Sprite) only.");

                            i += 4;
                        }
                        else
                            Debug.LogWarning("Unknown Command arg: {0}", args[i]);
                    }
                }
            }

            vm = new VirtualMachine(filepath);

            if (debugMode)
            {
                Debug.Enabled = true;
                Debug.Init(new Debugger());

                // Output the code
                Console.WriteLine("Disassembled Code of {0}: ", filepath);
                // Remove empty line (Whitespace)
                System.Collections.Generic.List<string> disassembled = vm.GetDisassmble(true);
                // Print the disassembled code out
                for (int i = 0; i < disassembled.Count; i++)
                    Console.WriteLine(disassembled[i]);
            }
            
            if (IsConsoleApp())
            {
                // Output Console
                Console.WriteLine("Enter any key to start the emulator");
                // Await for user to press any key
                Console.ReadKey();
            }
            
            // Init the Virtual Machine and Set up
            vm.Init(600, 600);
            vm.SetPalettes(color1, color2);

            if (debugMode)
                vm.DebugMainLoop();
            else
                vm.MainLoop();

            // Shutdown the Virtual Machine and clear the resources
            vm.ShutDown();
#else
            // Assume that User only input filepath
            if (args.Length == 1)
                filepath = args[0];
            else
                Console.WriteLine("Please enter filepath only or drag game file to Chip.exe. Example: Chip.exe Space Invaders [David Winter].ch8");

            vm = new VirtualMachine(filepath);
            // Default VM Display: 500 x 500
            vm.Init(500, 500);
            vm.MainLoop();
            vm.ShutDown();
#endif
        }
    }
}
