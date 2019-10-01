using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SDL2;
using static SDL2.SDL;

namespace _8bit_emulator
{
	public class Sound
	{
        int sample = 0;
        int beepSamples = 0;

        public SDL.SDL_AudioSpec Inicializar(CPU cpu)
        {
            SDL.SDL_AudioSpec audioSpec = new SDL.SDL_AudioSpec();
            audioSpec.channels = 1;
            audioSpec.freq = 44100;
            audioSpec.samples = 256;
            audioSpec.format = SDL.AUDIO_S8;
            audioSpec.callback = new SDL.SDL_AudioCallback((userdata, stream, length) =>
            {
                if (cpu == null) return;

                sbyte[] waveData = new sbyte[length];

                for (int i = 0; i < waveData.Length && cpu.sound_timer > 0; i++, beepSamples++)
                {
                    if (beepSamples == 730)
                    {
                        beepSamples = 0;
                        cpu.sound_timer--;
                    }

                    waveData[i] = (sbyte)(127 * Math.Sin(sample * Math.PI * 2 * 604.1 / 44100));
                    sample++;
                }

                byte[] byteData = (byte[])(Array)waveData;

                Marshal.Copy(byteData, 0, stream, byteData.Length);
            });

            return audioSpec;
        }

	}
}
