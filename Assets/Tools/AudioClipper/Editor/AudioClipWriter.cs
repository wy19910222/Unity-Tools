/*
 * @Author: wangyun
 * @CreateTime: 2024-06-23 02:11:12 149
 * @LastEditor: wangyun
 * @EditTime: 2024-06-27 21:32:42 639
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NAudio.Lame;
using NAudio.Wave.WZT;
using OggVorbis;

public static class AudioClipWriter {
	public static void WriteWAV(string filePath, AudioClip clip, int bitsPerSample) {
		float[] data = new float[clip.samples * clip.channels];
		if (clip.GetData(data, 0)) {
			WavWriter.Write(filePath, data, bitsPerSample, clip.channels, clip.frequency);
		} else {
			Debug.LogError("Get clip data failed.");	
		}
	}
	
	public static void WriteMP3(string filePath, AudioClip clip, int bitsPerSample, int mp3Quality) {
		float[] data = new float[clip.samples * clip.channels];
		if (clip.GetData(data, 0)) {
			Mp3Writer.Write(filePath, data, bitsPerSample, clip.channels, clip.frequency, mp3Quality);
		} else {
			Debug.LogError("Get clip data failed.");	
		}
	}
	
	public static void WriteOGG(string filePath, AudioClip clip, float oggQuality) {
		float[] data = new float[clip.samples * clip.channels];
		if (clip.GetData(data, 0)) {
			OggWriter.Write(filePath, data, clip.channels, clip.frequency, oggQuality);
		} else {
			Debug.LogError("Get clip data failed.");	
		}
	}

	public static class WavWriter {
		public static void Write(string filePath, float[] data, int bitsPerSample, int channels, int frequency) {
			using FileStream fs = new FileStream(filePath, FileMode.Create);
			WriteHeader(fs, bitsPerSample / 8, channels, frequency, data.Length / channels);
			WriteData(fs, data, bitsPerSample / 8);
		}

		public static void WriteHeader(Stream stream, int bytesPerSample, int channels, int frequency, int samples) {
			int dataLength = samples * channels * bytesPerSample;

			byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
			stream.Write(riff, 0, 4);

			// 文件头有8+36个字节的数据
			byte[] chunkSize = BitConverter.GetBytes(dataLength + 36);
			stream.Write(chunkSize, 0, 4);

			byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
			stream.Write(wave, 0, 4);

			byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
			stream.Write(fmt, 0, 4);

			byte[] subChunk1 = BitConverter.GetBytes(16);
			stream.Write(subChunk1, 0, 4);

			byte[] audioFormat = BitConverter.GetBytes(1);
			stream.Write(audioFormat, 0, 2);

			byte[] numChannels = BitConverter.GetBytes(channels);
			stream.Write(numChannels, 0, 2);

			byte[] sampleRate = BitConverter.GetBytes(frequency);
			stream.Write(sampleRate, 0, 4);

			// sampleRate * bytesPerSample * channels, here 44100*2*2
			byte[] byteRate = BitConverter.GetBytes(frequency * channels * bytesPerSample);
			stream.Write(byteRate, 0, 4);

			ushort blockAlign = (ushort) (channels * bytesPerSample);
			stream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

			byte[] bitsPerSample = BitConverter.GetBytes(bytesPerSample * 8);
			stream.Write(bitsPerSample, 0, 2);

			byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
			stream.Write(dataString, 0, 4);

			byte[] subChunk2 = BitConverter.GetBytes(dataLength);
			stream.Write(subChunk2, 0, 4);
		}

		public static void WriteData(Stream stream, IEnumerable<float> data, int bytesPerSample) {
			long floatToIntFactor = (1L << bytesPerSample * 8 - 1) - 1;
			foreach (var f in data) {
				long value = (long) (f * floatToIntFactor);
				for (int j = 0; j < bytesPerSample; j++) {
					stream.WriteByte((byte) (value >> j * 8));
				}
			}
		}
	}

	public static class Mp3Writer {
		public static void Write(string filePath, float[] data, int bitsPerSample, int channels, int frequency, int quality) {
			int blockAlign = channels * bitsPerSample / 8;
			WaveFormat format = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm,
					frequency, channels, frequency * blockAlign, blockAlign, bitsPerSample);
			using LameMP3FileWriter writer = new LameMP3FileWriter(filePath, format, (LAMEPreset) quality);
			WavWriter.WriteData(writer, data, bitsPerSample / 8);
		}
	}

	public static class OggWriter {
		public static void Write(string filePath, float[] data, int channels, int frequency, float quality) {
			VorbisPlugin.Save(filePath, data, (short) channels, frequency, quality);
		}
	}
}