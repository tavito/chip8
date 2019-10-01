using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SDL2;
using static SDL2.SDL;

namespace _8bit_emulator
{
    public class SDLUtil
    {
        public SDL.SDL_Event sdlEvent;

        public System.IntPtr window = IntPtr.Zero;
        public System.IntPtr rnd = IntPtr.Zero;
        public System.IntPtr texture = IntPtr.Zero;
        public System.IntPtr pixels = IntPtr.Zero;
        public IntPtr sdlSurface, sdlTexture = IntPtr.Zero;
        public SDL.SDL_Rect rect = new SDL.SDL_Rect();
        public int pitch;
        uint[] Screen = new uint[64 * 32];


        public SDLUtil() {
            // TODO: Manejo de errores...

            SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING);

            window = SDL.SDL_CreateWindow("CHIP-8 Emulator", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
                    640, 320, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL);

            rnd = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
            texture = SDL.SDL_CreateTexture(rnd, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, 64, 32);
        }

        public void Destroy()
        {
            SDL.SDL_DestroyTexture(texture);
            SDL.SDL_DestroyRenderer(rnd);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }

        public void Render(CPU cpu)
        {
            // Actualiza la textura desde lo que hay en pantalla de la CPU
            SDL.SDL_LockTexture(texture, ref rect, out pixels, out pitch);

            cpu.screen.expansion(cpu.screen, ref Screen);

            var displayHandle = GCHandle.Alloc(Screen, GCHandleType.Pinned);

            sdlSurface = SDL.SDL_CreateRGBSurfaceFrom(displayHandle.AddrOfPinnedObject(), 64, 32, 32, 64 * 4, 0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000);
            sdlTexture = SDL.SDL_CreateTextureFromSurface(rnd, sdlSurface);

            displayHandle.Free();
            SDL.SDL_UnlockTexture(texture);

            //SDL_Delay(1);

            // Renderiza la textura en el renderer
            SDL.SDL_RenderClear(rnd);
            SDL.SDL_RenderCopy(rnd, sdlTexture, IntPtr.Zero, IntPtr.Zero);
            SDL.SDL_RenderPresent(rnd);
        }
    }
}
