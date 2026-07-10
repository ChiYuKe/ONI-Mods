using KModTool;
using System.IO;

namespace MusicBox
{
    public class ModAssets
    {
        public static float GetSFXVolume()
        {
            return KPlayerPrefs.GetFloat("Volume_SFX") * KPlayerPrefs.GetFloat("Volume_Master");
        }

        public static void LoadAll()
        {
            var path = Path.Combine(AudioUtil.ModPath, "assets");

            AudioUtil.LoadSound(ModAssets.Sounds.C,  Path.Combine(path, "c.wav"),  false, false);
            AudioUtil.LoadSound(ModAssets.Sounds.CS, Path.Combine(path, "c#.wav"), false, false);
            AudioUtil.LoadSound(ModAssets.Sounds.D,  Path.Combine(path, "d.wav"),  false, false);
            AudioUtil.LoadSound(ModAssets.Sounds.DS, Path.Combine(path, "d#.wav"), false, false);
            AudioUtil.LoadSound(ModAssets.Sounds.E,  Path.Combine(path, "e.wav"),  false, false);
            AudioUtil.LoadSound(ModAssets.Sounds.F,  Path.Combine(path, "f.wav"),  false, false);
            AudioUtil.LoadSound(ModAssets.Sounds.FS, Path.Combine(path, "f#.wav"), false, false);
            AudioUtil.LoadSound(ModAssets.Sounds.G,  Path.Combine(path, "g.wav"),  false, false);
            AudioUtil.LoadSound(ModAssets.Sounds.GS, Path.Combine(path, "g#.wav"), false, false);
            AudioUtil.LoadSound(ModAssets.Sounds.A,  Path.Combine(path, "a.wav"),  false, false);
            AudioUtil.LoadSound(ModAssets.Sounds.AS, Path.Combine(path, "a#.wav"), false, false);
            AudioUtil.LoadSound(ModAssets.Sounds.B,  Path.Combine(path, "b.wav"),  false, false);

            AudioUtil.SetSound3DMinMax(ModAssets.Sounds.C,  16f, 10f);
            AudioUtil.SetSound3DMinMax(ModAssets.Sounds.CS, 16f, 10f);
            AudioUtil.SetSound3DMinMax(ModAssets.Sounds.D,  16f, 10f);
            AudioUtil.SetSound3DMinMax(ModAssets.Sounds.DS, 16f, 10f);
            AudioUtil.SetSound3DMinMax(ModAssets.Sounds.E,  16f, 10f);
            AudioUtil.SetSound3DMinMax(ModAssets.Sounds.F,  16f, 10f);
            AudioUtil.SetSound3DMinMax(ModAssets.Sounds.FS, 16f, 10f);
            AudioUtil.SetSound3DMinMax(ModAssets.Sounds.G,  16f, 10f);
            AudioUtil.SetSound3DMinMax(ModAssets.Sounds.GS, 16f, 10f);
            AudioUtil.SetSound3DMinMax(ModAssets.Sounds.A,  16f, 10f);
            AudioUtil.SetSound3DMinMax(ModAssets.Sounds.AS, 16f, 10f);
            AudioUtil.SetSound3DMinMax(ModAssets.Sounds.B,  16f, 10f);
        }

        public static class Sounds
        {
            public static int C  = Hash.SDBMLower("KMOD_MUSICBOX_C");
            public static int CS = Hash.SDBMLower("KMOD_MUSICBOX_CS");
            public static int D  = Hash.SDBMLower("KMOD_MUSICBOX_D");
            public static int DS = Hash.SDBMLower("KMOD_MUSICBOX_DS");
            public static int E  = Hash.SDBMLower("KMOD_MUSICBOX_E");
            public static int F  = Hash.SDBMLower("KMOD_MUSICBOX_F");
            public static int FS = Hash.SDBMLower("KMOD_MUSICBOX_FS");
            public static int G  = Hash.SDBMLower("KMOD_MUSICBOX_G");
            public static int GS = Hash.SDBMLower("KMOD_MUSICBOX_GS");
            public static int A  = Hash.SDBMLower("KMOD_MUSICBOX_A");
            public static int AS = Hash.SDBMLower("KMOD_MUSICBOX_AS");
            public static int B  = Hash.SDBMLower("KMOD_MUSICBOX_B");
        }
    }
}
