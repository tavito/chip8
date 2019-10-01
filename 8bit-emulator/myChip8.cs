using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SDL2;
using static SDL2.SDL;

namespace _8bit_emulator
{
    public class MyChip8
    {
        // Chip8 es big endian, es decir, "menos significativo" se refiere a lo que está más a la derecha,
        // "mas significativo" se refiere a lo que está más a la izquierda

        public static Keyboard keyboard = new Keyboard();
        public static Display screen = new Display();
        public CPU cpu = new CPU(screen, keyboard);
        Sound sonido = new Sound();

        public MyChip8()
        {
            SDLUtil sdlUtil = new SDLUtil();
            cpu.Inicializar();

            //load("PONG");
            load("C:\\Users\\Usuario\\Downloads\\CHIP-8-v.1.2-win64\\games\\roms\\VBRIX");

            bool termina = false;
            uint last_ticks = 0;
            uint cycles = 0;
            
            bool detener = false;
            
            // Inicializo el sonido
            SDL.SDL_AudioSpec audioSpec = sonido.Inicializar(cpu);
            SDL.SDL_OpenAudio(ref audioSpec, IntPtr.Zero);

            // Bucle principal
            while (!termina)
            {
                // Administra el teclado
                while (SDL.SDL_PollEvent(out sdlUtil.sdlEvent) != 0)
                {
                    if (sdlUtil.sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        termina = true;
                        break;
                    }
                    else if (sdlUtil.sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN)
                    {
                        var key = KeyCodeToKey((int)sdlUtil.sdlEvent.key.keysym.sym);
                        cpu.keyboard.keyboard |= (ushort)(1 << key);

                        // **********************************************************
                        // Verifica tecla de "Pausa" --> No pertenece al CHIP 8!!!
                        if (key.ToString() == "25")
                            detener = true;
                        else
                            detener = false;
                        // **********************************************************

                        if (cpu.WaitingForKeyPress)
                            cpu.KeyPressed((byte)key);
                    }
                    else if (sdlUtil.sdlEvent.type == SDL.SDL_EventType.SDL_KEYUP)
                    {
                        var key = KeyCodeToKey((int)sdlUtil.sdlEvent.key.keysym.sym);
                        cpu.keyboard.keyboard &= (ushort)~(1 << key);
                    }
                }

                // Ejecuta un ciclo
                if (SDL_GetTicks() - cycles > 1)
                {
                    // Sólo se puede ejecutar un ciclo si no estamos esperando que el usuario pulse una tecla
                    if (!cpu.WaitingForKeyPress)
                        cpu.EmularCiclo();
                    else
                    {
                        // TODO: --> VIDEO 8 - 1:00:00
                    }
                    cycles = SDL_GetTicks();
                }
                
                // Actualizo un fotograma cada luego de 60 ticks por segundo (60 instrucciones x segundo)
                // y actualizo los timers
                if ((SDL.SDL_GetTicks() - last_ticks > (1000/60)) && !detener)
                {
                    // Update timers
                    if (cpu.delay_timer > 0)
                        --cpu.delay_timer;

                    if (cpu.sound_timer > 0)
                    {
                        if (cpu.sound_timer > 0)
                            SDL.SDL_PauseAudio(0);
                        else //if (cpu.sound_timer == 0)
                            SDL.SDL_PauseAudio(1);
                        --cpu.sound_timer;
                    }

                    sdlUtil.Render(cpu);

                    last_ticks = SDL.SDL_GetTicks();
                }
            }

            // Finalizo
            sdlUtil.Destroy();
        }

        private static int KeyCodeToKey(int keycode)
        {
            int keyIndex = 0;
            if (keycode < 58) keyIndex = keycode - 48;
            else keyIndex = keycode - 87;

            return keyIndex;
        }

        public void load(string game)
        {
            byte[] buffer = File.ReadAllBytes(game);

            for (int i = 0; i < buffer.Length; i++)
            {
                cpu.memory[i + 512] = buffer[i];
            }
        }
    }
}
