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

namespace CykUtils
{
    public static class AudioUtil
    {
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
                    LogUtil.LogError(string.Format("AutoUtil: Failed to create sound instance. (key={0}, error={1})", key, result));
                }
                else
                {
                    LogUtil.LogWarning(string.Format("AutoUtil: Failed to get master channel group. (key={0}, error={1})", key, masterChannelGroup));
                }
            }
            else
            {
                LogUtil.LogWarning(string.Format("AudioUtil: Tried to play sound that does not exist. (key={0})", key));
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
                    LogUtil.LogError(string.Format("AutoUtil: Failed to create sound instance. (key={0}, error={1})", key, result));
                }
                else
                {
                    LogUtil.LogError(string.Format("AutoUtil: Failed to get master channel group. (key={0}, error={1})", key, masterChannelGroup));
                }
            }
            else
            {
                LogUtil.LogWarning(string.Format("AudioUtil: Tried to play sound that does not exist. (key={0})", key));
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
                    LogUtil.LogError(string.Format("AutoUtil: Failed to create sound instance. (key={0}, error={1})", key, result));
                }
                else
                {
                    LogUtil.LogError(string.Format("AutoUtil: Failed to get master channel group. (key={0}, error={1})", key, masterChannelGroup));
                }
            }
            else
            {
                LogUtil.LogWarning(string.Format("AudioUtil: Tried to play sound that does not exist. (key={0})", key));
            }
            return default(Channel);
        }

        // Token: 0x0600000F RID: 15 RVA: 0x00002994 File Offset: 0x00000B94
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

        // Token: 0x04000012 RID: 18
        public static float soundMultiplier = -10f;

        // Token: 0x04000013 RID: 19
        private static readonly Dictionary<int, Sound> sounds = new Dictionary<int, Sound>();
        private static readonly Dictionary<int, Sound> soundsAB = new Dictionary<int, Sound>();
    }
}
