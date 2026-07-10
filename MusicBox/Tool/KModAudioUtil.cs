using FMOD;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KModTool
{
    public static class AudioUtil
    {

        public static string ModPath
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }

        public static string AssetsPath
        {
            get
            {
                return Path.Combine(ModPath, "assets");
            }
        }

        public static bool LoadSound(int key, string soundFile, bool looping = false, bool oneAtATime = false)
        {
            MODE mode = MODE._2D | MODE._3D | MODE.CREATESAMPLE | MODE._3D_WORLDRELATIVE;
            if (looping)
            {
                mode |= MODE.LOOP_NORMAL;
            }
            if (oneAtATime)
            {
                mode |= MODE.UNIQUE;
            }
            Sound sound;
            RESULT result = RuntimeManager.CoreSystem.createSound(soundFile, mode, out sound);
            if (result == RESULT.OK)
            {
                AudioUtil.sounds.Add(key, sound);
                return true;
            }

            return false;
        }

        public static int PlaySound(int key, float volume = 1f, float pitch = 1f)
        {
            Sound sound;
            if (AudioUtil.sounds.TryGetValue(key, out sound))
            {
                ChannelGroup channelGroup;
                RESULT masterChannelGroup = RuntimeManager.CoreSystem.getMasterChannelGroup(out channelGroup);
                if (masterChannelGroup == RESULT.OK)
                {
                    Channel channel;
                    RESULT result = RuntimeManager.CoreSystem.playSound(sound, channelGroup, true, out channel);
                    if (result == RESULT.OK)
                    {
                        VECTOR vector = new Vector3(SoundListenerController.Instance.transform.position.x, SoundListenerController.Instance.transform.position.y, SoundListenerController.Instance.transform.position.z).ToFMODVector();
                        VECTOR vector2 = default(VECTOR);
                        channel.set3DAttributes(ref vector, ref vector2);
                        channel.setVolume(volume);
                        channel.setPitch(pitch);
                        channel.setPaused(false);
                        int num;
                        channel.getIndex(out num);
                        return num;
                    }
                    global::Debug.LogError(string.Format("AutoUtil: Failed to create sound instance. (key={0}, error={1})", key, result));
                }
                else
                {
                    global::Debug.LogError(string.Format("AutoUtil: Failed to get master channel group. (key={0}, error={1})", key, masterChannelGroup));
                }
            }
            else
            {
                global::Debug.LogWarning(string.Format("AudioUtil: Tried to play sound that does not exist. (key={0})", key));
            }
            return -1;
        }

        /// <summary>
        /// Play sound in pure 2D mode — no 3D positioning, no distance falloff.
        /// Volume is consistent regardless of camera position.
        /// </summary>
        public static int PlaySound2D(int key, float volume = 1f, float pitch = 1f)
        {
            Sound sound;
            if (AudioUtil.sounds.TryGetValue(key, out sound))
            {
                ChannelGroup channelGroup;
                RESULT masterChannelGroup = RuntimeManager.CoreSystem.getMasterChannelGroup(out channelGroup);
                if (masterChannelGroup == RESULT.OK)
                {
                    Channel channel;
                    RESULT result = RuntimeManager.CoreSystem.playSound(sound, channelGroup, true, out channel);
                    if (result == RESULT.OK)
                    {
                        channel.setVolume(volume);
                        channel.setPitch(pitch);
                        channel.setPaused(false);
                        int num;
                        channel.getIndex(out num);
                        return num;
                    }
                    global::Debug.LogError(string.Format("AudioUtil: Failed to create 2D sound instance. (key={0}, error={1})", key, result));
                }
                else
                {
                    global::Debug.LogError(string.Format("AudioUtil: Failed to get master channel group. (key={0}, error={1})", key, masterChannelGroup));
                }
            }
            else
            {
                global::Debug.LogWarning(string.Format("AudioUtil: Tried to play sound that does not exist. (key={0})", key));
            }
            return -1;
        }

        public static int PlaySound(int key, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            Sound sound;
            if (AudioUtil.sounds.TryGetValue(key, out sound))
            {
                ChannelGroup channelGroup;
                RESULT masterChannelGroup = RuntimeManager.CoreSystem.getMasterChannelGroup(out channelGroup);
                if (masterChannelGroup == RESULT.OK)
                {
                    Channel channel;
                    RESULT result = RuntimeManager.CoreSystem.playSound(sound, channelGroup, true, out channel);
                    if (result == RESULT.OK)
                    {
                        VECTOR vector = CameraController.Instance.GetVerticallyScaledPosition(position, false).ToFMODVector();
                        VECTOR vector2 = default(VECTOR);
                        channel.set3DAttributes(ref vector, ref vector2);
                        channel.setVolume(volume);
                        channel.setPitch(pitch);
                        channel.setPaused(false);
                        int num;
                        channel.getIndex(out num);
                        return num;
                    }
                    global::Debug.LogError(string.Format("AutoUtil: Failed to create sound instance. (key={0}, error={1})", key, result));
                }
                else
                {
                    global::Debug.LogError(string.Format("AutoUtil: Failed to get master channel group. (key={0}, error={1})", key, masterChannelGroup));
                }
            }
            else
            {
                global::Debug.LogWarning(string.Format("AudioUtil: Tried to play sound that does not exist. (key={0})", key));
            }
            return -1;
        }

        public static Channel CreateSound(int key)
        {
            Sound sound;
            if (AudioUtil.sounds.TryGetValue(key, out sound))
            {
                ChannelGroup channelGroup;
                RESULT masterChannelGroup = RuntimeManager.CoreSystem.getMasterChannelGroup(out channelGroup);
                if (masterChannelGroup == RESULT.OK)
                {
                    Channel channel;
                    RESULT result = RuntimeManager.CoreSystem.playSound(sound, channelGroup, true, out channel);
                    if (result == RESULT.OK)
                    {
                        return channel;
                    }
                    global::Debug.LogError(string.Format("AutoUtil: Failed to create sound instance. (key={0}, error={1})", key, result));
                }
                else
                {
                    global::Debug.LogError(string.Format("AutoUtil: Failed to get master channel group. (key={0}, error={1})", key, masterChannelGroup));
                }
            }
            else
            {
                global::Debug.LogWarning(string.Format("AudioUtil: Tried to play sound that does not exist. (key={0})", key));
            }
            return default(Channel);
        }

        public static void StopSound(int channelID)
        {
            if (channelID < 0)
            {
                return;
            }
            Channel channel;
            if (RuntimeManager.CoreSystem.getChannel(channelID, out channel) == RESULT.OK)
            {
                channel.stop();
                channel.clearHandle();
            }
        }

        public static void SetSound3DMinMax(int key, float minDistance, float maxDistance)
        {
            Sound sound;
            if (AudioUtil.sounds.TryGetValue(key, out sound))
            {
                sound.set3DMinMaxDistance(minDistance, maxDistance);
            }
        }

        public static float soundMultiplier = -10f;

        private static readonly Dictionary<int, Sound> sounds = new Dictionary<int, Sound>();
    }
}
