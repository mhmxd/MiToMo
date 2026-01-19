using System.Media;

namespace Multi.Cursor
{
    internal class Sounder
    {

        private static SoundPlayer _hitSound = new SoundPlayer(Multi.Cursor.Properties.Resources.hit);
        private static SoundPlayer _startMiss = new SoundPlayer(Multi.Cursor.Properties.Resources.start_miss);
        private static SoundPlayer _targetMiss = new SoundPlayer(Multi.Cursor.Properties.Resources.target_miss);

        static Sounder() {
            _hitSound.Load();
            _startMiss.Load();
            _targetMiss.Load();
        }

        public static void PlayHit() 
        {
            _hitSound.Play(); 
        }
        public static void PlayStartMiss() 
        { 
            _startMiss.Play(); 
        }
        public static void PlayTargetMiss() 
        {
            _targetMiss.Play(); 
        }
    }
}
