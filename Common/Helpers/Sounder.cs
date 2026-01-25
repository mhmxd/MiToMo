using System.Media;
using System.Reflection;

namespace Common.Helpers
{
    public class Sounder
    {
        private static SoundPlayer _hitSound;
        private static SoundPlayer _startMiss;
        private static SoundPlayer _targetMiss;

        static Sounder()
        {
            // Helper to get the stream from the embedded folder
            _hitSound = new SoundPlayer(GetResourceStream("hit.wav"));
            _startMiss = new SoundPlayer(GetResourceStream("start_miss.wav"));
            _targetMiss = new SoundPlayer(GetResourceStream("target_miss.wav"));

            _hitSound.Load();
            _startMiss.Load();
            _targetMiss.Load();
        }

        private static Stream GetResourceStream(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            // Format: [Namespace].[Folder].[SubFolder].[FileName]
            return assembly.GetManifestResourceStream($"Common.Resources.{fileName}");
        }

        public static void PlayHit() => _hitSound.Play();
        public static void PlayStartMiss() => _startMiss.Play();
        public static void PlayTargetMiss() => _targetMiss.Play();
    }
}
