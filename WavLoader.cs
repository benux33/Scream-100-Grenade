using System;
using System.IO;
using UnityEngine;

namespace Scream100.Client
{
    internal static class WavLoader
    {
        internal static AudioClip Load(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (ReadFourCc(reader) != "RIFF")
                {
                    throw new InvalidDataException("Not a RIFF file: " + path);
                }

                reader.ReadInt32();
                if (ReadFourCc(reader) != "WAVE")
                {
                    throw new InvalidDataException("Not a WAVE file: " + path);
                }

                short format = 0;
                short channels = 0;
                int sampleRate = 0;
                short bitsPerSample = 0;
                byte[] audioData = null;

                while (stream.Position + 8 <= stream.Length)
                {
                    string chunkId = ReadFourCc(reader);
                    int chunkSize = reader.ReadInt32();
                    if (chunkSize < 0 || stream.Position + chunkSize > stream.Length)
                    {
                        throw new InvalidDataException("Invalid WAV chunk in " + path);
                    }

                    long nextChunk = stream.Position + chunkSize + (chunkSize & 1);
                    if (chunkId == "fmt ")
                    {
                        format = reader.ReadInt16();
                        channels = reader.ReadInt16();
                        sampleRate = reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt16();
                        bitsPerSample = reader.ReadInt16();
                    }
                    else if (chunkId == "data")
                    {
                        audioData = reader.ReadBytes(chunkSize);
                    }

                    stream.Position = nextChunk;
                }

                if (format != 1 || bitsPerSample != 16 || audioData == null ||
                    channels < 1 || sampleRate < 8000)
                {
                    throw new InvalidDataException(Path.GetFileName(path) + " must be PCM 16-bit WAV audio.");
                }

                int sampleCount = audioData.Length / 2;
                float[] samples = new float[sampleCount];
                for (int index = 0, offset = 0; index < sampleCount; index++, offset += 2)
                {
                    short value = (short)(audioData[offset] | (audioData[offset + 1] << 8));
                    samples[index] = value / 32768f;
                }

                int frameCount = sampleCount / channels;
                AudioClip clip = AudioClip.Create(
                    Path.GetFileNameWithoutExtension(path),
                    frameCount,
                    channels,
                    sampleRate,
                    false);
                if (!clip.SetData(samples, 0))
                {
                    UnityEngine.Object.Destroy(clip);
                    throw new InvalidDataException("Unity rejected " + Path.GetFileName(path) + ".");
                }

                return clip;
            }
        }

        private static string ReadFourCc(BinaryReader reader)
        {
            return new string(reader.ReadChars(4));
        }
    }
}
